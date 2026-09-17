namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// A player character class (Warrior, Thief, Priest, Mage, ...). Each
/// instance is just a named base-stat template applied once at character
/// creation -- mirroring how Monster.Archetype supplies stats for
/// monsters -- rather than a subclass per class, so adding a new class
/// is a one-line addition here instead of a new type.
/// </summary>
public class CharacterClass
{
    public string Name { get; }
    public string Description { get; }
    public int BaseHealth { get; }
    public int BaseMana { get; }
    public int BasePhysicalAttack { get; }
    public int BaseMagicAttack { get; }
    public int BaseDefense { get; }
    public int BaseSpeed { get; }

    /// <summary>Per-level-up growth curve: XPRequired = BaseXp * level^GrowthRate. See LevelProgression.</summary>
    public double BaseXp { get; }
    public double GrowthRate { get; }

    /// <summary>HP gained per level-up = BaseHPGain + floor(adjusted CON * HpMultiplier). See ResourceProgressionCalculator.</summary>
    public double HpMultiplier { get; }

    /// <summary>Mana gained per level-up; 0/null (ManaStat) for non-casters. See ResourceProgressionCalculator.</summary>
    public double ManaMultiplier { get; }
    public PrimaryAttribute? ManaStat { get; }

    /// <summary>Whether this class can cast spells at all -- derived from ManaStat rather than a separate flag, so a future spellcasting class automatically qualifies just by having one. Drives Spell.AllSpellcasters gating.</summary>
    public bool IsSpellcaster => ManaStat.HasValue;

    /// <summary>
    /// Physical attack bonus from Strength: floor((AdjustedSTR - 10) * this).
    /// Baseline-centered at 10 (an average roll) rather than STR * multiplier
    /// directly, so a fresh character doesn't get an un-earned jump in
    /// attack at creation -- the bonus grows in as STR rises above average
    /// via leveling (Player.AddExperience adds +1 STR/level) or gear.
    /// </summary>
    public double StrengthAttackMultiplier { get; }

    public IReadOnlyDictionary<PrimaryAttribute, int> StatModifiers { get; }

    /// <summary>Weapon categories this class may equip -- see EquipmentCompatibility.IsAllowedForClass. Data-driven rather than special-cased per class, so a new class is just a new set here.</summary>
    public IReadOnlyCollection<WeaponType> AllowedWeaponTypes { get; }

    /// <summary>Armor weight classes this class may equip, checked against any worn-armor item's material (Body, Head, Gloves, Wrists, Arms, Legs, Feet). Rings and Neck items (jewelry, not armor material) aren't weight-restricted at all.</summary>
    public IReadOnlyCollection<ArmorWeight> AllowedArmorWeights { get; }

    /// <summary>Which shield sizes (Item.Size on a Shield-category item) this class may equip -- see EquipmentCompatibility.IsAllowedForClass. Empty means this class can't use a shield at all; a class can be restricted to just the smaller ones (e.g. Thief -> Small only) rather than an all-or-nothing flag.</summary>
    public IReadOnlyCollection<Size> AllowedShieldSizes { get; }

    /// <summary>Which weapon sizes (Item.Size on a Weapon-category item) this class may wield -- see EquipmentCompatibility.IsAllowedForClass. Defaults to every size (no restriction) when not specified, same "opt into a restriction" shape as AllowedShieldSizes; only Thief currently narrows this (Small/Medium only -- no Large weapons like the Long Sword).</summary>
    public IReadOnlyCollection<Size> AllowedWeaponSizes { get; }

    /// <summary>Which CanBeThrown items' ThrowableCategory this class may throw -- see GameLoop.HandleFireProjectile's throwables filter. Independent of AllowedWeaponTypes: a class can throw something (a Spear) it could never wield in melee, and vice versa. Empty (the default) means this class can't throw anything.</summary>
    public IReadOnlyCollection<ThrowableCategory> AllowedThrowableCategories { get; }

    /// <summary>
    /// Natural HP regen rate = Adjusted(CON) / HpRegenDivisor, in HP per turn (typically well
    /// under 1 -- see Player.HpRegenAccumulator for how a sub-1 rate still produces whole-point
    /// heals on a regular cadence, not on a clean turn boundary in general). A lower divisor
    /// means faster regen: Warrior 64 (fastest -- a 16 CON Warrior heals every 4 turns),
    /// Thief/Priest 89, Mage 114 (slowest -- spending vitality on magic instead of physical
    /// toughness). All three keep the same relative spacing the user originally set (46:64:82),
    /// rescaled by 64/46 once the Warrior's own reference point was retuned.
    /// </summary>
    public double HpRegenDivisor { get; }

    /// <summary>Natural mana regen rate = Adjusted(ManaStat) / ManaRegenDivisor, in mana per turn -- meaningless (never read) for Warrior/Thief, which have no ManaStat at all. Both spellcasting classes share the same divisor (64, mirroring Warrior's own HP divisor, same relationship as before rescaling).</summary>
    public double ManaRegenDivisor { get; }

    /// <summary>Small specialized elemental/magic resistance tendencies -- see the Resistance System spec. Defaults to ResistanceSet.Zero so a class without explicit values behaves exactly as before this existed.</summary>
    public ResistanceSet ResistanceModifiers { get; }

    private CharacterClass(
        string name, string description,
        int baseHealth, int baseMana, int basePhysicalAttack, int baseMagicAttack, int baseDefense, int baseSpeed,
        double baseXp, double growthRate, double hpMultiplier, double manaMultiplier, PrimaryAttribute? manaStat,
        double strengthAttackMultiplier,
        IReadOnlyDictionary<PrimaryAttribute, int> statModifiers,
        IReadOnlyCollection<WeaponType> allowedWeaponTypes, IReadOnlyCollection<ArmorWeight> allowedArmorWeights,
        IReadOnlyCollection<Size> allowedShieldSizes,
        IReadOnlyCollection<ThrowableCategory> allowedThrowableCategories = null,
        IReadOnlyCollection<Size> allowedWeaponSizes = null,
        double hpRegenDivisor = 36, double manaRegenDivisor = 36, ResistanceSet resistanceModifiers = null)
    {
        if (baseXp <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseXp), "BaseXp must be positive.");
        }
        if (growthRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(growthRate), "GrowthRate must be positive.");
        }

        Name = name;
        Description = description;
        BaseHealth = baseHealth;
        BaseMana = baseMana;
        BasePhysicalAttack = basePhysicalAttack;
        BaseMagicAttack = baseMagicAttack;
        BaseDefense = baseDefense;
        BaseSpeed = baseSpeed;
        BaseXp = baseXp;
        GrowthRate = growthRate;
        HpMultiplier = hpMultiplier;
        ManaMultiplier = manaMultiplier;
        ManaStat = manaStat;
        StrengthAttackMultiplier = strengthAttackMultiplier;
        StatModifiers = statModifiers;
        AllowedWeaponTypes = allowedWeaponTypes;
        AllowedArmorWeights = allowedArmorWeights;
        AllowedShieldSizes = allowedShieldSizes ?? Array.Empty<Size>();
        AllowedThrowableCategories = allowedThrowableCategories ?? Array.Empty<ThrowableCategory>();
        AllowedWeaponSizes = allowedWeaponSizes ?? new[] { Size.Small, Size.Medium, Size.Large };
        HpRegenDivisor = hpRegenDivisor;
        ManaRegenDivisor = manaRegenDivisor;
        ResistanceModifiers = resistanceModifiers ?? ResistanceSet.Zero;
    }

    public static readonly CharacterClass Warrior = new(
        "Warrior", "Frontline fighter: high health and defense, hits hard in melee.",
        baseHealth: 30, baseMana: 0, basePhysicalAttack: 6, baseMagicAttack: 0, baseDefense: 3, baseSpeed: 9,
        baseXp: 100, growthRate: 1.20, hpMultiplier: 1.00, manaMultiplier: 0, manaStat: null,
        strengthAttackMultiplier: 0.6,
        statModifiers: new Dictionary<PrimaryAttribute, int>
        {
            [PrimaryAttribute.Strength] = 1,
            [PrimaryAttribute.Constitution] = 1,
            [PrimaryAttribute.Agility] = 0,
            [PrimaryAttribute.Wisdom] = -1,
            [PrimaryAttribute.Knowledge] = -1,
            [PrimaryAttribute.Charisma] = -1,
            [PrimaryAttribute.Luck] = 0
        },
        // Broadest access of any class -- everything except Wand, which stays caster-specific.
        allowedWeaponTypes: new[] { WeaponType.Dagger, WeaponType.Sword, WeaponType.Axe, WeaponType.Blunt, WeaponType.Bow, WeaponType.Crossbow, WeaponType.Sling, WeaponType.Spear },
        allowedArmorWeights: new[] { ArmorWeight.Light, ArmorWeight.Medium, ArmorWeight.Heavy },
        // Any shield size, same as a Warrior's broad access to everything else.
        allowedShieldSizes: new[] { Size.Small, Size.Medium, Size.Large },
        // A real thrown weapon (knife/axe/spear) needs martial training -- a Warrior has it.
        allowedThrowableCategories: new[] { ThrowableCategory.Martial },
        hpRegenDivisor: 64,
        resistanceModifiers: new ResistanceSet(fire: 3, ice: 3, poison: 3));

    public static readonly CharacterClass Thief = new(
        "Thief", "Fast and fragile: acts more often than any other class, but can't take a beating.",
        baseHealth: 16, baseMana: 0, basePhysicalAttack: 4, baseMagicAttack: 0, baseDefense: 1, baseSpeed: 13,
        baseXp: 110, growthRate: 1.10, hpMultiplier: 0.80, manaMultiplier: 0, manaStat: null,
        strengthAttackMultiplier: 0.4,
        statModifiers: new Dictionary<PrimaryAttribute, int>
        {
            [PrimaryAttribute.Strength] = -1,
            [PrimaryAttribute.Constitution] = 0,
            [PrimaryAttribute.Agility] = 2,
            [PrimaryAttribute.Wisdom] = 0,
            [PrimaryAttribute.Knowledge] = 1,
            [PrimaryAttribute.Charisma] = 1,
            [PrimaryAttribute.Luck] = 0
        },
        // Light and fast -- no Axe/Blunt (too heavy). Medium armor is allowed (leather is a
        // Thief's classic gear), but unlike Warrior/Priest it isn't free for this class:
        // EquipmentCompatibility.AgilityPenaltyForWeight costs a Thief agility for anything
        // heavier than Light -- the other classes wear Medium/Heavy for free.
        // Bow/Sling fit the classic light-and-fast archer/skirmisher role; Crossbow/Spear stay Warrior-only (heavier gear).
        allowedWeaponTypes: new[] { WeaponType.Dagger, WeaponType.Sword, WeaponType.Bow, WeaponType.Sling },
        allowedArmorWeights: new[] { ArmorWeight.Light, ArmorWeight.Medium },
        // Small shields only -- light and quick enough not to slow a Thief down, unlike the
        // bulkier Medium/Large ones a Warrior/Priest can strap on.
        allowedShieldSizes: new[] { Size.Small },
        // Everything a Warrior can throw, plus exotic (Shuriken) and light (Dart) tools no
        // other physical class trains with.
        allowedThrowableCategories: new[] { ThrowableCategory.Martial, ThrowableCategory.Exotic, ThrowableCategory.Light },
        // No Large weapons (Long Sword, Broadsword, ...) -- a Thief is limited to small/medium
        // gear (Dagger, Short Sword, Rapier, Bow) it can wield quickly, same "light and fast"
        // reasoning as the Small-only shield restriction above.
        allowedWeaponSizes: new[] { Size.Small, Size.Medium },
        hpRegenDivisor: 89,
        resistanceModifiers: new ResistanceSet(shock: 3, poison: 4));

    public static readonly CharacterClass Priest = new(
        "Priest", "Durable support: modest offense, best survivability outside the Warrior.",
        baseHealth: 22, baseMana: 25, basePhysicalAttack: 3, baseMagicAttack: 3, baseDefense: 2, baseSpeed: 10,
        baseXp: 85, growthRate: 1.27, hpMultiplier: 0.65, manaMultiplier: 0.90, manaStat: PrimaryAttribute.Wisdom,
        strengthAttackMultiplier: 0.25,
        statModifiers: new Dictionary<PrimaryAttribute, int>
        {
            [PrimaryAttribute.Strength] = -1,
            [PrimaryAttribute.Constitution] = 0,
            [PrimaryAttribute.Agility] = -1,
            [PrimaryAttribute.Wisdom] = 2,
            [PrimaryAttribute.Knowledge] = 1,
            [PrimaryAttribute.Charisma] = 1,
            [PrimaryAttribute.Luck] = 0
        },
        // Blunt weapons only (no edged Sword/Axe -- traditional "priests don't draw blood" restriction),
        // any armor weight, and a shield -- as durable as a Warrior in what they can wear, even
        // though their weapon options stay far narrower.
        allowedWeaponTypes: new[] { WeaponType.Blunt, WeaponType.Dagger },
        allowedArmorWeights: new[] { ArmorWeight.Light, ArmorWeight.Medium, ArmorWeight.Heavy },
        // Any shield size -- as durable as a Warrior in what they can wear.
        allowedShieldSizes: new[] { Size.Small, Size.Medium, Size.Large },
        hpRegenDivisor: 89, manaRegenDivisor: 64,
        resistanceModifiers: new ResistanceSet(poison: 4, magic: 5));

    public static readonly CharacterClass Mage = new(
        "Mage", "Glass cannon: lowest health and no defense to speak of, but the strongest hits.",
        baseHealth: 13, baseMana: 30, basePhysicalAttack: 1, baseMagicAttack: 5, baseDefense: 0, baseSpeed: 10,
        baseXp: 85, growthRate: 1.27, hpMultiplier: 0.50, manaMultiplier: 1.00, manaStat: PrimaryAttribute.Knowledge,
        strengthAttackMultiplier: 0.15,
        statModifiers: new Dictionary<PrimaryAttribute, int>
        {
            [PrimaryAttribute.Strength] = -2,
            [PrimaryAttribute.Constitution] = -2,
            [PrimaryAttribute.Agility] = -1,
            [PrimaryAttribute.Wisdom] = 1,
            [PrimaryAttribute.Knowledge] = 2,
            [PrimaryAttribute.Charisma] = -1,
            [PrimaryAttribute.Luck] = 0
        },
        // Least equipment access of any class -- a Dagger in a pinch, otherwise Wand, Light armor, no shield.
        allowedWeaponTypes: new[] { WeaponType.Dagger, WeaponType.Wand },
        allowedArmorWeights: new[] { ArmorWeight.Light },
        allowedShieldSizes: Array.Empty<Size>(),
        // No martial or exotic training -- just the improvised objects anyone could throw.
        allowedThrowableCategories: new[] { ThrowableCategory.Light },
        hpRegenDivisor: 114, manaRegenDivisor: 64,
        resistanceModifiers: new ResistanceSet(fire: 2, ice: 2, poison: -2, magic: 5));

    public static readonly IReadOnlyList<CharacterClass> All = new[] { Warrior, Thief, Priest, Mage };
}
