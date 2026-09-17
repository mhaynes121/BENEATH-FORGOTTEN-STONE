using BENEATH_FORGOTTEN_STONE.Entities.Components;
using BENEATH_FORGOTTEN_STONE.Entities.Proficiency;
using BENEATH_FORGOTTEN_STONE.Entities.Skills;
using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Entities;

public class Player : Actor
{
    public CharacterClass Class { get; }
    public Race Race { get; }
    public CharacterStats Stats { get; }
    public ExperienceComponent Experience { get; }
    public EquipmentComponent Equipment { get; } = new();
    public long Gold { get; private set; }

    /// <summary>Lifetime run statistics -- see AdventureRecord's own doc comment. One fresh instance per new character; persisted separately (see Persistence/AdventureRecordData), never reset by anything short of starting a new character.</summary>
    public AdventureRecord AdventureRecord { get; } = new();

    /// <summary>Physical Warrior/Thief skills known so far -- granted automatically by level (see GrantSkillsForLevel), never learned from an item the way KnownSpells' contents are. Kept separate from KnownSpells so magic-only code (SpellCasterAI, HandleLearnSpell, the "Known Spells" UI) never has to special-case a mixed list.</summary>
    public List<Skill> KnownSkills { get; } = new();

    /// <summary>Mirrors Actor.SpellCooldowns but for Skill -- kept here rather than on Actor since only the player ever knows a skill in this solo game.</summary>
    public Dictionary<Skill, int> SkillCooldowns { get; } = new();

    public bool HasSkill(Skill skill) => KnownSkills.Contains(skill);

    /// <summary>
    /// Ability Proficiency System: per-ability progression, keyed by Skill.ProficiencyId/
    /// Spell.ProficiencyId (a null/empty id means the ability is unranked and never gets an
    /// entry here -- see ProficiencyTracker). Missing entry for a ranked ability's id means it
    /// has never been used yet, which is exactly Novice (proficiency 0) -- see RankOf.
    /// </summary>
    public Dictionary<string, AbilityProficiency> Proficiencies { get; } = new();

    /// <summary>
    /// Proficient (today's unranked, unscaled numbers) for an unranked ability (null/empty id) --
    /// scaling was never meant to touch those. Novice for a ranked ability never yet used (an
    /// AbilityProficiency's own Proficiency starts at 0, which already maps to Novice -- this
    /// just avoids needing an explicit Novice entry for every ranked ability up front). Otherwise
    /// the ability's actual earned rank -- see Proficiencies.
    /// </summary>
    public ProficiencyRank RankOf(string proficiencyId) =>
        string.IsNullOrEmpty(proficiencyId)
            ? ProficiencyRank.Proficient
            : Proficiencies.TryGetValue(proficiencyId, out var proficiency) ? proficiency.Rank : ProficiencyRank.Novice;

    /// <summary>Sneak: reduces ChaseAI/SpellCasterAI's effective detection radius (see DetectionRules) until broken by attacking (see GameLoop.HandleMove) or by a monster noticing anyway. A plain toggle, not duration-based -- "enters a stealth state" reads as an on/off state transition, not a timed buff.</summary>
    public bool IsSneaking { get; set; }

    /// <summary>Poison Weapon: while Level.TurnNumber is below this, a successful player melee hit has a chance to also apply a poison DoT -- see GameLoop.HandleMove.</summary>
    public int PoisonWeaponUntilTurn { get; set; }

    /// <summary>Detect Traps (Thief skill / Mage spell): while Level.TurnNumber is below this, each move rolls a DetectTrapsStat-scaled chance to reveal any trap within GameLoop's TrapRevealRadius -- see GameLoop.RollDetectTraps and DetectTrapsEffect. Being active (this window open) and successfully detecting something on a given roll are different states.</summary>
    public int DetectTrapsUntilTurn { get; set; }

    /// <summary>Which stat powers the roll while DetectTrapsUntilTurn is active -- Agility for the Thief skill, Knowledge for the Mage spell. Meaningless once the window has closed.</summary>
    public PrimaryAttribute DetectTrapsStat { get; set; }

    /// <summary>Intercession (Priest skill): true while the next lethal damage instance is guarded against -- see CombatStatsTracker.ApplyDamage's interception check and GameLoop's per-turn expiry sweep against IntercessionExpiresOnTurn.</summary>
    public bool IntercessionActive { get; set; }

    /// <summary>The turn Intercession's protection window lapses harmlessly if no lethal damage occurs by then -- meaningless while IntercessionActive is false.</summary>
    public int IntercessionExpiresOnTurn { get; set; }

    /// <summary>
    /// Set by CombatStatsTracker.ApplyDamage the instant Intercession actually triggers -- a
    /// static utility method has no messageLog reference of its own to append to directly, so it
    /// leaves the flavor text here for GameLoop.Run's turn loop to flush via AddStatusMessage right
    /// after whichever actor's turn caused the damage (melee, a trap, a DoT tick, environmental
    /// Fire/Lava -- all of them funnel through the same ApplyDamage choke point). Null the rest of
    /// the time; never persisted -- Intercession can only trigger mid-turn, never across a save/load.
    /// </summary>
    public string PendingInterceptionMessage { get; set; }

    /// <summary>Pet and Companion System: the player's own companion, or null if this character has none (see the old-save compatibility rule in PetFactory/SaveManager -- only a NEWLY created character gets one). Deliberately typed to the player rather than a generic Actor.Pets collection -- see Pet's own doc comment on why the Owner concept still generalizes past this.</summary>
    public Pet Pet { get; set; }

    /// <summary>Mirrors PendingInterceptionMessage's own pattern: PetFactory's OnLevelUp subscription has no messageLog reference of its own, so it leaves flavor text (a level-up growth message) here for GameLoop.Run's turn loop to flush. Null the rest of the time; never persisted.</summary>
    public string PendingPetMessage { get; set; }

    /// <summary>PreviousLevel, NewLevel, HpGained, ManaGained. Fired once per level gained -- a single large XP award can fire it several times in a row.</summary>
    public event Action<int, int, int, int> OnLevelUp;

    /// <summary>
    /// Total normal player turns taken this run -- incremented exactly once, from GameLoop's
    /// single centralized "the player just consumed a normal turn" spot (the same place
    /// EffectProcessor.Tick and Player.TurnCount... er, this itself, live), never from
    /// individual commands. A projectile traveling several tiles is still just one player
    /// action, so it never inflates this beyond +1. Never reset on a dungeon-level change --
    /// see DungeonManager/GameLoop, neither of which touch this. Persisted verbatim (see
    /// SaveManager) and shown on the Inventory screen and the death notice.
    /// </summary>
    public long TurnCount { get; set; }

    /// <summary>
    /// Fractional natural-regen progress banked between turns -- most classes now regenerate
    /// well under 1 HP/Mana per turn (see ResourceRegenerationCalculator), so a whole point is
    /// only actually healed once enough turns' worth of partial progress has accumulated (e.g.
    /// a 0.25/turn rate heals +1 every 4th turn, on the dot, rather than the old "round down to
    /// 0 most turns, floor of 1 forced on others" behavior). Deliberately not persisted across
    /// save/load -- losing a fraction of a turn's progress on reload is imperceptible.
    /// </summary>
    public double HpRegenAccumulator { get; set; }

    /// <summary>Same as HpRegenAccumulator, for Mana -- meaningless (stays 0) for a class with no ManaStat.</summary>
    public double ManaRegenAccumulator { get; set; }

    private readonly int basePhysicalAttackPower;
    private readonly int baseMagicalAttackPower;
    private readonly int baseDefensePower;

    public override int Agility => Stats.Adjusted(PrimaryAttribute.Agility);

    /// <summary>The equipped weapon's AttackType (checking PrimaryHand then OffHand), or Hit when unarmed or holding something non-physical (e.g. a wand). See Item.AttackType.</summary>
    public override AttackType AttackType =>
        Equipment.Get(EquipmentSlot.PrimaryHand)?.AttackType ??
        Equipment.Get(EquipmentSlot.OffHand)?.AttackType ??
        AttackType.Hit;

    public override IEnumerable<Item> GetEquippedItems() => Equipment.AllEquipped.Select(kvp => kvp.Value);

    /// <summary>Race + Class + Constitution/Wisdom (stat-derived) + Equipment (cursed and not) + currently-active buffs/debuffs, clamped to ResistanceConfig's Min/Max -- see the Resistance System spec. Constitution governs Fire/Water/Ice/Shock/Poison; Wisdom governs Magic (sections 6/7). A player has no PreferredFloorType, so unlike Monster.GetEffectiveResistance there's no environmental term here.</summary>
    public int GetEffectiveResistance(ResistanceType type)
    {
        int total = Race.ResistanceModifiers.Get(type)
            + Class.ResistanceModifiers.Get(type)
            + GetStatResistanceModifier(type)
            + ResistanceCalculator.SumEquipmentResistance(this, type, cursedOnly: false)
            + ResistanceCalculator.SumEquipmentResistance(this, type, cursedOnly: true)
            + ResistanceCalculator.SumActiveEffectResistance(this, type);
        return Math.Clamp(total, ResistanceConfig.MinimumResistance, ResistanceConfig.MaximumResistance);
    }

    /// <summary>Unclamped, per-source contributions ending with the clamped Effective total -- not shown on the compact inventory line (which just calls GetEffectiveResistance directly), but available for debugging/tooltips/future UI and for tests (design spec section 41).</summary>
    public IReadOnlyList<(string Source, int Amount)> GetResistanceBreakdown(ResistanceType type) => new (string, int)[]
    {
        ("Race", Race.ResistanceModifiers.Get(type)),
        ("Class", Class.ResistanceModifiers.Get(type)),
        ("Stat", GetStatResistanceModifier(type)),
        ("Equipment", ResistanceCalculator.SumEquipmentResistance(this, type, cursedOnly: false)),
        ("Curse", ResistanceCalculator.SumEquipmentResistance(this, type, cursedOnly: true)),
        ("Buff/Debuff", ResistanceCalculator.SumActiveEffectResistance(this, type)),
        ("Environment", 0),
        ("Effective", GetEffectiveResistance(type))
    };

    private int GetStatResistanceModifier(ResistanceType type) => type == ResistanceType.Magic
        ? ResistanceStatModifiers.GetWisdomMagicModifier(Stats.Adjusted(PrimaryAttribute.Wisdom))
        : ResistanceStatModifiers.GetConstitutionModifier(Stats.Adjusted(PrimaryAttribute.Constitution));

    public Player(string name, CharacterClass characterClass, Race race, CharacterStats stats)
    {
        Class = characterClass;
        Race = race;
        Stats = stats;
        Name = name;
        Symbol = '@';
        Color = ConsoleColor.Yellow;
        Speed = characterClass.BaseSpeed;
        basePhysicalAttackPower = characterClass.BasePhysicalAttack;
        baseMagicalAttackPower = characterClass.BaseMagicAttack;
        baseDefensePower = characterClass.BaseDefense;
        Health = new HealthComponent(characterClass.BaseHealth);
        Mana = new ManaComponent(characterClass.BaseMana);
        Experience = new ExperienceComponent(LevelProgression.GetXpRequiredForNextLevel(characterClass, Level));
        AI = null;
        RecalculateDerivedStats();

        foreach (var spell in StartingSpellsFor(characterClass))
        {
            KnownSpells.Add(spell);
        }

        GrantSkillsForLevel(Level);
    }

    /// <summary>
    /// Grants every skill unlocking at exactly this level for this class --
    /// unlike StartingSpellsFor's single random pick, skill unlocks are a
    /// deterministic, fixed table (see SkillCatalog), so every eligible
    /// entry is granted, not one chosen at random. Called once at
    /// construction (level 1) and again from AddExperience's level-up loop
    /// for every level gained after that -- including several at once from
    /// a single large XP award, since AddExperience calls this once per
    /// Level++ inside its own loop rather than once per AddExperience call.
    /// </summary>
    public void GrantSkillsForLevel(int level)
    {
        foreach (var skill in SkillCatalog.All.Where(s => s.AllowedClasses.Contains(Class) && s.LevelFor(Class) == level))
        {
            KnownSkills.Add(skill);
            // Passives apply their one-shot permanent effect right here -- they never go
            // through SkillCaster.Cast, which would need a fabricated context/turn number
            // and would wrongly produce a "You use X" status message for a non-action.
            skill.OnGrant?.Invoke(this);
        }
    }

    /// <summary>Character-creation-only randomness -- mirrors StatRollScreen's own local Random rather than threading GameLoop's shared rng through Player, which doesn't exist yet at this point.</summary>
    private static readonly Random creationRng = new();

    /// <summary>
    /// Every other spell (including the rest of what used to be each class's
    /// fixed starting kit) must be learned from a scroll (Mage) or spellbook
    /// (Priest) now -- see SpellScrollCatalog, SpellbookCatalog, and
    /// GameLoop.HandleLearnSpell. Non-spellcasting classes get nothing, same
    /// as before.
    /// </summary>
    private static IEnumerable<Spell> StartingSpellsFor(CharacterClass characterClass)
    {
        if (!characterClass.IsSpellcaster)
        {
            return Array.Empty<Spell>();
        }

        var candidates = SpellCatalog.All.Where(s => s.Level == 1 && s.CanBeCastBy(characterClass)).ToList();
        if (candidates.Count == 0)
        {
            return Array.Empty<Spell>();
        }

        return new[] { candidates[creationRng.Next(candidates.Count)] };
    }

    /// <returns>The item previously in that slot, if any (so the caller can return it to inventory).</returns>
    public Item EquipInSlot(EquipmentSlot slot, Item item)
    {
        var previous = Equipment.EquipInSlot(slot, item);
        if (previous != null)
        {
            RemoveStatModifiers(previous);
        }
        ApplyStatModifiers(item);
        RecalculateDerivedStats();
        return previous;
    }

    /// <returns>The item that was in that slot, or null if it was already empty.</returns>
    public Item Unequip(EquipmentSlot slot)
    {
        var item = Equipment.Unequip(slot);
        if (item != null)
        {
            RemoveStatModifiers(item);
            RecalculateDerivedStats();
        }
        return item;
    }

    private void ApplyStatModifiers(Item item)
    {
        foreach (var (attribute, amount) in item.StatModifiers)
        {
            Stats.Get(attribute).EquipmentModifier += amount;
        }

        int penalty = EquipmentCompatibility.AgilityPenaltyForWeight(Class, item.ArmorWeight);
        if (penalty != 0)
        {
            Stats.Get(PrimaryAttribute.Agility).EquipmentModifier -= penalty;
        }

        CarryCapacityBonus += item.EncumbranceModifier;
    }

    private void RemoveStatModifiers(Item item)
    {
        foreach (var (attribute, amount) in item.StatModifiers)
        {
            Stats.Get(attribute).EquipmentModifier -= amount;
        }

        int penalty = EquipmentCompatibility.AgilityPenaltyForWeight(Class, item.ArmorWeight);
        if (penalty != 0)
        {
            Stats.Get(PrimaryAttribute.Agility).EquipmentModifier += penalty;
        }

        CarryCapacityBonus -= item.EncumbranceModifier;
    }

    /// <summary>
    /// Sums PhysicalAttackBonus/MagicalAttackBonus/DefenseBonus across every
    /// currently-equipped item (there can now be several at once, e.g. two
    /// hand items), plus a Strength-derived physical attack bonus so damage
    /// output actually grows over a playthrough instead of being frozen at
    /// the class base value. Baseline-centered at STR 10 -- see
    /// CharacterClass.StrengthAttackMultiplier.
    /// </summary>
    private void RecalculateDerivedStats()
    {
        int physicalBonus = 0, magicalBonus = 0, defenseBonus = 0;
        foreach (var kvp in Equipment.AllEquipped)
        {
            // RangedWeapon/Ammunition bonuses are intentionally excluded here -- they apply only
            // to a fired shot's own damage formula (see ProjectileFactory.ForWeaponAndAmmo),
            // never to melee or to this player's general base attack power. Folding them in here
            // would double-count them (added again by ForWeaponAndAmmo) AND incorrectly let a
            // launcher's own PhysicalAttackBonus buff an unrelated melee attack.
            if (kvp.Key is EquipmentSlot.RangedWeapon or EquipmentSlot.Ammunition)
            {
                continue;
            }
            physicalBonus += kvp.Value.PhysicalAttackBonus;
            magicalBonus += kvp.Value.MagicalAttackBonus;
            defenseBonus += kvp.Value.DefenseBonus;
        }

        int strengthBonus = (int)Math.Floor((Stats.Adjusted(PrimaryAttribute.Strength) - 10) * Class.StrengthAttackMultiplier);

        BasePhysicalAttackPower = basePhysicalAttackPower + physicalBonus + strengthBonus;
        BaseMagicalAttackPower = baseMagicalAttackPower + magicalBonus;
        DefensePower = baseDefensePower + defenseBonus;
    }

    /// <summary>Gameplay gold gain -- monster/boss rewards, selling an item, any future chest/room/quest/pickup gold. Counts toward AdventureRecord.GoldCollected. See RestoreGold for the save-loading counterpart, which deliberately does not.</summary>
    public void CollectGold(long amount)
    {
        if (amount > 0)
        {
            Gold += amount;
            AdventureRecord.GoldCollected += amount;
        }
    }

    /// <summary>Gameplay gold cost -- buying from a trader, paying for identification, any future purchase/service/toll/fee. Counts toward AdventureRecord.GoldSpent only on success; a failed (insufficient-funds) attempt never touches either counter.</summary>
    public bool SpendGold(long amount)
    {
        if (amount <= 0 || amount > Gold)
        {
            return false;
        }
        Gold -= amount;
        AdventureRecord.GoldSpent += amount;
        return true;
    }

    /// <summary>Sets current gold directly from a save (or a test fixture priming a starting amount) without it ever counting as newly "collected" -- the gold already existed, this just reattaches it. Internal since only SaveManager (and SelfTest, same assembly) has a legitimate reason to bypass the normal Collect/Spend bookkeeping.</summary>
    internal void RestoreGold(long amount) => Gold = Math.Max(0, amount);

    /// <summary>
    /// Adds XP and processes every level-up it triggers -- a single large
    /// award can cross several thresholds in one call. Growing HP/Mana per
    /// level uses ResourceProgressionCalculator, keyed to the CURRENT
    /// adjusted Constitution/magic stat at the moment each level lands.
    /// Primary attributes (STR/CON/AGI/WIS/KNO) are never touched here --
    /// leveling only grows HP/Mana and grants skills; the only things that
    /// can raise or lower a primary attribute are equipment (EquipmentModifier)
    /// and spell/skill effects, never level-up itself.
    /// </summary>
    public void AddExperience(long amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Experience.Current += amount;

        while (Experience.Current >= Experience.ToNextLevel)
        {
            Experience.Current -= Experience.ToNextLevel;
            int previousLevel = Level;
            Level++;

            int hpGain = ResourceProgressionCalculator.CalculateHpGain(Class, Stats);
            int manaGain = ResourceProgressionCalculator.CalculateManaGain(Class, Stats);
            Health.IncreaseMax(hpGain);
            if (manaGain > 0)
            {
                Mana.IncreaseMax(manaGain);
            }

            GrantSkillsForLevel(Level);

            Experience.ToNextLevel = LevelProgression.GetXpRequiredForNextLevel(Class, Level);
            OnLevelUp?.Invoke(previousLevel, Level, hpGain, manaGain);
        }
    }
}
