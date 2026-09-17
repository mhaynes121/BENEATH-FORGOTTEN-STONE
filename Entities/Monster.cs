using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities.AI;
using BENEATH_FORGOTTEN_STONE.Entities.Components;
using BENEATH_FORGOTTEN_STONE.Entities.Sounds;
using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Entities;

public class Monster : Actor
{
    private class Archetype
    {
        public string Name;
        public char Symbol;
        public ConsoleColor Color;
        public int Hp;
        public int Attack;
        public int Defense;
        public int Speed;
        public int Mana;
        public int Agility = 10;
        public double DifficultyRating = 1.0;
        public long? XpRewardOverride;
        public Spell[] KnownSpells = Array.Empty<Spell>();
        public string ShortDescription = "";
        public string LongDescription = "";
        public Size Size = Size.Small;
        public AttackType AttackType = AttackType.Hit;

        /// <summary>When true, CreateRandom force-equips a ranged weapon + ammo if the random loot roll didn't already grant one -- see EquipGuaranteedRangedWeapon. Only decides the SPAWN-time gear guarantee; AI assignment itself is derived from whatever's actually equipped (IsRangedWeapon), not this flag, so a save/restore round-trip picks the right AI with no archetype lookup needed.</summary>
        public bool PreferRangedAttack = false;

        /// <summary>What kind of creature this is -- see CreatureType. Drives the CanCarryItems/CanEquipItems/CanUseItems defaults below via CreatureCapabilities, unless overridden.</summary>
        public CreatureType CreatureType = CreatureType.Other;

        /// <summary>Null (the default for every archetype above) means "use CreatureType's default" -- see CreatureCapabilities.GetDefaults. Set only to deviate from that default for an unusual individual archetype.</summary>
        public bool? CanCarryItemsOverride;
        public bool? CanEquipItemsOverride;
        public bool? CanUseItemsOverride;

        /// <summary>Null (the default -- ordinary monsters, section 17 of the tile-attuned monster spec) means this archetype has no terrain dependency at all. Non-null makes Monster.IsFloorAttuned true and drives spawn placement (DungeonGenerator.SpawnMonsters), movement preference (FloorAttunedAI), off-terrain attrition, and elemental resistance/vulnerability (FloorDamageCalculator). Deliberately not a separate bool -- see Monster.IsFloorAttuned's own doc comment.</summary>
        public FloorType? PreferredFloorType;

        /// <summary>Null (NONE) is valid even for a floor-attuned archetype (e.g. Grass/Ash creatures with no sensible elemental identity) -- OpposingElement is derived from this via ElementalOpposition, never hard-coded per archetype.</summary>
        public DamageType? ElementalAffinity;

        public double OffPreferredFloorDamagePercent = FloorAttunementConfig.DefaultOffPreferredFloorDamagePercent;

        /// <summary>Innate resistance values, independent of the floor-attunement system above -- see the Resistance System spec section 38/39. Defaults to ResistanceSet.Zero; only a handful of strongly elemental-themed archetypes (Fire Elemental, Frost Wraith, ...) declare explicit values.</summary>
        public ResistanceSet BaseResistances = ResistanceSet.Zero;

        /// <summary>Ambient-sound archetype (Ambient Sound / Hearing System spec) -- default None means naturally silent, contributing to neither the level-wide creature ambient pool nor (if this archetype ever becomes a boss) a boss sound. See Core/SoundSystem.cs.</summary>
        public MonsterSoundType SoundType = MonsterSoundType.None;
    }

    private readonly int agility;
    private readonly AttackType attackType;

    public override int Agility => agility;

    /// <summary>An equipped weapon's AttackType (PrimaryHand then OffHand) overrides this monster's innate archetype AttackType -- same rule as Player.AttackType. Falls back to the archetype's own natural attack (Bite/Sting/Hit) when nothing's equipped in either hand, which is the common case for most monsters.</summary>
    public override AttackType AttackType =>
        Equipment.Get(EquipmentSlot.PrimaryHand)?.AttackType ??
        Equipment.Get(EquipmentSlot.OffHand)?.AttackType ??
        attackType;

    public double DifficultyRating { get; }
    public long? XpRewardOverride { get; }

    /// <summary>What's equipped, if anything -- see CanEquipItems and LootGenerator.GenerateSpawnItems. Mirrors Player.Equipment, but nothing here ever changes after spawn (monsters have no equip/unequip UI), so unlike Player there's no separate base-vs-derived stat bookkeeping: equipment bonuses are folded into BasePhysicalAttackPower/BaseMagicalAttackPower/DefensePower exactly once, in CreateRandom/Restore.</summary>
    public EquipmentComponent Equipment { get; } = new();

    public override IEnumerable<Item> GetEquippedItems() => Equipment.AllEquipped.Select(kvp => kvp.Value);

    /// <summary>Gates which loot Size tiers this monster can drop -- see LootGenerator.</summary>
    public Size Size { get; }

    /// <summary>What kind of creature this is -- see CreatureType's own doc comment. Never used for behavior decisions directly; check CanCarryItems/CanEquipItems/CanUseItems instead.</summary>
    public CreatureType CreatureType { get; set; }

    /// <summary>Whether this monster can carry items at all -- LootGenerator returns no drops whatsoever when false, regardless of CanEquipItems. Defaults from CreatureType (see CreatureCapabilities), overridable per archetype.</summary>
    public bool CanCarryItems { get; set; }

    /// <summary>Whether this monster can be generated with equippable gear (Weapon/Armor/anything with an EquipmentType) as loot -- see LootGenerator. Meaningless if CanCarryItems is false.</summary>
    public bool CanEquipItems { get; set; }

    /// <summary>Whether this monster is capable of using an item (e.g. drinking a potion) -- see ChaseAI's healing-item check.</summary>
    public bool CanUseItems { get; set; }

    /// <summary>Null for an ordinary monster (design spec section 17) -- the FloorType this monster is naturally adapted to. Settable (like CreatureType below) rather than constructor-only, assigned once at spawn/restore and never mutated again. See IsFloorAttuned, FloorAttunedAI, FloorDamageCalculator.GetElementalTerrainMultiplier, and Core/EnvironmentalFloorEffects's off-preferred-floor attrition.</summary>
    public FloorType? PreferredFloorType { get; set; }

    /// <summary>Convenience for "this monster has a terrain dependency at all" -- deliberately not a separate stored bool (the design spec explicitly calls out avoiding redundant state here): PreferredFloorType.HasValue already says the same thing.</summary>
    public bool IsFloorAttuned => PreferredFloorType.HasValue;

    /// <summary>This monster's elemental identity, if any -- null (NONE) is valid even for a floor-attuned monster (e.g. a Grass/Ash creature with no sensible element). Drives OpposingElement below via the one centralized ElementalOpposition table; never set directly from a monster's name/species elsewhere.</summary>
    public DamageType? ElementalAffinity { get; set; }

    /// <summary>Always derived from ElementalAffinity via the one centralized ElementalOpposition table -- never stored/persisted separately, so it can never drift out of sync with ElementalAffinity. See FloorDamageCalculator.GetElementalTerrainMultiplier, the only thing that reads this.</summary>
    public DamageType? OpposingElement => ElementalOpposition.GetOpposingElement(ElementalAffinity);

    /// <summary>Fraction of MaxHP lost per completed turn spent off PreferredFloorType -- meaningless when PreferredFloorType is null. See Core/EnvironmentalFloorEffects.ApplyFloorAttunementAttrition.</summary>
    public double OffPreferredFloorDamagePercent { get; set; } = FloorAttunementConfig.DefaultOffPreferredFloorDamagePercent;

    /// <summary>Innate resistance values (Resistance System spec section 38) -- independent of, and additive with, the floor-attunement environmental bonus computed in GetEffectiveResistance below. Defaults to ResistanceSet.Zero.</summary>
    public ResistanceSet BaseResistances { get; set; } = ResistanceSet.Zero;

    /// <summary>Ambient-sound archetype (Ambient Sound / Hearing System spec) -- see Core/SoundSystem.cs, which reads this both for the level-wide creature ambient pool (when this monster isn't the boss) and, when it IS the boss (IsBoss), for the boss's own distance-gated sound. Default None means naturally silent.</summary>
    public MonsterSoundType SoundType { get; set; } = MonsterSoundType.None;

    /// <summary>
    /// BaseResistances plus, for a floor-attuned monster, its OpposingElement's own environmental
    /// swing: FloorAttunementConfig.OpposingElementBaseResistance always applies (representing
    /// "away from home, quite vulnerable"), and PreferredFloorEnvironmentalResistanceBonus adds
    /// on top only while standing on PreferredFloorType (representing terrain protection) --
    /// together they reproduce exactly the old on/off-preferred-floor damage multipliers
    /// (0.5x/1.5x) this replaces, just expressed as resistance instead of a separate special
    /// multiplier (Resistance System spec sections 35-37: only one representation of this
    /// concept should exist). Clamped to ResistanceConfig's Min/Max like any other effective
    /// resistance.
    /// </summary>
    public int GetEffectiveResistance(ResistanceType type, FloorType currentFloorType)
    {
        int total = BaseResistances.Get(type)
            + ResistanceCalculator.SumEquipmentResistance(this, type, cursedOnly: false)
            + ResistanceCalculator.SumEquipmentResistance(this, type, cursedOnly: true)
            + ResistanceCalculator.SumActiveEffectResistance(this, type);

        if (IsFloorAttuned && OpposingElement.HasValue && ResistanceTypeMapping.ForDamageType(OpposingElement.Value) == type)
        {
            total += FloorAttunementConfig.OpposingElementBaseResistance;
            if (currentFloorType == PreferredFloorType)
            {
                total += FloorAttunementConfig.PreferredFloorEnvironmentalResistanceBonus;
            }
        }

        return Math.Clamp(total, ResistanceConfig.MinimumResistance, ResistanceConfig.MaximumResistance);
    }

    /// <summary>Set once ChaseAI/SpellCasterAI starts chasing the player -- the accepted stand-in for "hasn't noticed you yet" (Backstab/Shadowstep), since this game has no facing/geometry concept. Never resets, so a monster that once noticed the player stays alerted for the rest of the encounter.</summary>
    public bool IsAlerted { get; set; }

    /// <summary>An enhanced version of a normal archetype rather than a distinct monster class -- see CreateBoss. Its underlying Name/CreatureType stay exactly as a normal spawn would have them; player-facing text should use DisplayName instead, which returns a generated proper name + epithet for a boss (see BossName/BossEpithet/BossNameGenerator) and just Name otherwise.</summary>
    public bool IsBoss { get; set; }

    /// <summary>Proper first name for a boss -- see BossNameGenerator. Null for a normal monster. Deliberately independent of Name/CreatureType so every ordinary gameplay system (AI, loot, equipment, combat math) keeps working from the unchanged underlying archetype.</summary>
    public string BossName { get; set; }

    /// <summary>Epithet paired with BossName -- combined per BossEpithetFormat to build DisplayName. Null for a normal monster.</summary>
    public string BossEpithet { get; set; }

    /// <summary>Whether BossEpithet displays directly after BossName ("Lormax Golden Wing") or after "the" ("Norro the Eternal") -- see BossNameGenerator.EpithetFormat.</summary>
    public EpithetFormat BossEpithetFormat { get; set; }

    /// <summary>Combines BossName/BossEpithet/BossEpithetFormat into the boss's player-facing identity. Falls back to the plain archetype Name for a non-boss. Every combat/Look/death message should read this instead of Name.</summary>
    public override string DisplayName => IsBoss
        ? (BossEpithetFormat == EpithetFormat.The ? $"{BossName} the {BossEpithet}" : $"{BossName} {BossEpithet}")
        : Name;

    public override bool UsesProperNounDisplayName => IsBoss;

    private Monster(int agility, double difficultyRating, long? xpRewardOverride, Size size, AttackType attackType)
    {
        this.agility = agility;
        DifficultyRating = difficultyRating;
        XpRewardOverride = xpRewardOverride;
        Size = size;
        this.attackType = attackType;
    }

    // Base stats retuned so early combat isn't degenerate: previously a floor-1
    // rat (Hp 4+1, Attack 2, Defense 0) died to a single Warrior hit every
    // time, and dealt exactly 0 damage back to a Warrior's base Defense of 3.
    private static readonly Archetype[] Archetypes =
    {
            new()
            {
                Name = "rat", Symbol = 'r', Color = ConsoleColor.DarkGray, Hp = 8, Attack = 3, Defense = 1, Speed = 12, Agility = 14, DifficultyRating = 0.25, CreatureType = CreatureType.Animal,
                ShortDescription = "A mangy rat, twitching and wary.",
                LongDescription = "An oversized rat, its fur matted and its teeth yellowed. Its eyes dart toward every movement, sizing up whether to bite or flee.",
                Size = Size.Small, AttackType = AttackType.Bite
            },
            new()
            {
                Name = "goblin", Symbol = 'g', Color = ConsoleColor.Green, Hp = 16, Attack = 5, Defense = 2, Speed = 10, Agility = 10, DifficultyRating = 0.50, CreatureType = CreatureType.Humanoid,
                ShortDescription = "A small, nervous-looking goblin clutching a crude weapon.",
                LongDescription = "The goblin is covered in dirt and old scars. Its crude weapon looks poorly maintained, but its watchful eyes suggest it is more dangerous than its appearance implies.",
                Size = Size.Small, AttackType = AttackType.Hit, SoundType = MonsterSoundType.Voices
            },
            new()
            {
                Name = "kobold shaman", Symbol = 'k', Color = ConsoleColor.Magenta, CreatureType = CreatureType.Humanoid,
                Hp = 12, Attack = 3, Defense = 2, Speed = 10, Mana = 20, Agility = 8, DifficultyRating = 1.25,
                KnownSpells = new[] { SpellCatalog.Heal, SpellCatalog.MagicMissile },
                ShortDescription = "A hunched kobold muttering under its breath.",
                LongDescription = "A wiry kobold draped in bone talismans, murmuring words in a language that predates its own tribe. Its claws crackle faintly with restless magic.",
                Size = Size.Small, AttackType = AttackType.Hit
            },

            // Bulk roster below: no KnownSpells assigned yet even where Mana > 0 --
            // that pool sits unused (ChaseAI, not SpellCasterAI) until spells are
            // explicitly assigned per monster, same as every other archetype above
            // started out. "orc" here replaces the single old entry of that name.
            new()
            {
                Name = "giant rat", Symbol = 'r', Color = ConsoleColor.Gray, Hp = 8, Attack = 3, Defense = 1, Speed = 14, Mana = 0, Agility = 14, DifficultyRating = 0.20, CreatureType = CreatureType.Animal,
                ShortDescription = "An oversized rat with matted fur and yellowed teeth.",
                LongDescription = "Its beady eyes dart from side to side as it sniffs the air, ready to flee or bite anything that comes too close.",
                Size = Size.Small, AttackType = AttackType.Bite
            },
            new()
            {
                Name = "giant bat", Symbol = 'b', Color = ConsoleColor.DarkGray, Hp = 7, Attack = 3, Defense = 1, Speed = 15, Mana = 0, Agility = 14, DifficultyRating = 0.20, CreatureType = CreatureType.Animal,
                ShortDescription = "A leathery bat with an enormous wingspan.",
                LongDescription = "The creature circles silently overhead, its tiny eyes fixed upon you as it searches for an opportunity to strike.",
                Size = Size.Small, AttackType = AttackType.Bite, SoundType = MonsterSoundType.Wings
            },
            new()
            {
                Name = "giant centipede", Symbol = 'c', Color = ConsoleColor.DarkRed, Hp = 12, Attack = 4, Defense = 1, Speed = 13, Mana = 0, Agility = 13, DifficultyRating = 0.30, CreatureType = CreatureType.Animal,
                ShortDescription = "A many-legged insect with gleaming mandibles.",
                LongDescription = "Dozens of legs propel the centipede rapidly across the dungeon floor while its venomous mandibles snap hungrily.",
                Size = Size.Small, AttackType = AttackType.Sting
            },
            new()
            {
                Name = "cave lizard", Symbol = 'l', Color = ConsoleColor.Green, Hp = 11, Attack = 4, Defense = 2, Speed = 11, Mana = 0, Agility = 11, DifficultyRating = 0.25, CreatureType = CreatureType.Animal,
                ShortDescription = "A thick-scaled lizard adapted to the darkness.",
                LongDescription = "Its rough scales blend into the stone as it creeps forward, its claws scraping quietly against the dungeon floor.",
                Size = Size.Small, AttackType = AttackType.Bite
            },
            new()
            {
                Name = "kobold", Symbol = 'k', Color = ConsoleColor.DarkYellow, Hp = 10, Attack = 4, Defense = 2, Speed = 10, Mana = 0, Agility = 9, DifficultyRating = 0.35, CreatureType = CreatureType.Humanoid,
                ShortDescription = "A wiry reptilian humanoid with a crude weapon.",
                LongDescription = "Its scales are dull and scarred, and it watches you with nervous eyes while gripping its crude weapon with both hands.",
                Size = Size.Medium, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "giant cockroach", Symbol = 'C', Color = ConsoleColor.DarkYellow, Hp = 10, Attack = 3, Defense = 2, Speed = 12, Mana = 0, Agility = 12, DifficultyRating = 0.25, CreatureType = CreatureType.Animal,
                ShortDescription = "An enormous insect scurrying across the floor.",
                LongDescription = "Its armored shell gleams in the dim light as it skitters unpredictably across the dungeon floor.",
                Size = Size.Small, AttackType = AttackType.Bite
            },
            new()
            {
                Name = "cave spider", Symbol = 's', Color = ConsoleColor.DarkMagenta, Hp = 13, Attack = 5, Defense = 2, Speed = 12, Mana = 6, Agility = 13, DifficultyRating = 0.35, CreatureType = CreatureType.Animal,
                ShortDescription = "A hairy spider creeping along the stone.",
                LongDescription = "Its eight legs move with unsettling precision as it searches the darkness for something warm to bite.",
                Size = Size.Small, AttackType = AttackType.Sting, SoundType = MonsterSoundType.Skitter
            },
            new()
            {
                Name = "goblin scavenger", Symbol = 'g', Color = ConsoleColor.Green, Hp = 13, Attack = 5, Defense = 2, Speed = 11, Mana = 0, Agility = 11, DifficultyRating = 0.40, CreatureType = CreatureType.Humanoid,
                ShortDescription = "A scruffy goblin carrying scavenged equipment.",
                LongDescription = "Rusty scraps of armor hang from its wiry body as it clutches a collection of stolen weapons and watches you suspiciously.",
                Size = Size.Medium, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "giant frog", Symbol = 'f', Color = ConsoleColor.DarkGreen, Hp = 16, Attack = 5, Defense = 2, Speed = 9, Mana = 0, Agility = 10, DifficultyRating = 0.35, CreatureType = CreatureType.Animal,
                ShortDescription = "A bloated frog large enough to swallow a small creature.",
                LongDescription = "Its throat bulges as it croaks loudly, while its powerful hind legs tense beneath its enormous body.",
                Size = Size.Small, AttackType = AttackType.Bite, SoundType = MonsterSoundType.Croak
            },
            new()
            {
                Name = "skeleton", Symbol = 'S', Color = ConsoleColor.White, Hp = 16, Attack = 5, Defense = 3, Speed = 8, Mana = 0, Agility = 8, DifficultyRating = 0.45, CreatureType = CreatureType.Undead,
                ShortDescription = "A rattling skeleton carrying a battered weapon.",
                LongDescription = "Ancient bones held together by dark magic shift beneath scraps of rotted armor as empty eye sockets stare toward you.",
                Size = Size.Medium, AttackType = AttackType.Hit, SoundType = MonsterSoundType.Rattle
            },
            new()
            {
                Name = "giant spider", Symbol = 'P', Color = ConsoleColor.DarkMagenta, Hp = 20, Attack = 6, Defense = 3, Speed = 11, Mana = 12, Agility = 12, DifficultyRating = 0.55, CreatureType = CreatureType.Animal,
                ShortDescription = "A bulbous spider skittering across the wall.",
                LongDescription = "Its bulbous abdomen glistens with venom, and eight glassy eyes track your every movement.",
                Size = Size.Medium, AttackType = AttackType.Sting, SoundType = MonsterSoundType.Skitter
            },
            new()
            {
                Name = "goblin warrior", Symbol = 'G', Color = ConsoleColor.Green, Hp = 17, Attack = 6, Defense = 3, Speed = 11, Mana = 0, Agility = 10, DifficultyRating = 0.50, CreatureType = CreatureType.Humanoid,
                ShortDescription = "A battle-ready goblin carrying a crude weapon.",
                LongDescription = "Unlike its scavenging kin, this goblin carries itself with confidence and keeps its weapon raised as it advances.",
                Size = Size.Medium, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "goblin archer", Symbol = 'G', Color = ConsoleColor.DarkGreen, Hp = 14, Attack = 4, Defense = 2, Speed = 11, Mana = 0, Agility = 12, DifficultyRating = 0.55, CreatureType = CreatureType.Humanoid,
                ShortDescription = "A goblin with a drawn bow, keeping its distance.",
                LongDescription = "The goblin keeps a nocked arrow trained on you, circling to keep its distance rather than closing in.",
                Size = Size.Medium, AttackType = AttackType.Hit, PreferRangedAttack = true
            },
            new()
            {
                Name = "giant snake", Symbol = 'n', Color = ConsoleColor.DarkGreen, Hp = 18, Attack = 7, Defense = 2, Speed = 13, Mana = 0, Agility = 13, DifficultyRating = 0.50, CreatureType = CreatureType.Animal,
                ShortDescription = "A thick serpent with gleaming scales.",
                LongDescription = "The enormous snake coils across the dungeon floor, its tongue flicking through the air as it searches for your scent.",
                Size = Size.Small, AttackType = AttackType.Bite, SoundType = MonsterSoundType.Hiss
            },
            new()
            {
                Name = "zombie", Symbol = 'Z', Color = ConsoleColor.DarkGray, Hp = 25, Attack = 7, Defense = 3, Speed = 5, Mana = 0, Agility = 5, DifficultyRating = 0.60, CreatureType = CreatureType.Undead,
                ShortDescription = "A shambling corpse covered in rotting clothes.",
                LongDescription = "The corpse lurches forward with unnatural determination, barely resembling the person it once was.",
                Size = Size.Medium, AttackType = AttackType.Hit, SoundType = MonsterSoundType.Moan
            },
            new()
            {
                Name = "hobgoblin scout", Symbol = 'h', Color = ConsoleColor.DarkYellow, Hp = 20, Attack = 7, Defense = 3, Speed = 13, Mana = 0, Agility = 12, DifficultyRating = 0.60, CreatureType = CreatureType.Humanoid,
                ShortDescription = "A lean goblinoid carrying a short blade.",
                LongDescription = "The scout moves with surprising speed, constantly shifting its position while searching for an opening.",
                Size = Size.Medium, AttackType = AttackType.Hit, SoundType = MonsterSoundType.Footsteps
            },
            new()
            {
                Name = "giant leech", Symbol = 'e', Color = ConsoleColor.DarkRed, Hp = 22, Attack = 6, Defense = 2, Speed = 8, Mana = 0, Agility = 10, DifficultyRating = 0.55, CreatureType = CreatureType.Animal,
                ShortDescription = "A bloated leech with a circular maw.",
                LongDescription = "Its slick body contracts and expands as it crawls toward you, drawn by the warmth of your blood.",
                Size = Size.Small, AttackType = AttackType.Bite
            },
            new()
            {
                Name = "orc", Symbol = 'o', Color = ConsoleColor.DarkGreen, Hp = 22, Attack = 8, Defense = 4, Speed = 8, Mana = 0, Agility = 6, DifficultyRating = 0.70, CreatureType = CreatureType.Humanoid,
                ShortDescription = "A broad-shouldered green-skinned warrior.",
                LongDescription = "The orc towers over you, its scarred hide covered in crude armor as it raises its weapon with obvious intent to kill.",
                Size = Size.Medium, AttackType = AttackType.Hit, SoundType = MonsterSoundType.Growl
            },
            new()
            {
                Name = "dark acolyte", Symbol = 'a', Color = ConsoleColor.DarkMagenta, Hp = 18, Attack = 5, Defense = 3, Speed = 9, Mana = 12, Agility = 10, DifficultyRating = 0.65, CreatureType = CreatureType.Humanoid,
                ShortDescription = "A robed cultist muttering strange prayers.",
                LongDescription = "The acolyte whispers beneath its hood as faint magical energy gathers around its hands.",
                Size = Size.Medium, AttackType = AttackType.Hit, SoundType = MonsterSoundType.Chanting
            },
            new()
            {
                Name = "dire rat", Symbol = 'R', Color = ConsoleColor.DarkGray, Hp = 20, Attack = 7, Defense = 2, Speed = 14, Mana = 0, Agility = 14, DifficultyRating = 0.55, CreatureType = CreatureType.Animal,
                ShortDescription = "A vicious rat nearly the size of a dog.",
                LongDescription = "Its scarred hide bristles as it bares yellowed teeth and charges with surprising speed.",
                Size = Size.Small, AttackType = AttackType.Bite
            },
            new()
            {
                Name = "giant beetle", Symbol = 'B', Color = ConsoleColor.DarkYellow, Hp = 24, Attack = 7, Defense = 5, Speed = 7, Mana = 0, Agility = 8, DifficultyRating = 0.60, CreatureType = CreatureType.Animal,
                ShortDescription = "A heavily armored beetle the size of a dog.",
                LongDescription = "Its thick shell absorbs the dim dungeon light as its mandibles click together with startling force.",
                Size = Size.Small, AttackType = AttackType.Bite
            },
            new()
            {
                Name = "orc warrior", Symbol = 'O', Color = ConsoleColor.DarkGreen, Hp = 28, Attack = 10, Defense = 5, Speed = 8, Mana = 0, Agility = 6, DifficultyRating = 0.80, CreatureType = CreatureType.Humanoid,
                ShortDescription = "A heavily armed orc warrior.",
                LongDescription = "Thick muscles strain beneath crude armor as the orc advances with a heavy weapon held ready.",
                Size = Size.Medium, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "giant scorpion", Symbol = 'X', Color = ConsoleColor.DarkYellow, Hp = 30, Attack = 10, Defense = 5, Speed = 9, Mana = 0, Agility = 10, DifficultyRating = 0.85, CreatureType = CreatureType.Animal,
                ShortDescription = "A massive scorpion with raised pincers.",
                LongDescription = "Its armored body scrapes against the stone as its enormous pincers open and close beneath a poised venomous stinger.",
                Size = Size.Small, AttackType = AttackType.Sting, SoundType = MonsterSoundType.Scraping
            },
            new()
            {
                Name = "hobgoblin warrior", Symbol = 'H', Color = ConsoleColor.Red, Hp = 28, Attack = 9, Defense = 5, Speed = 9, Mana = 0, Agility = 8, DifficultyRating = 0.80, CreatureType = CreatureType.Humanoid,
                ShortDescription = "A disciplined goblinoid warrior in battered armor.",
                LongDescription = "Unlike its smaller kin, this goblinoid moves with purpose and discipline, carrying a well-maintained weapon and battered shield.",
                Size = Size.Medium, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "ghoul", Symbol = 'q', Color = ConsoleColor.DarkGray, Hp = 27, Attack = 9, Defense = 4, Speed = 11, Mana = 0, Agility = 11, DifficultyRating = 0.80, CreatureType = CreatureType.Undead,
                ShortDescription = "A corpse-eating undead creature with long claws.",
                LongDescription = "Its hunched body twitches with unnatural hunger as it sniffs the air and fixes its eyes upon you.",
                Size = Size.Medium, AttackType = AttackType.Bite, SoundType = MonsterSoundType.Moan
            },
            new()
            {
                Name = "cultist", Symbol = 'u', Color = ConsoleColor.DarkRed, Hp = 22, Attack = 6, Defense = 3, Speed = 9, Mana = 14, Agility = 10, DifficultyRating = 0.85, CreatureType = CreatureType.Humanoid,
                ShortDescription = "A robed figure muttering strange words.",
                LongDescription = "The cultist's hood conceals most of its face, but its whispered words carry an unsettling rhythm as strange symbols glow faintly upon its robes.",
                Size = Size.Medium, AttackType = AttackType.Hit, SoundType = MonsterSoundType.Chanting
            },
            new()
            {
                Name = "giant wolf spider", Symbol = 'w', Color = ConsoleColor.DarkMagenta, Hp = 26, Attack = 9, Defense = 3, Speed = 14, Mana = 8, Agility = 14, DifficultyRating = 0.75, CreatureType = CreatureType.Animal,
                ShortDescription = "A fast spider with long powerful legs.",
                LongDescription = "The spider darts across the floor with alarming speed, its numerous eyes following your every movement.",
                Size = Size.Small, AttackType = AttackType.Sting, SoundType = MonsterSoundType.Skitter
            },
            new()
            {
                Name = "ogre brute", Symbol = 'Y', Color = ConsoleColor.DarkYellow, Hp = 40, Attack = 12, Defense = 4, Speed = 6, Mana = 0, Agility = 5, DifficultyRating = 0.90, CreatureType = CreatureType.Humanoid,
                ShortDescription = "A hulking brute carrying a crude club.",
                LongDescription = "The enormous humanoid swings its crude club experimentally, apparently unconcerned by the damage it could cause.",
                Size = Size.Large, AttackType = AttackType.Hit, SoundType = MonsterSoundType.HeavyFootsteps
            },
            new()
            {
                Name = "skeleton archer", Symbol = 'A', Color = ConsoleColor.White, Hp = 22, Attack = 8, Defense = 3, Speed = 9, Mana = 0, Agility = 10, DifficultyRating = 0.80, CreatureType = CreatureType.Undead,
                ShortDescription = "A skeletal archer clutching an ancient bow.",
                LongDescription = "Its skeletal fingers draw the bowstring with practiced precision despite the weapon's age.",
                Size = Size.Medium, AttackType = AttackType.Hit, SoundType = MonsterSoundType.Rattle
            },
            new()
            {
                Name = "fire beetle", Symbol = 'F', Color = ConsoleColor.Red, Hp = 25, Attack = 9, Defense = 4, Speed = 8, Mana = 0, Agility = 9, DifficultyRating = 0.75, CreatureType = CreatureType.Animal,
                ShortDescription = "A beetle glowing with an eerie internal heat.",
                LongDescription = "A faint red glow shines through cracks in its armored shell as it crawls toward you.",
                Size = Size.Small, AttackType = AttackType.Bite,
                // Tile-attuned monster system, design spec section 35 -- converted in place
                // rather than duplicated; combat stats otherwise unchanged.
                PreferredFloorType = FloorType.Fire, ElementalAffinity = DamageType.Fire
            },
            new()
            {
                Name = "swamp hag", Symbol = 'W', Color = ConsoleColor.DarkGreen, Hp = 24, Attack = 7, Defense = 4, Speed = 7, Mana = 18, Agility = 9, DifficultyRating = 0.85, CreatureType = CreatureType.Humanoid,
                ShortDescription = "A hunched old creature with tangled hair.",
                LongDescription = "The hag's crooked smile reveals too many teeth as she whispers to herself and watches you with ancient eyes.",
                Size = Size.Medium, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "wight", Symbol = 'W', Color = ConsoleColor.DarkCyan, Hp = 35, Attack = 11, Defense = 6, Speed = 10, Mana = 0, Agility = 10, DifficultyRating = 1.00, CreatureType = CreatureType.Undead,
                ShortDescription = "A gaunt undead warrior wrapped in ancient armor.",
                LongDescription = "A pale corpse-like figure advances with the cold patience of something that has not known fear or mercy for centuries.",
                Size = Size.Medium, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "orc berserker", Symbol = 'Q', Color = ConsoleColor.DarkGreen, Hp = 38, Attack = 13, Defense = 4, Speed = 10, Mana = 0, Agility = 8, DifficultyRating = 1.00, CreatureType = CreatureType.Humanoid,
                ShortDescription = "A furious orc warrior covered in scars.",
                LongDescription = "The berserker seems barely capable of containing its rage as it charges forward with reckless determination.",
                Size = Size.Medium, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "ogre", Symbol = 'T', Color = ConsoleColor.DarkYellow, Hp = 48, Attack = 13, Defense = 5, Speed = 6, Mana = 0, Agility = 5, DifficultyRating = 1.05, CreatureType = CreatureType.Humanoid,
                ShortDescription = "A towering humanoid wielding a massive club.",
                LongDescription = "The enormous creature towers above you, its crude armor barely covering its thick hide as it hefts a club nearly as large as you are.",
                Size = Size.Large, AttackType = AttackType.Hit, SoundType = MonsterSoundType.HeavyFootsteps
            },
            new()
            {
                Name = "dire wolf", Symbol = 'D', Color = ConsoleColor.Gray, Hp = 38, Attack = 12, Defense = 4, Speed = 13, Mana = 0, Agility = 12, DifficultyRating = 0.95, CreatureType = CreatureType.Animal,
                ShortDescription = "A massive wolf with predatory eyes.",
                LongDescription = "Far larger than a normal wolf, the beast circles with practiced confidence, its lips pulling back to reveal rows of long sharp teeth.",
                Size = Size.Large, AttackType = AttackType.Bite, SoundType = MonsterSoundType.Howl
            },
            new()
            {
                Name = "giant constrictor", Symbol = 'N', Color = ConsoleColor.DarkGreen, Hp = 42, Attack = 13, Defense = 5, Speed = 9, Mana = 0, Agility = 11, DifficultyRating = 1.00, CreatureType = CreatureType.Animal,
                ShortDescription = "An enormous serpent with powerful coils.",
                LongDescription = "The serpent's muscular body fills much of the passage as it slowly coils around itself and prepares to strike.",
                Size = Size.Large, AttackType = AttackType.Bite, SoundType = MonsterSoundType.Hiss
            },
            new()
            {
                Name = "shadow", Symbol = 'd', Color = ConsoleColor.DarkGray, Hp = 28, Attack = 10, Defense = 4, Speed = 14, Mana = 14, Agility = 14, DifficultyRating = 0.95, CreatureType = CreatureType.Undead,
                ShortDescription = "A humanoid shadow detached from any living body.",
                LongDescription = "The darkness moves independently of the dungeon's light, forming vague limbs that reach toward you.",
                Size = Size.Medium, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "dark priest", Symbol = 'P', Color = ConsoleColor.DarkMagenta, Hp = 38, Attack = 8, Defense = 5, Speed = 8, Mana = 22, Agility = 11, DifficultyRating = 1.05, CreatureType = CreatureType.Humanoid,
                ShortDescription = "A sinister priest wrapped in dark ceremonial robes.",
                LongDescription = "A cold presence surrounds the priest as it raises one hand and begins whispering words that make the air itself feel wrong.",
                Size = Size.Medium, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "cave troll", Symbol = 't', Color = ConsoleColor.DarkGreen, Hp = 52, Attack = 13, Defense = 6, Speed = 6, Mana = 0, Agility = 6, DifficultyRating = 1.05, CreatureType = CreatureType.Humanoid,
                ShortDescription = "A scarred troll adapted to the darkness.",
                LongDescription = "The troll's long arms nearly scrape the floor as it advances, old wounds already beginning to close across its thick hide.",
                Size = Size.Large, AttackType = AttackType.Hit, SoundType = MonsterSoundType.HeavyFootsteps
            },
            new()
            {
                Name = "assassin", Symbol = 'x', Color = ConsoleColor.DarkGray, Hp = 32, Attack = 14, Defense = 4, Speed = 15, Mana = 0, Agility = 14, DifficultyRating = 1.00, CreatureType = CreatureType.Humanoid,
                ShortDescription = "A silent killer armed with poisoned blades.",
                LongDescription = "The assassin remains almost perfectly still until your attention shifts, then suddenly glides forward with deadly purpose.",
                Size = Size.Medium, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "ghast", Symbol = 'V', Color = ConsoleColor.DarkGray, Hp = 34, Attack = 12, Defense = 5, Speed = 12, Mana = 0, Agility = 12, DifficultyRating = 1.05, CreatureType = CreatureType.Undead,
                ShortDescription = "A foul undead predator with elongated claws.",
                LongDescription = "Its corpse-like body reeks of decay as it crouches low, ready to spring upon anything living.",
                Size = Size.Medium, AttackType = AttackType.Bite
            },
            new()
            {
                Name = "minotaur", Symbol = 'M', Color = ConsoleColor.Red, Hp = 65, Attack = 17, Defense = 7, Speed = 10, Mana = 0, Agility = 8, DifficultyRating = 1.15, CreatureType = CreatureType.Humanoid,
                ShortDescription = "A towering bull-headed warrior.",
                LongDescription = "The creature's muscular frame is covered in scars, and its horned head lowers as it prepares to charge.",
                Size = Size.Large, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "troll", Symbol = 'T', Color = ConsoleColor.DarkGreen, Hp = 60, Attack = 15, Defense = 6, Speed = 7, Mana = 0, Agility = 7, DifficultyRating = 1.15, CreatureType = CreatureType.Humanoid,
                ShortDescription = "A towering creature with gray-green skin.",
                LongDescription = "The troll's long arms hang almost to the floor, and old wounds scar its thick hide as it advances with a hungry growl.",
                Size = Size.Large, AttackType = AttackType.Hit, SoundType = MonsterSoundType.HeavyFootsteps
            },
            new()
            {
                Name = "wraith", Symbol = 'V', Color = ConsoleColor.DarkGray, Hp = 45, Attack = 14, Defense = 5, Speed = 14, Mana = 18, Agility = 14, DifficultyRating = 1.15, CreatureType = CreatureType.Undead,
                ShortDescription = "A translucent spirit drifting above the floor.",
                LongDescription = "A vaguely humanoid shape floats within tattered darkness, its indistinct features shifting as though seen through deep water.",
                Size = Size.Medium, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "vampire spawn", Symbol = 'v', Color = ConsoleColor.DarkRed, Hp = 42, Attack = 13, Defense = 7, Speed = 12, Mana = 0, Agility = 13, DifficultyRating = 1.10, CreatureType = CreatureType.Undead,
                ShortDescription = "A pale undead creature with bloodstained clothing.",
                LongDescription = "Its unnatural movements are graceful and deliberate, and its pale eyes remain fixed upon your neck as it advances.",
                Size = Size.Medium, AttackType = AttackType.Bite
            },
            new()
            {
                Name = "stone golem", Symbol = 'G', Color = ConsoleColor.Gray, Hp = 75, Attack = 16, Defense = 10, Speed = 5, Mana = 0, Agility = 4, DifficultyRating = 1.20,
                ShortDescription = "A massive humanoid carved from stone.",
                LongDescription = "Ancient runes cover the creature's stone body as it moves with deliberate grinding steps that echo through the dungeon.",
                Size = Size.Large, AttackType = AttackType.Hit, SoundType = MonsterSoundType.StoneGrinding
            },
            new()
            {
                Name = "flame hound", Symbol = 'h', Color = ConsoleColor.Red, Hp = 45, Attack = 15, Defense = 5, Speed = 13, Mana = 16, Agility = 12, DifficultyRating = 1.10, CreatureType = CreatureType.Animal,
                ShortDescription = "A monstrous hound wreathed in flame.",
                LongDescription = "Heat shimmers around the beast as flames lick across its dark hide and smoke curls from its open jaws.",
                Size = Size.Medium, AttackType = AttackType.Bite
            },
            new()
            {
                Name = "giant scorpion queen", Symbol = 'C', Color = ConsoleColor.DarkYellow, Hp = 55, Attack = 16, Defense = 7, Speed = 9, Mana = 0, Agility = 11, DifficultyRating = 1.15, CreatureType = CreatureType.Animal,
                ShortDescription = "An enormous armored scorpion with a massive stinger.",
                LongDescription = "The queen's plated body nearly fills the passage as several smaller scorpions crawl over its armored back.",
                Size = Size.Large, AttackType = AttackType.Sting
            },
            new()
            {
                Name = "necromancer", Symbol = 'n', Color = ConsoleColor.DarkMagenta, Hp = 36, Attack = 7, Defense = 5, Speed = 8, Mana = 28, Agility = 12, DifficultyRating = 1.15, CreatureType = CreatureType.Humanoid,
                ShortDescription = "A robed mage surrounded by drifting bone fragments.",
                LongDescription = "The necromancer raises a skeletal hand as fragments of bone orbit its body and pale magic flickers between its fingers.",
                Size = Size.Medium, AttackType = AttackType.Hit, SoundType = MonsterSoundType.Chanting
            },
            new()
            {
                Name = "werewolf", Symbol = 'Y', Color = ConsoleColor.DarkGray, Hp = 50, Attack = 16, Defense = 6, Speed = 14, Mana = 0, Agility = 14, DifficultyRating = 1.20, CreatureType = CreatureType.Humanoid,
                ShortDescription = "A towering wolf-like humanoid with savage claws.",
                LongDescription = "The creature's lupine head turns toward you as it flexes enormous claws and releases a low threatening growl.",
                Size = Size.Large, AttackType = AttackType.Bite, SoundType = MonsterSoundType.Howl
            },
            new()
            {
                Name = "carrion crawler", Symbol = 'C', Color = ConsoleColor.DarkGreen, Hp = 48, Attack = 12, Defense = 6, Speed = 8, Mana = 0, Agility = 10, DifficultyRating = 1.10, CreatureType = CreatureType.Animal,
                ShortDescription = "A long segmented creature with rows of legs.",
                LongDescription = "Its many legs carry the bloated creature across the walls while a cluster of twitching feelers surrounds its mouth.",
                Size = Size.Large, AttackType = AttackType.Bite
            },
            new()
            {
                Name = "mummy", Symbol = 'm', Color = ConsoleColor.Yellow, Hp = 55, Attack = 15, Defense = 8, Speed = 7, Mana = 0, Agility = 7, DifficultyRating = 1.20, CreatureType = CreatureType.Undead,
                ShortDescription = "An ancient corpse wrapped in dusty bandages.",
                LongDescription = "Ancient bandages hang from a desiccated body as it advances with silent purpose, carrying the lingering curse of a forgotten tomb.",
                Size = Size.Medium, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "fire elemental", Symbol = 'E', Color = ConsoleColor.Red, Hp = 50, Attack = 18, Defense = 6, Speed = 12, Mana = 25, Agility = 13, DifficultyRating = 1.25,
                ShortDescription = "A humanoid shape formed entirely from roaring flame.",
                LongDescription = "Flames twist into the rough shape of a humanoid figure, radiating intense heat as burning embers swirl around its body.",
                Size = Size.Large, AttackType = AttackType.Hit,
                // Resistance System spec section 38's own worked example.
                BaseResistances = new ResistanceSet(fire: 75, water: -40, ice: -25)
            },
            new()
            {
                Name = "frost wraith", Symbol = 'I', Color = ConsoleColor.Cyan, Hp = 48, Attack = 15, Defense = 7, Speed = 13, Mana = 22, Agility = 13, DifficultyRating = 1.20, CreatureType = CreatureType.Undead,
                ShortDescription = "A ghostly figure surrounded by freezing mist.",
                LongDescription = "Frost gathers wherever the spirit passes, coating nearby stone with a thin layer of ice.",
                Size = Size.Medium, AttackType = AttackType.Hit,
                // Resistance System spec section 38's own worked example.
                BaseResistances = new ResistanceSet(fire: -25, ice: 60)
            },
            new()
            {
                Name = "ogre mage", Symbol = 'J', Color = ConsoleColor.DarkMagenta, Hp = 58, Attack = 13, Defense = 6, Speed = 6, Mana = 30, Agility = 8, DifficultyRating = 1.25, CreatureType = CreatureType.Humanoid,
                ShortDescription = "A massive ogre capable of wielding crude magic.",
                LongDescription = "Symbols are painted across the ogre's enormous body as it mutters words that cause strange sparks to leap from its hands.",
                Size = Size.Large, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "werebear", Symbol = 'B', Color = ConsoleColor.DarkYellow, Hp = 65, Attack = 18, Defense = 8, Speed = 9, Mana = 0, Agility = 10, DifficultyRating = 1.25, CreatureType = CreatureType.Humanoid,
                ShortDescription = "A gigantic bear-like humanoid covered in thick fur.",
                LongDescription = "The enormous beast rises onto its hind legs and roars, revealing claws capable of tearing through heavy armor.",
                Size = Size.Large, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "death knight initiate", Symbol = 'K', Color = ConsoleColor.DarkGray, Hp = 60, Attack = 18, Defense = 8, Speed = 10, Mana = 20, Agility = 9, DifficultyRating = 1.25, CreatureType = CreatureType.Undead,
                ShortDescription = "A heavily armored undead warrior.",
                LongDescription = "Blackened armor conceals a corpse sustained by unnatural forces as the knight advances without hesitation.",
                Size = Size.Large, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "medusa", Symbol = 'N', Color = ConsoleColor.DarkGreen, Hp = 48, Attack = 15, Defense = 6, Speed = 11, Mana = 20, Agility = 12, DifficultyRating = 1.25, CreatureType = CreatureType.Humanoid,
                ShortDescription = "A serpent-haired humanoid carrying a deadly gaze.",
                LongDescription = "Snakes writhe where hair should be as the creature watches you with an unsettling, almost hypnotic stare.",
                Size = Size.Medium, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "greater ghoul", Symbol = 'Q', Color = ConsoleColor.DarkGray, Hp = 55, Attack = 17, Defense = 7, Speed = 13, Mana = 0, Agility = 13, DifficultyRating = 1.25, CreatureType = CreatureType.Undead,
                ShortDescription = "A powerful undead predator with enormous claws.",
                LongDescription = "The creature moves with disturbing speed for something so gaunt, its claws scraping against the stone as it closes the distance.",
                Size = Size.Medium, AttackType = AttackType.Bite
            },
            new()
            {
                Name = "lich", Symbol = 'L', Color = ConsoleColor.DarkMagenta, Hp = 65, Attack = 16, Defense = 9, Speed = 9, Mana = 35, Agility = 14, DifficultyRating = 1.30, CreatureType = CreatureType.Undead,
                ShortDescription = "An ancient undead sorcerer wrapped in decayed robes.",
                LongDescription = "Ancient magic clings to the lich like a physical presence as its hollow gaze studies you with the patience of something that has defeated death itself.",
                Size = Size.Medium, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "frost giant", Symbol = 'F', Color = ConsoleColor.Cyan, Hp = 90, Attack = 20, Defense = 9, Speed = 7, Mana = 0, Agility = 6, DifficultyRating = 1.35, CreatureType = CreatureType.Humanoid,
                ShortDescription = "A towering giant with pale blue skin.",
                LongDescription = "The giant's pale skin seems almost blue in the dungeon gloom, and each slow step shakes dust from the ceiling as it approaches.",
                Size = Size.Large, AttackType = AttackType.Hit,
                // Resistance System spec section 39 -- merely cold-themed rather than physically
                // dependent on ICE terrain (deliberately NOT made floor-attuned, per that
                // section's own guidance), but still innately resistant/vulnerable.
                BaseResistances = new ResistanceSet(fire: -15, ice: 40)
            },
            new()
            {
                Name = "greater demon", Symbol = 'D', Color = ConsoleColor.DarkRed, Hp = 80, Attack = 21, Defense = 8, Speed = 11, Mana = 28, Agility = 12, DifficultyRating = 1.40,
                ShortDescription = "A towering demonic creature covered in dark armor.",
                LongDescription = "The creature's unnatural form radiates hostility as dark energy flickers across its body and its eyes burn with an otherworldly light.",
                Size = Size.Large, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "black knight", Symbol = 'K', Color = ConsoleColor.Black, Hp = 72, Attack = 20, Defense = 11, Speed = 8, Mana = 0, Agility = 8, DifficultyRating = 1.30, CreatureType = CreatureType.Humanoid,
                ShortDescription = "A heavily armored knight wielding a darkened weapon.",
                LongDescription = "The knight's armor absorbs the dungeon's light as it advances in complete silence, its weapon held in both hands.",
                Size = Size.Large, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "storm elemental", Symbol = 'R', Color = ConsoleColor.Cyan, Hp = 58, Attack = 20, Defense = 7, Speed = 15, Mana = 30, Agility = 15, DifficultyRating = 1.30,
                ShortDescription = "A swirling humanoid shape filled with crackling energy.",
                LongDescription = "Lightning arcs through the creature's translucent form as winds spiral around it and fill the chamber with a low rumble.",
                Size = Size.Large, AttackType = AttackType.Hit,
                BaseResistances = new ResistanceSet(shock: 65)
            },
            new()
            {
                Name = "ancient vampire", Symbol = 'V', Color = ConsoleColor.DarkRed, Hp = 70, Attack = 20, Defense = 10, Speed = 14, Mana = 30, Agility = 15, DifficultyRating = 1.35, CreatureType = CreatureType.Undead,
                ShortDescription = "An ancient undead noble with an unnerving smile.",
                LongDescription = "The vampire's immaculate appearance does little to conceal the predatory hunger behind its ancient eyes.",
                Size = Size.Medium, AttackType = AttackType.Bite
            },
            new()
            {
                Name = "iron golem", Symbol = 'I', Color = ConsoleColor.Gray, Hp = 100, Attack = 22, Defense = 14, Speed = 4, Mana = 0, Agility = 3, DifficultyRating = 1.40,
                ShortDescription = "A towering construct made from plates of iron.",
                LongDescription = "Massive iron plates grind against one another as the construct advances with mechanical precision.",
                Size = Size.Large, AttackType = AttackType.Hit, SoundType = MonsterSoundType.StoneGrinding
            },
            new()
            {
                Name = "purple worm", Symbol = 'W', Color = ConsoleColor.DarkMagenta, Hp = 110, Attack = 23, Defense = 10, Speed = 8, Mana = 0, Agility = 6, DifficultyRating = 1.40, CreatureType = CreatureType.Animal,
                ShortDescription = "An enormous subterranean worm with a gaping maw.",
                LongDescription = "The massive creature erupts from the dungeon floor, its segmented body filling the chamber as its enormous jaws open wide.",
                Size = Size.Large, AttackType = AttackType.Bite
            },
            new()
            {
                Name = "hydra", Symbol = 'H', Color = ConsoleColor.DarkGreen, Hp = 125, Attack = 23, Defense = 11, Speed = 9, Mana = 0, Agility = 9, DifficultyRating = 1.45, CreatureType = CreatureType.Animal,
                ShortDescription = "A massive many-headed reptilian monster.",
                LongDescription = "Several serpentine heads sway independently atop the creature's enormous body, each watching you with hungry anticipation.",
                Size = Size.Large, AttackType = AttackType.Bite
            },
            new()
            {
                Name = "ancient mummy", Symbol = 'M', Color = ConsoleColor.Yellow, Hp = 85, Attack = 21, Defense = 11, Speed = 8, Mana = 20, Agility = 8, DifficultyRating = 1.40, CreatureType = CreatureType.Undead,
                ShortDescription = "An ancient undead wrapped in enchanted bandages.",
                LongDescription = "The mummy's ancient wrappings glow faintly with cursed symbols as it slowly raises one hand toward you.",
                Size = Size.Medium, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "lava elemental", Symbol = 'E', Color = ConsoleColor.Red, Hp = 85, Attack = 25, Defense = 9, Speed = 9, Mana = 35, Agility = 11, DifficultyRating = 1.45,
                ShortDescription = "A towering creature formed from molten rock.",
                LongDescription = "Cracks across its rocky body glow with intense heat as molten material drips from its massive limbs.",
                Size = Size.Large, AttackType = AttackType.Hit,
                // Tile-attuned monster system, design spec section 36 -- an excellent candidate
                // for physical terrain dependency, unlike e.g. Frost Giant/Storm Elemental
                // (merely themed, not converted -- see section 36's own guidance).
                PreferredFloorType = FloorType.Lava, ElementalAffinity = DamageType.Fire,
                // Resistance System spec section 39 -- an explicit innate bonus on top of (not
                // instead of) the floor-attunement environmental swing above; a body of literal
                // magma is fire-resistant everywhere, not only while standing on Lava.
                BaseResistances = new ResistanceSet(fire: 50)
            },
            new()
            {
                Name = "pit fiend", Symbol = 'P', Color = ConsoleColor.DarkRed, Hp = 100, Attack = 25, Defense = 10, Speed = 12, Mana = 40, Agility = 13, DifficultyRating = 1.45,
                ShortDescription = "A massive horned demon radiating terrible heat.",
                LongDescription = "Great horns rise from its armored head as heat and dark energy radiate from its enormous body.",
                Size = Size.Large, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "ancient lich", Symbol = 'A', Color = ConsoleColor.DarkMagenta, Hp = 90, Attack = 20, Defense = 12, Speed = 10, Mana = 50, Agility = 15, DifficultyRating = 1.45, CreatureType = CreatureType.Undead,
                ShortDescription = "A lich whose power has grown across centuries.",
                LongDescription = "The undead sorcerer radiates centuries of accumulated magical power as countless fragments of ancient relics float around it.",
                Size = Size.Medium, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "death knight", Symbol = 'K', Color = ConsoleColor.DarkGray, Hp = 95, Attack = 24, Defense = 12, Speed = 10, Mana = 30, Agility = 9, DifficultyRating = 1.45, CreatureType = CreatureType.Undead,
                ShortDescription = "A heavily armored undead warrior radiating dark power.",
                LongDescription = "Blackened armor conceals a body sustained by unnatural forces as the knight advances with the relentless purpose of an executioner.",
                Size = Size.Large, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "shadow dragon", Symbol = 'S', Color = ConsoleColor.DarkGray, Hp = 130, Attack = 25, Defense = 11, Speed = 13, Mana = 45, Agility = 13, DifficultyRating = 1.50,
                ShortDescription = "A dragon whose scales seem to absorb the surrounding light.",
                LongDescription = "The dragon's scales appear to drink in the darkness as its vast wings unfold and ancient eyes settle upon you.",
                Size = Size.Large, AttackType = AttackType.Bite, SoundType = MonsterSoundType.Roar
            },
            new()
            {
                Name = "titan", Symbol = 'T', Color = ConsoleColor.White, Hp = 140, Attack = 27, Defense = 13, Speed = 8, Mana = 0, Agility = 7, DifficultyRating = 1.50, CreatureType = CreatureType.Humanoid,
                ShortDescription = "A colossal humanoid warrior carrying an enormous weapon.",
                LongDescription = "The enormous warrior towers over the dungeon chamber, each movement sending vibrations through the stone beneath your feet.",
                Size = Size.Large, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "ancient dragon", Symbol = 'd', Color = ConsoleColor.DarkYellow, Hp = 140, Attack = 25, Defense = 12, Speed = 10, Mana = 40, Agility = 10, DifficultyRating = 1.55,
                ShortDescription = "An enormous dragon covered in ancient scales.",
                LongDescription = "Massive wings fold against its armored body as ancient eyes regard you with calculating intelligence, and the heat of its breath fills the chamber.",
                Size = Size.Large, AttackType = AttackType.Bite, SoundType = MonsterSoundType.Roar
            },
            new()
            {
                Name = "demon lord", Symbol = 'B', Color = ConsoleColor.DarkMagenta, Hp = 120, Attack = 27, Defense = 11, Speed = 12, Mana = 45, Agility = 13, DifficultyRating = 1.55,
                ShortDescription = "A towering demon whose presence fills the chamber with dread.",
                LongDescription = "The enormous creature seems almost too large for the chamber, surrounded by an oppressive aura of darkness that bends the air around it.",
                Size = Size.Large, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "lich king", Symbol = 'L', Color = ConsoleColor.DarkMagenta, Hp = 115, Attack = 23, Defense = 14, Speed = 10, Mana = 65, Agility = 16, DifficultyRating = 1.55, CreatureType = CreatureType.Undead,
                ShortDescription = "An undead monarch wielding devastating magical power.",
                LongDescription = "A crown of blackened metal rests upon the lich's ancient skull as waves of magic distort the air around its throne-like presence.",
                Size = Size.Medium, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "balor", Symbol = 'X', Color = ConsoleColor.Red, Hp = 150, Attack = 29, Defense = 12, Speed = 13, Mana = 50, Agility = 14, DifficultyRating = 1.60,
                ShortDescription = "A gigantic demon wielding terrible supernatural power.",
                LongDescription = "Flames and darkness coil around the demon's immense form as it towers over the chamber and raises its weapon.",
                Size = Size.Large, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "behemoth", Symbol = 'B', Color = ConsoleColor.DarkYellow, Hp = 170, Attack = 30, Defense = 14, Speed = 7, Mana = 0, Agility = 7, DifficultyRating = 1.60, CreatureType = CreatureType.Animal,
                ShortDescription = "An enormous beast covered in thick natural armor.",
                LongDescription = "The creature's massive frame seems to fill the entire passage as it lowers its head and prepares to charge.",
                Size = Size.Large, AttackType = AttackType.Bite
            },
            new()
            {
                Name = "ancient frost giant", Symbol = 'F', Color = ConsoleColor.Cyan, Hp = 135, Attack = 27, Defense = 13, Speed = 8, Mana = 30, Agility = 7, DifficultyRating = 1.55, CreatureType = CreatureType.Humanoid,
                ShortDescription = "A colossal frost giant carrying an enormous weapon.",
                LongDescription = "Ice clings to the giant's pale armor as freezing winds swirl around its massive body.",
                Size = Size.Large, AttackType = AttackType.Hit,
                BaseResistances = new ResistanceSet(fire: -15, ice: 45)
            },
            new()
            {
                Name = "abyssal horror", Symbol = 'A', Color = ConsoleColor.DarkBlue, Hp = 125, Attack = 26, Defense = 12, Speed = 11, Mana = 55, Agility = 14, DifficultyRating = 1.55,
                ShortDescription = "A twisted creature from an otherworldly abyss.",
                LongDescription = "Its alien form seems to shift between several shapes as impossible eyes open across its body.",
                Size = Size.Large, AttackType = AttackType.Bite
            },
            new()
            {
                Name = "dread knight", Symbol = 'K', Color = ConsoleColor.Black, Hp = 120, Attack = 28, Defense = 15, Speed = 9, Mana = 35, Agility = 10, DifficultyRating = 1.55, CreatureType = CreatureType.Undead,
                ShortDescription = "A monstrous undead knight clad in black armor.",
                LongDescription = "The knight's enormous armor is covered in ancient runes as a cold supernatural presence radiates from beneath its helm.",
                Size = Size.Large, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "world serpent", Symbol = 'W', Color = ConsoleColor.DarkGreen, Hp = 180, Attack = 31, Defense = 14, Speed = 9, Mana = 25, Agility = 10, DifficultyRating = 1.60, CreatureType = CreatureType.Animal,
                ShortDescription = "A colossal serpent whose body disappears into darkness.",
                LongDescription = "The serpent's enormous coils disappear into the surrounding darkness as its head rises high above the dungeon floor.",
                Size = Size.Large, AttackType = AttackType.Bite
            },
            new()
            {
                Name = "elder dragon", Symbol = 'E', Color = ConsoleColor.DarkRed, Hp = 190, Attack = 32, Defense = 15, Speed = 11, Mana = 55, Agility = 12, DifficultyRating = 1.65,
                ShortDescription = "A colossal dragon of immense age and power.",
                LongDescription = "Countless scars cover its ancient scales as the dragon regards you with the cold intelligence of a creature that has survived for centuries.",
                Size = Size.Large, AttackType = AttackType.Bite
            },
            new()
            {
                Name = "demon prince", Symbol = 'P', Color = ConsoleColor.DarkMagenta, Hp = 170, Attack = 32, Defense = 14, Speed = 14, Mana = 65, Agility = 15, DifficultyRating = 1.65,
                ShortDescription = "A towering demon surrounded by an oppressive aura.",
                LongDescription = "Dark energy bends the air around the demon prince as it advances with the confidence of something that has conquered countless realms.",
                Size = Size.Large, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "lich sovereign", Symbol = 'S', Color = ConsoleColor.DarkCyan, Hp = 140, Attack = 26, Defense = 16, Speed = 11, Mana = 80, Agility = 17, DifficultyRating = 1.65, CreatureType = CreatureType.Undead,
                ShortDescription = "An impossibly ancient lich surrounded by magical energy.",
                LongDescription = "The lich sovereign floats above the ground as arcane power spirals around its skeletal body and ancient relics orbit its form.",
                Size = Size.Medium, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "colossus", Symbol = 'C', Color = ConsoleColor.Gray, Hp = 220, Attack = 34, Defense = 18, Speed = 5, Mana = 0, Agility = 4, DifficultyRating = 1.70,
                ShortDescription = "A gigantic construct built from enchanted stone.",
                LongDescription = "The enormous construct moves with geological patience, each heavy step sending vibrations through the dungeon.",
                Size = Size.Large, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "void elemental", Symbol = 'V', Color = ConsoleColor.DarkBlue, Hp = 145, Attack = 30, Defense = 13, Speed = 16, Mana = 70, Agility = 17, DifficultyRating = 1.65,
                ShortDescription = "A creature formed from swirling darkness and empty space.",
                LongDescription = "The creature's outline constantly collapses into nothingness before reforming several steps away.",
                Size = Size.Large, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "dread hydra", Symbol = 'H', Color = ConsoleColor.DarkGreen, Hp = 210, Attack = 31, Defense = 15, Speed = 10, Mana = 30, Agility = 11, DifficultyRating = 1.70, CreatureType = CreatureType.Animal,
                ShortDescription = "A colossal hydra with numerous vicious heads.",
                LongDescription = "Its many heads move independently as the creature's enormous body coils across the chamber, searching for several targets at once.",
                Size = Size.Large, AttackType = AttackType.Bite
            },
            new()
            {
                Name = "hell giant", Symbol = 'G', Color = ConsoleColor.Red, Hp = 200, Attack = 34, Defense = 15, Speed = 9, Mana = 35, Agility = 8, DifficultyRating = 1.70, CreatureType = CreatureType.Humanoid,
                ShortDescription = "A colossal giant covered in scorched armor.",
                LongDescription = "Heat radiates from the giant's body as it grips an enormous weapon capable of crushing stone.",
                Size = Size.Large, AttackType = AttackType.Hit,
                BaseResistances = new ResistanceSet(fire: 40)
            },
            new()
            {
                Name = "nightmare", Symbol = 'N', Color = ConsoleColor.DarkRed, Hp = 130, Attack = 28, Defense = 10, Speed = 17, Mana = 40, Agility = 17, DifficultyRating = 1.60,
                ShortDescription = "A terrifying supernatural horse wreathed in dark flame.",
                LongDescription = "Black flames trail from the creature's hooves as it moves without making a sound, its eyes glowing with unnatural intelligence.",
                Size = Size.Large, AttackType = AttackType.Bite,
                BaseResistances = new ResistanceSet(fire: 30, magic: 20)
            },
            new()
            {
                Name = "dragon emperor", Symbol = 'D', Color = ConsoleColor.DarkYellow, Hp = 240, Attack = 36, Defense = 17, Speed = 12, Mana = 75, Agility = 14, DifficultyRating = 1.75,
                ShortDescription = "A colossal dragon that has survived countless ages.",
                LongDescription = "Its immense body nearly fills the dungeon chamber as ancient eyes measure you with the detached curiosity of an apex predator.",
                Size = Size.Large, AttackType = AttackType.Bite
            },
            new()
            {
                Name = "demon overlord", Symbol = 'O', Color = ConsoleColor.DarkMagenta, Hp = 220, Attack = 37, Defense = 16, Speed = 14, Mana = 85, Agility = 16, DifficultyRating = 1.75,
                ShortDescription = "An enormous demon radiating overwhelming power.",
                LongDescription = "The creature's presence alone makes the chamber feel smaller as dark energy churns around its towering form.",
                Size = Size.Large, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "lich emperor", Symbol = 'L', Color = ConsoleColor.DarkCyan, Hp = 175, Attack = 30, Defense = 18, Speed = 12, Mana = 100, Agility = 18, DifficultyRating = 1.75, CreatureType = CreatureType.Undead,
                ShortDescription = "The ancient ruler of an undead kingdom.",
                LongDescription = "A crown of ancient bone rests upon its skull as overwhelming necromantic power fills the chamber around it.",
                Size = Size.Medium, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "astral titan", Symbol = 'A', Color = ConsoleColor.White, Hp = 260, Attack = 39, Defense = 19, Speed = 10, Mana = 60, Agility = 12, DifficultyRating = 1.80, CreatureType = CreatureType.Humanoid,
                ShortDescription = "A colossal being seemingly carved from starlight.",
                LongDescription = "Strange lights shimmer beneath its translucent armor as the enormous being looks down upon you with incomprehensible patience.",
                Size = Size.Large, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "void dragon", Symbol = 'V', Color = ConsoleColor.DarkBlue, Hp = 250, Attack = 38, Defense = 17, Speed = 14, Mana = 90, Agility = 16, DifficultyRating = 1.80,
                ShortDescription = "A dragon whose body seems partially absent from reality.",
                LongDescription = "Portions of the enormous dragon flicker in and out of existence as dark energy pours from its open jaws.",
                Size = Size.Large, AttackType = AttackType.Bite
            },
            new()
            {
                Name = "abyssal titan", Symbol = 'T', Color = ConsoleColor.DarkRed, Hp = 280, Attack = 40, Defense = 19, Speed = 9, Mana = 70, Agility = 10, DifficultyRating = 1.85,
                ShortDescription = "A colossal warrior from beyond the mortal world.",
                LongDescription = "The titan's enormous silhouette towers above the dungeon as impossible symbols glow across its ancient armor.",
                Size = Size.Large, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "death sovereign", Symbol = 'K', Color = ConsoleColor.Black, Hp = 230, Attack = 39, Defense = 20, Speed = 11, Mana = 80, Agility = 14, DifficultyRating = 1.85, CreatureType = CreatureType.Undead,
                ShortDescription = "An undead warlord surrounded by supernatural darkness.",
                LongDescription = "The armored sovereign carries an ancient weapon as an oppressive aura of death follows every deliberate movement.",
                Size = Size.Large, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "primordial hydra", Symbol = 'H', Color = ConsoleColor.DarkGreen, Hp = 300, Attack = 38, Defense = 18, Speed = 11, Mana = 50, Agility = 12, DifficultyRating = 1.85, CreatureType = CreatureType.Animal,
                ShortDescription = "A monstrous hydra with countless ancient heads.",
                LongDescription = "The enormous beast's many heads roar in unison, shaking dust from the ceiling as its massive body coils across the chamber.",
                Size = Size.Large, AttackType = AttackType.Bite
            },
            new()
            {
                Name = "elder wyrm", Symbol = 'W', Color = ConsoleColor.DarkRed, Hp = 320, Attack = 42, Defense = 20, Speed = 12, Mana = 100, Agility = 15, DifficultyRating = 1.90,
                ShortDescription = "An ancient dragon of terrifying size and power.",
                LongDescription = "The elder wyrm's enormous wings blot out the chamber's light as it raises its head and unleashes a roar that shakes the dungeon.",
                Size = Size.Large, AttackType = AttackType.Bite
            },
            new()
            {
                Name = "demon emperor", Symbol = 'D', Color = ConsoleColor.DarkMagenta, Hp = 290, Attack = 43, Defense = 19, Speed = 15, Mana = 110, Agility = 17, DifficultyRating = 1.90,
                ShortDescription = "A supreme demon surrounded by terrible power.",
                LongDescription = "Reality seems to warp around the demon emperor as it advances, dark energy gathering around its enormous hands.",
                Size = Size.Large, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "lich god", Symbol = 'G', Color = ConsoleColor.DarkCyan, Hp = 220, Attack = 35, Defense = 21, Speed = 13, Mana = 130, Agility = 19, DifficultyRating = 1.90, CreatureType = CreatureType.Undead,
                ShortDescription = "An undead entity that has transcended mortal magic.",
                LongDescription = "The skeletal figure floats within a storm of arcane energy as ancient symbols appear and vanish across its form.",
                Size = Size.Medium, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "world eater", Symbol = 'E', Color = ConsoleColor.DarkYellow, Hp = 350, Attack = 45, Defense = 21, Speed = 10, Mana = 80, Agility = 10, DifficultyRating = 1.95, CreatureType = CreatureType.Animal,
                ShortDescription = "An enormous beast capable of destroying entire fortresses.",
                LongDescription = "The creature's immense bulk fills the chamber as it opens a cavernous maw capable of swallowing a creature whole.",
                Size = Size.Large, AttackType = AttackType.Bite
            },
            new()
            {
                Name = "void emperor", Symbol = 'V', Color = ConsoleColor.DarkBlue, Hp = 280, Attack = 42, Defense = 20, Speed = 17, Mana = 125, Agility = 19, DifficultyRating = 1.95,
                ShortDescription = "A terrifying entity formed from the darkness between worlds.",
                LongDescription = "Its shape constantly shifts between recognizable forms as the surrounding darkness bends toward it.",
                Size = Size.Large, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "ancient godspawn", Symbol = 'A', Color = ConsoleColor.DarkMagenta, Hp = 330, Attack = 45, Defense = 22, Speed = 14, Mana = 140, Agility = 18, DifficultyRating = 2.00,
                ShortDescription = "A monstrous being descended from an ancient power.",
                LongDescription = "The creature's impossible anatomy seems to defy natural law as an oppressive presence fills the entire chamber.",
                Size = Size.Large, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "dread dragon", Symbol = 'R', Color = ConsoleColor.DarkRed, Hp = 360, Attack = 47, Defense = 23, Speed = 13, Mana = 125, Agility = 17, DifficultyRating = 2.00,
                ShortDescription = "A gigantic dragon radiating overwhelming destructive power.",
                LongDescription = "Ancient scars cover its enormous body as supernatural fire burns behind its eyes.",
                Size = Size.Large, AttackType = AttackType.Bite
            },
            new()
            {
                Name = "abyssal lord", Symbol = 'B', Color = ConsoleColor.DarkMagenta, Hp = 340, Attack = 48, Defense = 22, Speed = 16, Mana = 150, Agility = 19, DifficultyRating = 2.00,
                ShortDescription = "A supreme demon lord from the deepest abyss.",
                LongDescription = "The enormous demon radiates such overwhelming power that the air itself seems to recoil from its presence.",
                Size = Size.Large, AttackType = AttackType.Hit
            },
            new()
            {
                Name = "death god avatar", Symbol = 'K', Color = ConsoleColor.Black, Hp = 300, Attack = 46, Defense = 24, Speed = 13, Mana = 140, Agility = 18, DifficultyRating = 2.00, CreatureType = CreatureType.Undead,
                ShortDescription = "An avatar of an ancient power over death.",
                LongDescription = "A terrible armored figure stands motionless in the darkness, surrounded by an aura that makes the living instinctively recoil.",
                Size = Size.Large, AttackType = AttackType.Hit
            },

            // --- Tile-attuned monster system -- see "Tile-Attuned Monster System --
            // Implementation Specification.txt". Each biome's roster spans a real difficulty
            // range rather than one flat tier (the spec's own explicit requirement), and
            // PreferredFloorType/ElementalAffinity are the ONLY things that make these
            // different from an ordinary archetype -- CreateRandom/BuildMonster treat them
            // identically otherwise. OpposingElement is never set here directly; it's always
            // derived from ElementalAffinity via the one centralized ElementalOpposition table.

            // WATER
            new()
            {
                Name = "water snake", Symbol = 'n', Color = ConsoleColor.Blue, Hp = 14, Attack = 5, Defense = 2, Speed = 12, Agility = 12, DifficultyRating = 0.35, CreatureType = CreatureType.Animal,
                ShortDescription = "A slick serpent gliding through shallow water.",
                LongDescription = "Its scales barely break the water's surface as it coils closer, perfectly at home in the shallows.",
                Size = Size.Small, AttackType = AttackType.Bite,
                PreferredFloorType = FloorType.Water, ElementalAffinity = DamageType.Water
            },
            new()
            {
                Name = "water imp", Symbol = 'i', Color = ConsoleColor.Blue, Hp = 16, Attack = 5, Defense = 2, Speed = 10, Mana = 8, Agility = 10, DifficultyRating = 0.45, CreatureType = CreatureType.Other,
                ShortDescription = "A small, translucent creature made of rippling water.",
                LongDescription = "Its body flows and reshapes like liquid given a will of its own, never quite settling into one form for long.",
                Size = Size.Small, AttackType = AttackType.Hit,
                PreferredFloorType = FloorType.Water, ElementalAffinity = DamageType.Water
            },
            new()
            {
                Name = "drowned crawler", Symbol = 'c', Color = ConsoleColor.DarkBlue, Hp = 20, Attack = 7, Defense = 3, Speed = 9, Agility = 9, DifficultyRating = 0.55, CreatureType = CreatureType.Undead,
                ShortDescription = "A bloated corpse dragging itself out of the water.",
                LongDescription = "Waterlogged flesh sloughs from its bones as it crawls forward, never straying far from the water that claimed it.",
                Size = Size.Medium, AttackType = AttackType.Bite,
                PreferredFloorType = FloorType.Water, ElementalAffinity = DamageType.Water
            },
            new()
            {
                Name = "river drake", Symbol = 'k', Color = ConsoleColor.Cyan, Hp = 26, Attack = 9, Defense = 4, Speed = 11, Agility = 11, DifficultyRating = 0.70, CreatureType = CreatureType.Animal,
                ShortDescription = "A sleek, finned reptile at home in deep water.",
                LongDescription = "Webbed claws and a finned tail make the drake nearly as fast in water as it is dangerous out of it.",
                Size = Size.Medium, AttackType = AttackType.Bite,
                PreferredFloorType = FloorType.Water, ElementalAffinity = DamageType.Water
            },
            new()
            {
                Name = "giant water leech", Symbol = 'e', Color = ConsoleColor.DarkBlue, Hp = 22, Attack = 7, Defense = 2, Speed = 8, Agility = 9, DifficultyRating = 0.60, CreatureType = CreatureType.Animal,
                ShortDescription = "A bloated leech drifting just beneath the surface.",
                LongDescription = "It undulates lazily through the water, sensing the warmth of blood long before it ever draws near.",
                Size = Size.Small, AttackType = AttackType.Bite,
                PreferredFloorType = FloorType.Water, ElementalAffinity = DamageType.Water
            },

            // ICE
            new()
            {
                Name = "ice imp", Symbol = 'i', Color = ConsoleColor.Cyan, Hp = 15, Attack = 5, Defense = 2, Speed = 10, Mana = 8, Agility = 10, DifficultyRating = 0.45, CreatureType = CreatureType.Other,
                ShortDescription = "A small creature of jagged, translucent ice.",
                LongDescription = "Frost creeps outward from its feet with every step, and its hollow eyes glitter like broken glass.",
                Size = Size.Small, AttackType = AttackType.Hit,
                PreferredFloorType = FloorType.Ice, ElementalAffinity = DamageType.Ice
            },
            new()
            {
                Name = "frost beetle", Symbol = 'B', Color = ConsoleColor.Cyan, Hp = 20, Attack = 6, Defense = 4, Speed = 7, Agility = 8, DifficultyRating = 0.40, CreatureType = CreatureType.Animal,
                ShortDescription = "A beetle with a shell of natural ice.",
                LongDescription = "Its frozen carapace creaks faintly with each slow, deliberate step across the ice.",
                Size = Size.Small, AttackType = AttackType.Bite,
                PreferredFloorType = FloorType.Ice, ElementalAffinity = DamageType.Ice
            },
            new()
            {
                Name = "ice serpent", Symbol = 'n', Color = ConsoleColor.Cyan, Hp = 18, Attack = 7, Defense = 3, Speed = 11, Agility = 11, DifficultyRating = 0.55, CreatureType = CreatureType.Animal,
                ShortDescription = "A pale serpent gliding smoothly across the ice.",
                LongDescription = "Frost clings to its scales as it slithers across the frozen floor without ever losing its footing.",
                Size = Size.Small, AttackType = AttackType.Bite,
                PreferredFloorType = FloorType.Ice, ElementalAffinity = DamageType.Ice
            },
            new()
            {
                Name = "rime spider", Symbol = 's', Color = ConsoleColor.White, Hp = 22, Attack = 8, Defense = 3, Speed = 12, Mana = 6, Agility = 12, DifficultyRating = 0.65, CreatureType = CreatureType.Animal,
                ShortDescription = "A pale spider trailing threads of frost.",
                LongDescription = "Every strand of its web glitters with rime as it skitters across the frozen ground, unbothered by the cold.",
                Size = Size.Small, AttackType = AttackType.Sting,
                PreferredFloorType = FloorType.Ice, ElementalAffinity = DamageType.Ice
            },
            new()
            {
                Name = "frost salamander", Symbol = 'l', Color = ConsoleColor.Cyan, Hp = 28, Attack = 10, Defense = 4, Speed = 9, Agility = 9, DifficultyRating = 0.80, CreatureType = CreatureType.Animal,
                ShortDescription = "A salamander whose skin radiates bitter cold.",
                LongDescription = "Frost forms on the ground wherever it lingers, and its breath mists even the coldest air.",
                Size = Size.Medium, AttackType = AttackType.Bite,
                PreferredFloorType = FloorType.Ice, ElementalAffinity = DamageType.Ice
            },

            // FIRE
            new()
            {
                Name = "fire imp", Symbol = 'i', Color = ConsoleColor.Red, Hp = 15, Attack = 5, Defense = 2, Speed = 10, Mana = 8, Agility = 10, DifficultyRating = 0.45, CreatureType = CreatureType.Other,
                ShortDescription = "A small creature wreathed in flickering flame.",
                LongDescription = "Its body burns without ever being consumed, casting flickering light across the walls as it approaches.",
                Size = Size.Small, AttackType = AttackType.Hit,
                PreferredFloorType = FloorType.Fire, ElementalAffinity = DamageType.Fire
            },
            new()
            {
                Name = "ember beetle", Symbol = 'B', Color = ConsoleColor.Red, Hp = 20, Attack = 6, Defense = 4, Speed = 8, Agility = 8, DifficultyRating = 0.40, CreatureType = CreatureType.Animal,
                ShortDescription = "A beetle with a shell of smoldering coals.",
                LongDescription = "Tiny embers flake from its shell and wink out on the stone as it scuttles closer.",
                Size = Size.Small, AttackType = AttackType.Bite,
                PreferredFloorType = FloorType.Fire, ElementalAffinity = DamageType.Fire
            },
            new()
            {
                Name = "cinder hound", Symbol = 'h', Color = ConsoleColor.Red, Hp = 28, Attack = 10, Defense = 3, Speed = 13, Mana = 4, Agility = 12, DifficultyRating = 0.65, CreatureType = CreatureType.Animal,
                ShortDescription = "A lean hound with a hide of smoldering cinders.",
                LongDescription = "Its paws leave scorched prints on the stone as it circles, cinders trailing from its open jaws.",
                Size = Size.Medium, AttackType = AttackType.Bite,
                PreferredFloorType = FloorType.Fire, ElementalAffinity = DamageType.Fire
            },
            new()
            {
                Name = "flame salamander", Symbol = 'l', Color = ConsoleColor.Red, Hp = 26, Attack = 9, Defense = 4, Speed = 9, Agility = 9, DifficultyRating = 0.70, CreatureType = CreatureType.Animal,
                ShortDescription = "A salamander whose skin glows like a dying coal.",
                LongDescription = "Heat radiates from its hide in waves, and the stone scorches faintly wherever it crawls.",
                Size = Size.Medium, AttackType = AttackType.Bite,
                PreferredFloorType = FloorType.Fire, ElementalAffinity = DamageType.Fire
            },
            new()
            {
                Name = "ember spider", Symbol = 's', Color = ConsoleColor.Red, Hp = 22, Attack = 8, Defense = 3, Speed = 12, Mana = 6, Agility = 12, DifficultyRating = 0.60, CreatureType = CreatureType.Animal,
                ShortDescription = "A spider whose web smolders instead of burning.",
                LongDescription = "Threads of glowing silk trail behind it as it darts across the scorched ground.",
                Size = Size.Small, AttackType = AttackType.Sting,
                PreferredFloorType = FloorType.Fire, ElementalAffinity = DamageType.Fire
            },

            // LAVA
            new()
            {
                Name = "lava imp", Symbol = 'i', Color = ConsoleColor.DarkRed, Hp = 18, Attack = 6, Defense = 3, Speed = 9, Mana = 10, Agility = 9, DifficultyRating = 0.55, CreatureType = CreatureType.Other,
                ShortDescription = "A small creature formed from cooling magma.",
                LongDescription = "Molten cracks glow across its rocky hide, pulsing faintly brighter whenever it grows agitated.",
                Size = Size.Small, AttackType = AttackType.Hit,
                PreferredFloorType = FloorType.Lava, ElementalAffinity = DamageType.Fire
            },
            new()
            {
                Name = "magma crawler", Symbol = 'c', Color = ConsoleColor.DarkRed, Hp = 30, Attack = 10, Defense = 5, Speed = 7, Agility = 7, DifficultyRating = 0.75, CreatureType = CreatureType.Animal,
                ShortDescription = "A many-legged creature dripping molten rock.",
                LongDescription = "Droplets of magma harden into a trail behind it as its segmented body drags across the molten floor.",
                Size = Size.Medium, AttackType = AttackType.Bite,
                PreferredFloorType = FloorType.Lava, ElementalAffinity = DamageType.Fire
            },
            new()
            {
                Name = "lava serpent", Symbol = 'n', Color = ConsoleColor.DarkRed, Hp = 32, Attack = 11, Defense = 5, Speed = 9, Agility = 9, DifficultyRating = 0.85, CreatureType = CreatureType.Animal,
                ShortDescription = "A serpent that swims through molten rock as if it were water.",
                LongDescription = "Its scales glow a dull orange as it surfaces briefly from the lava before slipping beneath again.",
                Size = Size.Medium, AttackType = AttackType.Bite,
                PreferredFloorType = FloorType.Lava, ElementalAffinity = DamageType.Fire
            },
            new()
            {
                Name = "magma toad", Symbol = 'f', Color = ConsoleColor.DarkRed, Hp = 24, Attack = 8, Defense = 4, Speed = 6, Agility = 7, DifficultyRating = 0.65, CreatureType = CreatureType.Animal,
                ShortDescription = "A bloated toad with a hide of hardened magma.",
                LongDescription = "Its throat glows like a furnace as it croaks, and the ground hisses faintly beneath its weight.",
                Size = Size.Small, AttackType = AttackType.Bite,
                PreferredFloorType = FloorType.Lava, ElementalAffinity = DamageType.Fire
            },
            new()
            {
                Name = "obsidian beetle", Symbol = 'B', Color = ConsoleColor.DarkGray, Hp = 34, Attack = 11, Defense = 7, Speed = 6, Agility = 6, DifficultyRating = 0.90, CreatureType = CreatureType.Animal,
                ShortDescription = "A heavily armored beetle with a shell of black glass.",
                LongDescription = "Its obsidian shell, cooled and hardened from countless years in the lava, deflects everything but the strongest blows.",
                Size = Size.Medium, AttackType = AttackType.Bite,
                PreferredFloorType = FloorType.Lava, ElementalAffinity = DamageType.Fire
            },

            // MUD
            new()
            {
                Name = "mud imp", Symbol = 'i', Color = ConsoleColor.DarkYellow, Hp = 15, Attack = 5, Defense = 3, Speed = 8, Mana = 6, Agility = 8, DifficultyRating = 0.40, CreatureType = CreatureType.Other,
                ShortDescription = "A small creature of wet, sucking mud.",
                LongDescription = "It squelches with every movement, leaving no footprints because it never fully separates from the ground.",
                Size = Size.Small, AttackType = AttackType.Hit,
                PreferredFloorType = FloorType.Mud, ElementalAffinity = DamageType.Earth
            },
            new()
            {
                Name = "mud crawler", Symbol = 'c', Color = ConsoleColor.DarkYellow, Hp = 20, Attack = 6, Defense = 4, Speed = 6, Agility = 6, DifficultyRating = 0.45, CreatureType = CreatureType.Animal,
                ShortDescription = "A thick-hided creature that wallows through the mud.",
                LongDescription = "Caked in drying mud, it drags itself forward with slow, deliberate persistence.",
                Size = Size.Medium, AttackType = AttackType.Bite,
                PreferredFloorType = FloorType.Mud, ElementalAffinity = DamageType.Earth
            },
            new()
            {
                Name = "mire toad", Symbol = 'f', Color = ConsoleColor.DarkGreen, Hp = 22, Attack = 7, Defense = 3, Speed = 7, Agility = 8, DifficultyRating = 0.55, CreatureType = CreatureType.Animal,
                ShortDescription = "A bloated toad half-submerged in the mire.",
                LongDescription = "Its throat bulges as it croaks from the mud, barely visible until it lunges.",
                Size = Size.Small, AttackType = AttackType.Bite,
                PreferredFloorType = FloorType.Mud, ElementalAffinity = DamageType.Earth
            },
            new()
            {
                Name = "bog serpent", Symbol = 'n', Color = ConsoleColor.DarkYellow, Hp = 26, Attack = 9, Defense = 4, Speed = 8, Agility = 9, DifficultyRating = 0.65, CreatureType = CreatureType.Animal,
                ShortDescription = "A thick serpent coiled in the wet mud.",
                LongDescription = "It moves with surprising speed through the mud, leaving barely a ripple behind.",
                Size = Size.Medium, AttackType = AttackType.Bite,
                PreferredFloorType = FloorType.Mud, ElementalAffinity = DamageType.Earth
            },

            // SAND
            new()
            {
                Name = "sand imp", Symbol = 'i', Color = ConsoleColor.Yellow, Hp = 15, Attack = 5, Defense = 2, Speed = 11, Mana = 6, Agility = 11, DifficultyRating = 0.40, CreatureType = CreatureType.Other,
                ShortDescription = "A small creature formed from swirling grains of sand.",
                LongDescription = "It shifts and reforms constantly, scattering into loose sand whenever it stands still too long.",
                Size = Size.Small, AttackType = AttackType.Hit,
                PreferredFloorType = FloorType.Sand, ElementalAffinity = DamageType.Earth
            },
            new()
            {
                Name = "dune scorpion", Symbol = 'X', Color = ConsoleColor.Yellow, Hp = 24, Attack = 8, Defense = 4, Speed = 9, Agility = 10, DifficultyRating = 0.60, CreatureType = CreatureType.Animal,
                ShortDescription = "A sand-colored scorpion with a raised, venomous tail.",
                LongDescription = "Its pale carapace blends almost perfectly into the surrounding sand as its stinger arcs overhead.",
                Size = Size.Small, AttackType = AttackType.Sting,
                PreferredFloorType = FloorType.Sand, ElementalAffinity = DamageType.Earth
            },
            new()
            {
                Name = "sand serpent", Symbol = 'n', Color = ConsoleColor.Yellow, Hp = 22, Attack = 7, Defense = 3, Speed = 12, Agility = 12, DifficultyRating = 0.55, CreatureType = CreatureType.Animal,
                ShortDescription = "A serpent that swims beneath the sand.",
                LongDescription = "Only a faint ripple across the sand betrays its position before it bursts upward to strike.",
                Size = Size.Small, AttackType = AttackType.Bite,
                PreferredFloorType = FloorType.Sand, ElementalAffinity = DamageType.Earth
            },
            new()
            {
                Name = "dust beetle", Symbol = 'B', Color = ConsoleColor.DarkYellow, Hp = 18, Attack = 6, Defense = 4, Speed = 8, Agility = 8, DifficultyRating = 0.45, CreatureType = CreatureType.Animal,
                ShortDescription = "A beetle coated in a fine layer of dust.",
                LongDescription = "A small cloud of dust rises with every step it takes across the dry ground.",
                Size = Size.Small, AttackType = AttackType.Bite,
                PreferredFloorType = FloorType.Sand, ElementalAffinity = DamageType.Earth
            },

            // GRASS -- no sensible elemental affinity yet (design spec section 32); still
            // floor-attuned (off-terrain attrition and movement preference still apply),
            // just with no elemental resistance/vulnerability interaction.
            new()
            {
                Name = "thorn lizard", Symbol = 'l', Color = ConsoleColor.Green, Hp = 16, Attack = 6, Defense = 3, Speed = 10, Agility = 10, DifficultyRating = 0.45, CreatureType = CreatureType.Animal,
                ShortDescription = "A lizard covered in short, hardened thorns.",
                LongDescription = "Thorny ridges run the length of its back, blending seamlessly into the tall grass around it.",
                Size = Size.Small, AttackType = AttackType.Bite,
                PreferredFloorType = FloorType.Grass
            },
            new()
            {
                Name = "thorn imp", Symbol = 'i', Color = ConsoleColor.Green, Hp = 14, Attack = 5, Defense = 2, Speed = 10, Mana = 6, Agility = 10, DifficultyRating = 0.40, CreatureType = CreatureType.Other,
                ShortDescription = "A small creature woven from brambles and vine.",
                LongDescription = "Thorned vines writhe where hair should be, rustling faintly even when nothing else moves.",
                Size = Size.Small, AttackType = AttackType.Hit,
                PreferredFloorType = FloorType.Grass
            },
            new()
            {
                Name = "razor hare", Symbol = 'h', Color = ConsoleColor.Green, Hp = 12, Attack = 6, Defense = 1, Speed = 16, Agility = 15, DifficultyRating = 0.35, CreatureType = CreatureType.Animal,
                ShortDescription = "A wiry hare with claws sharpened like blades.",
                LongDescription = "It darts through the grass in short, unpredictable bursts, claws flashing before it vanishes again.",
                Size = Size.Small, AttackType = AttackType.Bite,
                PreferredFloorType = FloorType.Grass
            },

            // SWAMP -- treated as Water-aligned per design spec section 33's own suggestion.
            new()
            {
                Name = "swamp imp", Symbol = 'i', Color = ConsoleColor.DarkGreen, Hp = 17, Attack = 6, Defense = 3, Speed = 9, Mana = 8, Agility = 9, DifficultyRating = 0.50, CreatureType = CreatureType.Other,
                ShortDescription = "A small creature lurking in the stagnant water.",
                LongDescription = "Its skin is slick with algae, and it watches from the murk with patient, unblinking eyes.",
                Size = Size.Small, AttackType = AttackType.Hit,
                PreferredFloorType = FloorType.Swamp, ElementalAffinity = DamageType.Water
            },
            new()
            {
                Name = "marsh serpent", Symbol = 'n', Color = ConsoleColor.DarkGreen, Hp = 24, Attack = 8, Defense = 3, Speed = 10, Agility = 10, DifficultyRating = 0.60, CreatureType = CreatureType.Animal,
                ShortDescription = "A serpent gliding silently through the marsh.",
                LongDescription = "It moves through the murky water without a ripple, surfacing only when it's already too late.",
                Size = Size.Medium, AttackType = AttackType.Bite,
                PreferredFloorType = FloorType.Swamp, ElementalAffinity = DamageType.Water
            },
            new()
            {
                Name = "bog spider", Symbol = 's', Color = ConsoleColor.DarkGreen, Hp = 26, Attack = 9, Defense = 3, Speed = 11, Mana = 6, Agility = 11, DifficultyRating = 0.65, CreatureType = CreatureType.Animal,
                ShortDescription = "A spider that spins webs low across the murky water.",
                LongDescription = "Its webs stretch between half-submerged roots, glistening with condensation from the swamp air.",
                Size = Size.Small, AttackType = AttackType.Sting,
                PreferredFloorType = FloorType.Swamp, ElementalAffinity = DamageType.Water
            },
            new()
            {
                Name = "mire drake", Symbol = 'k', Color = ConsoleColor.DarkGreen, Hp = 32, Attack = 11, Defense = 5, Speed = 9, Agility = 9, DifficultyRating = 0.80, CreatureType = CreatureType.Animal,
                ShortDescription = "A squat, powerful reptile at home in the swamp.",
                LongDescription = "Its thick hide is caked in mud and algae, and it moves through the swamp with unhurried confidence.",
                Size = Size.Medium, AttackType = AttackType.Bite,
                PreferredFloorType = FloorType.Swamp, ElementalAffinity = DamageType.Water
            },

            // ASH -- no forced elemental affinity (design spec section 34).
            new()
            {
                Name = "ash imp", Symbol = 'i', Color = ConsoleColor.DarkGray, Hp = 16, Attack = 6, Defense = 3, Speed = 9, Mana = 6, Agility = 9, DifficultyRating = 0.45, CreatureType = CreatureType.Other,
                ShortDescription = "A small creature formed from drifting ash.",
                LongDescription = "It crumbles and reforms with every gust, leaving a faint gray haze hanging in the air behind it.",
                Size = Size.Small, AttackType = AttackType.Hit,
                PreferredFloorType = FloorType.Ash
            },
            new()
            {
                Name = "ash crawler", Symbol = 'c', Color = ConsoleColor.DarkGray, Hp = 20, Attack = 7, Defense = 4, Speed = 7, Agility = 7, DifficultyRating = 0.50, CreatureType = CreatureType.Animal,
                ShortDescription = "A many-legged creature coated in fine gray ash.",
                LongDescription = "Ash sifts from its carapace with every step, settling in a thin trail behind it.",
                Size = Size.Medium, AttackType = AttackType.Bite,
                PreferredFloorType = FloorType.Ash
            },
            new()
            {
                Name = "cinder rat", Symbol = 'r', Color = ConsoleColor.DarkGray, Hp = 14, Attack = 5, Defense = 2, Speed = 13, Agility = 13, DifficultyRating = 0.35, CreatureType = CreatureType.Animal,
                ShortDescription = "A rat with fur singed gray from the ash.",
                LongDescription = "It scurries low across the ash-covered floor, barely visible until it's already underfoot.",
                Size = Size.Small, AttackType = AttackType.Bite,
                PreferredFloorType = FloorType.Ash
            },
            new()
            {
                Name = "ash wraith", Symbol = 'W', Color = ConsoleColor.Gray, Hp = 30, Attack = 10, Defense = 5, Speed = 11, Mana = 10, Agility = 11, DifficultyRating = 0.70, CreatureType = CreatureType.Undead,
                ShortDescription = "A gaunt spirit wreathed in drifting ash.",
                LongDescription = "Ash swirls in place of a body as it drifts silently forward, leaving no mark on the ground beneath it.",
                Size = Size.Medium, AttackType = AttackType.Hit,
                PreferredFloorType = FloorType.Ash
            }
    };

    // With 100+ archetypes spanning DifficultyRating ~0.20-2.00, encounter
    // selection targets a difficulty appropriate to the floor and picks among
    // the nearest candidates -- reaching the toughest archetypes around floor
    // 50. (The old "unlock more of the array as floors deepen" scheme only
    // worked because there were just 4 archetypes; it ignored DifficultyRating
    // entirely and would have taken 200+ floors to unlock this roster.)
    private const double BaseDifficulty = 0.20;
    private const double DifficultyPerFloor = 0.036;
    private const int EncounterPoolSize = 6;

    /// <summary>Resolves an archetype's actual capabilities: its per-field override if set, otherwise CreatureType's default (see CreatureCapabilities). Shared by CreateRandom.</summary>
    private static (bool CanCarryItems, bool CanEquipItems, bool CanUseItems) ResolveCapabilities(Archetype template)
    {
        var defaults = CreatureCapabilities.GetDefaults(template.CreatureType);
        return (
            template.CanCarryItemsOverride ?? defaults.CanCarryItems,
            template.CanEquipItemsOverride ?? defaults.CanEquipItems,
            template.CanUseItemsOverride ?? defaults.CanUseItems);
    }

    /// <summary>
    /// Ordinary encounter table -- deliberately excludes every tile-attuned archetype (design
    /// spec section 17: "ordinary monsters remain unaffected," and section 37: a special-floor
    /// room's attuned roster is an ADDITION to the normal table, not a replacement). Without
    /// this split, the ~40 tile-attuned archetypes added for that system (clustered in the
    /// DifficultyRating 0.35-0.55 band to suit early floors) would dominate the "closest 6 by
    /// difficulty" pool CreateRandom uses for every ordinary spawn on every floor, crowding out
    /// goblins/rats/skeletons/etc. even in a plain room with no special floor at all. Computed
    /// once rather than re-filtering 140+ entries on every single spawn.
    /// </summary>
    private static readonly Archetype[] OrdinaryArchetypes = Archetypes.Where(a => a.PreferredFloorType == null).ToArray();

    public static Monster CreateRandom(int x, int y, int difficultyLevel, Random rng) =>
        BuildMonster(SelectArchetype(OrdinaryArchetypes, difficultyLevel, rng), x, y, difficultyLevel, rng);

    /// <summary>
    /// Like CreateRandom, but only draws from archetypes whose PreferredFloorType matches --
    /// used by DungeonGenerator.SpawnMonsters when populating a special-floor room, so a
    /// Water Snake only ever spawns via this method, never CreateRandom (design spec sections
    /// 14/26-38). Null if no archetype in the whole roster is attuned to that FloorType (never
    /// true for any of the 9 special FloorTypes today, given the roster below, but keeps the
    /// caller safe if that ever changes rather than throwing).
    /// </summary>
    public static Monster CreateFloorAttuned(int x, int y, int difficultyLevel, FloorType floorType, Random rng)
    {
        var candidates = Archetypes.Where(a => a.PreferredFloorType == floorType).ToList();
        return candidates.Count == 0 ? null : BuildMonster(SelectArchetype(candidates, difficultyLevel, rng), x, y, difficultyLevel, rng);
    }

    /// <summary>Shared "closest DifficultyRating to this floor, then random among the nearest EncounterPoolSize" selection CreateRandom and CreateFloorAttuned both use, over whatever candidate list the caller already narrowed down (the full roster, or one FloorType's attuned subset).</summary>
    private static Archetype SelectArchetype(IReadOnlyList<Archetype> candidates, int difficultyLevel, Random rng)
    {
        double targetDifficulty = BaseDifficulty + difficultyLevel * DifficultyPerFloor;
        int poolSize = Math.Min(EncounterPoolSize, candidates.Count);
        return candidates
            .OrderBy(a => Math.Abs(a.DifficultyRating - targetDifficulty))
            .Take(poolSize)
            .ElementAt(rng.Next(poolSize));
    }

    private static Monster BuildMonster(Archetype template, int x, int y, int difficultyLevel, Random rng)
    {
        var capabilities = ResolveCapabilities(template);
        var monster = new Monster(template.Agility, template.DifficultyRating, template.XpRewardOverride, template.Size, template.AttackType)
        {
            X = x,
            Y = y,
            Name = template.Name,
            Symbol = template.Symbol,
            Color = template.Color,
            ShortDescription = template.ShortDescription,
            LongDescription = template.LongDescription,
            Level = difficultyLevel,
            // Attack/HP/Defense all scale with depth now -- Defense previously never scaled at
            // all, so it fell further behind rising player Attack (itself now STR-driven) every floor.
            BasePhysicalAttackPower = template.Attack + (difficultyLevel / 2),
            DefensePower = template.Defense + (difficultyLevel / 4),
            Speed = template.Speed,
            Health = new HealthComponent(template.Hp + (difficultyLevel * 3)),
            Mana = new ManaComponent(template.Mana),
            CreatureType = template.CreatureType,
            CanCarryItems = capabilities.CanCarryItems,
            CanEquipItems = capabilities.CanEquipItems,
            CanUseItems = capabilities.CanUseItems,
            PreferredFloorType = template.PreferredFloorType,
            ElementalAffinity = template.ElementalAffinity,
            OffPreferredFloorDamagePercent = template.OffPreferredFloorDamagePercent,
            BaseResistances = template.BaseResistances,
            SoundType = template.SoundType
        };

        foreach (var spell in template.KnownSpells)
        {
            monster.KnownSpells.Add(spell);
        }

        // A monster capable of carrying items might spawn already holding one -- see
        // LootGenerator.GenerateSpawnItems. Equipment-type items go on (CanEquipItems
        // only, so this list is only ever non-empty for a monster that has one) and
        // actually affect its stats via ApplyEquipmentBonus; everything else (potions,
        // scrolls, ...) is just carried -- see ChaseAI's healing-item check, the reason
        // CanUseItems needs something to actually act on from turn one.
        foreach (var item in LootGenerator.GenerateSpawnItems(monster, rng))
        {
            var slot = item.EquipmentType != EquipmentType.None ? monster.Equipment.FindAutoEquipSlot(item) : null;
            if (slot.HasValue)
            {
                // A single-slot type (Head, Body, ...) always resolves even when already
                // occupied (see EquipmentComponent.FindAutoEquipSlot) -- carry whatever it
                // displaces rather than letting a second same-slot roll silently vanish.
                var previous = monster.Equipment.EquipInSlot(slot.Value, item);
                if (previous != null)
                {
                    RemoveEquipmentBonus(monster, previous, slot.Value);
                    monster.Inventory.AddItem(previous);
                }
                ApplyEquipmentBonus(monster, item, slot.Value);
            }
            else
            {
                monster.Inventory.AddItem(item);
            }
        }

        if (template.PreferRangedAttack)
        {
            EquipGuaranteedRangedWeapon(monster);
        }

        monster.AI = monster.GetEquippedItems().Any(IsRangedWeapon)
            ? new RangedAttackAI()
            : template.KnownSpells.Length > 0 ? new SpellCasterAI()
            : monster.IsFloorAttuned ? new FloorAttunedAI()
            : new ChaseAI();

        return monster;
    }

    private static bool IsRangedWeapon(Item item) => item.WeaponType == WeaponType.Bow || item.WeaponType == WeaponType.Crossbow || item.WeaponType == WeaponType.Sling;

    /// <summary>Forces a Bow + readied Arrows onto a PreferRangedAttack archetype when the random loot roll (LootGenerator.GenerateSpawnItems, just above) didn't already grant a ranged weapon -- otherwise a "goblin archer" could spawn with nothing to fire. No-op if it already got one (or something else) equipped in its RangedWeapon slot from that roll. Ammo is readied directly into EquipmentSlot.Ammunition now, not left loose in inventory -- see RangedAttackAI, which only ever reads from the two dedicated slots.</summary>
    private static void EquipGuaranteedRangedWeapon(Monster monster)
    {
        if (monster.GetEquippedItems().Any(IsRangedWeapon))
        {
            return;
        }

        var bow = Items.Bow.Clone();
        var slot = monster.Equipment.FindAutoEquipSlot(bow);
        if (slot.HasValue)
        {
            var previous = monster.Equipment.EquipInSlot(slot.Value, bow);
            if (previous != null)
            {
                RemoveEquipmentBonus(monster, previous, slot.Value);
                monster.Inventory.AddItem(previous);
            }
            ApplyEquipmentBonus(monster, bow, slot.Value);
        }

        if (monster.Equipment.Get(EquipmentSlot.Ammunition) == null)
        {
            monster.Equipment.EquipInSlot(EquipmentSlot.Ammunition, Items.Arrow.Clone());
        }
    }

    /// <summary>
    /// An enhanced version of a normal level-appropriate archetype -- built by calling
    /// CreateRandom (reusing 100% of archetype selection, capability resolution, and spawn-item
    /// equipping) and then scaling the result in place, rather than a separate boss-only
    /// construction path or per-archetype "Boss" variants. See BossConfig for every scaling
    /// knob and DungeonGenerator.TrySpawnBossRoom for where this is called.
    /// </summary>
    public static Monster CreateBoss(int x, int y, int difficultyLevel, Random rng)
    {
        var boss = CreateRandom(x, y, difficultyLevel, rng);

        boss.IsBoss = true;
        boss.Level += BossConfig.BossLevelBonus;
        boss.BasePhysicalAttackPower = (int)Math.Round(boss.BasePhysicalAttackPower * BossConfig.BossDamageMultiplier);
        boss.BaseMagicalAttackPower = (int)Math.Round(boss.BaseMagicalAttackPower * BossConfig.BossDamageMultiplier);
        boss.DefensePower += BossConfig.BossDefenseBonus;

        int scaledMaxHp = Math.Max(1, (int)Math.Round(boss.Health.Max * BossConfig.BossHealthMultiplier));
        boss.Health = new HealthComponent(scaledMaxHp);

        // Deliberately independent of Name/CreatureType -- see "Boss Monster Naming --
        // Epithet System.txt". A boss should read as an individual legendary creature
        // ("Lormax Golden Wing"), not a stronger version of its species ("Giant Cockroach
        // Chieftain"); the underlying creature type stays discoverable through the
        // description below, and every ordinary gameplay system keeps reading the
        // unchanged Name.
        var identity = BossNameGenerator.GenerateBossIdentity(rng);
        boss.BossName = identity.Name;
        boss.BossEpithet = identity.Epithet;
        boss.BossEpithetFormat = identity.Format;

        const string bossFlavorClause = "This one is far larger, scarred, and more dangerous than others of its kind.";
        boss.ShortDescription = $"{boss.ShortDescription} {bossFlavorClause}";
        boss.LongDescription = $"{boss.LongDescription} {bossFlavorClause}";

        EnsureBossEquipment(boss, rng);

        return boss;
    }

    /// <summary>
    /// Guarantees BossMinItems-BossMaxItems equipped items, at least one carrying a
    /// StatModifier, StatusEffect, blessing, or curse -- reuses this session's own
    /// item-properties system (Item.StatModifiers/StatusEffects/IsBlessed/IsCursed) rather
    /// than a boss-only "special item" concept, so identification/curse-lock/on-hit-effect
    /// behavior all just works unmodified. No MinimumLevel filter -- a boss is already
    /// enhanced, so an item that's "early" for its own stats is fine on one.
    /// </summary>
    private static void EnsureBossEquipment(Monster boss, Random rng)
    {
        int itemCount = rng.Next(BossConfig.BossMinItems, BossConfig.BossMaxItems + 1);
        if (itemCount == 0)
        {
            return;
        }

        var specialCandidates = Items.All
            .Where(i => i.EquipmentType != EquipmentType.None && i.Size <= boss.Size)
            .Where(i => i.StatModifiers.Count > 0 || i.StatusEffects.Count > 0 || i.IsBlessed || i.IsCursed)
            .ToList();

        if (specialCandidates.Count > 0)
        {
            EquipBossItem(boss, specialCandidates[rng.Next(specialCandidates.Count)]);
            itemCount--;
        }

        if (itemCount <= 0)
        {
            return;
        }

        var ordinaryCandidates = Items.All.Where(i => i.EquipmentType != EquipmentType.None && i.Size <= boss.Size).ToList();
        if (ordinaryCandidates.Count > 0)
        {
            EquipBossItem(boss, ordinaryCandidates[rng.Next(ordinaryCandidates.Count)]);
        }
    }

    private static void EquipBossItem(Monster boss, Item template)
    {
        // Same "don't hand out the shared catalog reference" rule as every other spawn path.
        var item = template.Charges != null || !template.IsIdentified ? template.Clone() : template;

        var slot = boss.Equipment.FindAutoEquipSlot(item);
        if (slot.HasValue)
        {
            var previous = boss.Equipment.EquipInSlot(slot.Value, item);
            if (previous != null)
            {
                RemoveEquipmentBonus(boss, previous, slot.Value);
                boss.Inventory.AddItem(previous);
            }
            ApplyEquipmentBonus(boss, item, slot.Value);
        }
        else
        {
            boss.Inventory.AddItem(item);
        }
    }

    /// <summary>
    /// Folds one equipped item's combat bonuses directly into the monster's already-
    /// depth-scaled stats -- simpler than Player's ApplyStatModifiers/RecalculateDerivedStats
    /// pair since a monster's equipment never changes after spawn, so there's no need to
    /// track a separate "base" value to recompute from later. Item.StatModifiers (a
    /// PrimaryAttribute bonus, e.g. Ring of Strength) is deliberately NOT applied here --
    /// Monster has no CharacterStats for that to modify, unlike Player.
    /// </summary>
    private static void ApplyEquipmentBonus(Monster monster, Item item, EquipmentSlot slot)
    {
        monster.CarryCapacityBonus += item.EncumbranceModifier;

        // RangedWeapon/Ammunition bonuses are intentionally excluded -- they apply only to a
        // fired shot's own damage formula (see ProjectileFactory.ForWeaponAndAmmo), never to
        // melee or to this monster's general base attack power. Mirrors Player.
        // RecalculateDerivedStats' identical exclusion for the same reason.
        if (slot is EquipmentSlot.RangedWeapon or EquipmentSlot.Ammunition)
        {
            return;
        }

        monster.BasePhysicalAttackPower += item.PhysicalAttackBonus;
        monster.BaseMagicalAttackPower += item.MagicalAttackBonus;
        monster.DefensePower += item.DefenseBonus;
    }

    /// <summary>Inverse of ApplyEquipmentBonus -- must be called on whatever EquipInSlot displaces before it's set aside, or the displaced item's bonus stays silently baked into the monster's stats forever (mirrors Player.EquipInSlot's RemoveStatModifiers-on-swap behavior).</summary>
    private static void RemoveEquipmentBonus(Monster monster, Item item, EquipmentSlot slot)
    {
        monster.CarryCapacityBonus -= item.EncumbranceModifier;

        if (slot is EquipmentSlot.RangedWeapon or EquipmentSlot.Ammunition)
        {
            return;
        }

        monster.BasePhysicalAttackPower -= item.PhysicalAttackBonus;
        monster.BaseMagicalAttackPower -= item.MagicalAttackBonus;
        monster.DefensePower -= item.DefenseBonus;
    }

    /// <summary>
    /// Fully-resolved snapshot for reconstructing a saved monster -- deliberately NOT
    /// "archetype name + difficulty level to re-derive Attack/HP/Defense from," since a
    /// future archetype rebalance would then silently change an old save's monsters. Holds
    /// Entities-only types (Spell/ActiveEffect) rather than Persistence DTOs so the
    /// name-to-object resolution (SpellCatalog lookups) stays SaveManager's job, keeping
    /// the dependency direction (Persistence -> Entities) correct. See Restore.
    /// </summary>
    public class RestoreData
    {
        public string Name;
        public char Symbol;
        public ConsoleColor Color;
        public int X;
        public int Y;
        public string ShortDescription = "";
        public string LongDescription = "";
        public int Level;
        public int Agility;
        public double DifficultyRating;
        public long? XpRewardOverride;
        public Size Size;
        public AttackType AttackType;
        public CreatureType CreatureType;
        public bool CanCarryItems;
        public bool CanEquipItems;
        public bool CanUseItems;
        public FloorType? PreferredFloorType;
        public DamageType? ElementalAffinity;
        public double OffPreferredFloorDamagePercent = FloorAttunementConfig.DefaultOffPreferredFloorDamagePercent;
        public ResistanceSet BaseResistances = ResistanceSet.Zero;
        public MonsterSoundType SoundType = MonsterSoundType.None;
        public int BasePhysicalAttackPower;
        public int BaseMagicalAttackPower;
        public int DefensePower;
        public int MagicResistance;
        public int Speed;
        public int Energy;
        public int CurrentHp;
        public int MaxHp;
        public int CurrentMana;
        public int MaxMana;
        public bool IsAlerted;
        public bool IsBoss;
        public string BossName;
        public string BossEpithet;
        public EpithetFormat BossEpithetFormat;
        public int StunnedUntilTurn;
        public int SilencedUntilTurn;
        public int CcImmuneUntilTurn;
        public bool IsProne;
        public int FrightenedUntilTurn;
        public List<Spell> KnownSpells = new();
        public Dictionary<Spell, int> SpellCooldowns = new();
        public List<ActiveEffect> ActiveEffects = new();

        /// <summary>Already-resolved (name-to-Item lookup is SaveManager's job, same as KnownSpells above). BasePhysicalAttackPower/BaseMagicalAttackPower/DefensePower above already include whatever bonus these were granting when saved -- restoring them here is for AttackType's equipped-weapon check and Inventory-vs-Equipment fidelity, not to reapply a bonus a second time.</summary>
        public Dictionary<EquipmentSlot, Item> Equipment = new();
    }

    /// <summary>Rebuilds a monster exactly as saved -- mirrors CreateRandom's construction shape, just from already-resolved values instead of an archetype + difficulty formula. AI is re-derived from whether it knows any spells, same rule CreateRandom uses.</summary>
    public static Monster Restore(RestoreData data)
    {
        var monster = new Monster(data.Agility, data.DifficultyRating, data.XpRewardOverride, data.Size, data.AttackType)
        {
            X = data.X,
            Y = data.Y,
            Name = data.Name,
            Symbol = data.Symbol,
            Color = data.Color,
            ShortDescription = data.ShortDescription,
            LongDescription = data.LongDescription,
            Level = data.Level,
            BasePhysicalAttackPower = data.BasePhysicalAttackPower,
            BaseMagicalAttackPower = data.BaseMagicalAttackPower,
            DefensePower = data.DefensePower,
            MagicResistance = data.MagicResistance,
            Speed = data.Speed,
            Energy = data.Energy,
            Health = new HealthComponent(data.MaxHp),
            Mana = new ManaComponent(data.MaxMana),
            IsAlerted = data.IsAlerted,
            IsBoss = data.IsBoss,
            BossName = data.BossName,
            BossEpithet = data.BossEpithet,
            BossEpithetFormat = data.BossEpithetFormat,
            StunnedUntilTurn = data.StunnedUntilTurn,
            SilencedUntilTurn = data.SilencedUntilTurn,
            CcImmuneUntilTurn = data.CcImmuneUntilTurn,
            IsProne = data.IsProne,
            FrightenedUntilTurn = data.FrightenedUntilTurn,
            CreatureType = data.CreatureType,
            CanCarryItems = data.CanCarryItems,
            CanEquipItems = data.CanEquipItems,
            CanUseItems = data.CanUseItems,
            PreferredFloorType = data.PreferredFloorType,
            ElementalAffinity = data.ElementalAffinity,
            OffPreferredFloorDamagePercent = data.OffPreferredFloorDamagePercent,
            BaseResistances = data.BaseResistances ?? ResistanceSet.Zero,
            SoundType = data.SoundType
        };

        monster.Health.SetCurrent(data.CurrentHp);
        monster.Mana.SetCurrent(data.CurrentMana);

        foreach (var spell in data.KnownSpells)
        {
            monster.KnownSpells.Add(spell);
        }
        foreach (var (spell, readyTurn) in data.SpellCooldowns)
        {
            monster.SpellCooldowns[spell] = readyTurn;
        }
        foreach (var effect in data.ActiveEffects)
        {
            monster.ActiveEffects.Add(effect);
        }
        foreach (var (slot, item) in data.Equipment)
        {
            monster.Equipment.EquipInSlot(slot, item);
        }

        monster.AI = monster.GetEquippedItems().Any(IsRangedWeapon)
            ? new RangedAttackAI()
            : monster.KnownSpells.Count > 0 ? new SpellCasterAI()
            : monster.IsFloorAttuned ? new FloorAttunedAI()
            : new ChaseAI();

        return monster;
    }
}
