using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Entities.Skills;

/// <summary>
/// A physical Warrior/Thief skill definition -- mirrors Spell's shape
/// (identity + configuration + effects), but stays a separate type rather
/// than a zero-mana Spell: several skills need gating fields meaningless
/// for any of the 39 magic spells (RequiresShield, target-HP% thresholds),
/// and keeping player.KnownSpells magic-only means SpellCasterAI,
/// HandleLearnSpell's scroll-reading, and the Character Sheet's "Known
/// Spells" section never need to special-case a mixed list. The underlying
/// effect engine (SpellEffect/SpellTargetingConfiguration/
/// SpellCastingContext/SpellCastResult) is reused directly -- see
/// SkillCaster. Immutable and shared, same as Spell/CharacterClass/Race.
/// </summary>
public class Skill
{
    public string Name { get; }
    public string Description { get; }
    public char DisplayCharacter { get; }

    /// <summary>Null for a pure passive (see IsPassive) -- never read by SkillCaster.Cast, which passives never go through.</summary>
    public SkillCastingConfiguration Casting { get; }

    public SpellTargetingConfiguration Targeting { get; }
    public IReadOnlyList<SpellEffect> Effects { get; }

    /// <summary>The default character level this skill is automatically granted at -- see Player.GrantSkillsForLevel. Unlike Spell.Level (a floor a known-but-not-yet-eligible spell might not clear yet), this IS the grant trigger: every skill at or below the character's level for their class is already known, so SkillRequirementValidator never needs a separate level check. Used as-is for any class in AllowedClasses that has no entry in LevelOverrides.</summary>
    public int Level { get; }

    /// <summary>
    /// Per-class exceptions to Level -- e.g. Bash is a Warrior skill at its base Level, but also
    /// available to Priest at double that level (floored at 5), since a Priest picking up a
    /// shield-bash technique is a later, secondary option for them rather than a core kit piece.
    /// Null (the common case) means every allowed class simply uses Level -- see LevelFor.
    /// </summary>
    public IReadOnlyDictionary<CharacterClass, int> LevelOverrides { get; }

    public IReadOnlyList<CharacterClass> AllowedClasses { get; }

    /// <summary>The actual unlock level for a specific class -- LevelOverrides[characterClass] if one exists, otherwise the plain Level every other allowed class uses. Player.GrantSkillsForLevel, InventoryScreen's Spells & Skills tab, and the Ability Proficiency System's catalog-level learning rate all read this instead of Level directly, so a class-specific override is honored everywhere the skill's level matters.</summary>
    public int LevelFor(CharacterClass characterClass) =>
        LevelOverrides != null && LevelOverrides.TryGetValue(characterClass, out int overrideLevel) ? overrideLevel : Level;

    /// <summary>Passives (Hardy Constitution, Stalwart Defender, Fleet Footed) never go through SkillCaster.Cast -- OnGrant applies their one-shot permanent effect the moment they're granted, and they're never "used" again afterward.</summary>
    public bool IsPassive { get; }

    /// <summary>Shield Block/Shield Slam: SkillCaster.Cast rejects the cast with a specific message if the caster has no shield equipped.</summary>
    public bool RequiresShield { get; }

    /// <summary>Exorcism: SkillCaster.Cast rejects the cast with a specific message unless a Hand-slot item with EquipmentCategory.Weapon is equipped in either hand -- bare hands and a Wand both fail this the same as an empty slot does.</summary>
    public bool RequiresMeleeWeapon { get; }

    /// <summary>Assassinate: the cast only takes effect if the target's health is at or above this fraction of its max (1.0 = full health only). Null = no gate.</summary>
    public double? TargetHealthPercentAtLeast { get; }

    /// <summary>Executioner: the cast only takes effect if the target's health is below this fraction of its max. Null = no gate.</summary>
    public double? TargetHealthPercentBelow { get; }

    /// <summary>Backstab: rejected outright unless the caster is Sneaking and the target Monster hasn't noticed anyone yet (Monster.IsAlerted). "Behind the target" isn't meaningful without a facing system, so this is the accepted stand-in.</summary>
    public bool RequiresUnalertedSneakTarget { get; }

    /// <summary>Passives only: applies this skill's one-shot permanent effect once, at the moment it's granted (character creation or level-up) -- see Player.GrantSkillsForLevel. Never invoked for an active skill.</summary>
    public Action<Player> OnGrant { get; }

    /// <summary>
    /// Ability Proficiency System (spec section 25): the stable, persistence-safe key into
    /// Player.Proficiencies, e.g. "skill.warrior.bash" -- never the display Name, which can be
    /// (theoretically) changed without breaking a save. Null/empty means this skill never ranks
    /// up at all -- the one-shot permanent passives (Hardy Constitution, Stalwart Defender,
    /// Fleet Footed), since "their effects occur only once when granted" (spec section 18).
    /// </summary>
    public string ProficiencyId { get; }

    /// <summary>The primary attribute that governs how fast this skill's proficiency accrues (spec section 9) -- null exactly when ProficiencyId is null.</summary>
    public PrimaryAttribute? GoverningAttribute { get; }

    /// <summary>
    /// Overrides SkillCaster.BuildMessage's generic "{caster} uses {name}." opening line --
    /// mirrors Spell.FlavorCastMessage exactly (e.g. Turn Undead's "You raise your holy symbol
    /// and invoke sacred authority!"). Null (the default) for every existing skill, which keeps
    /// the generic line exactly as before.
    /// </summary>
    public string FlavorCastMessage { get; }

    /// <summary>
    /// New Priest Skill Progression: a free-form precondition checked once target resolution (and
    /// the health-threshold/unalerted-sneak gates above) has already succeeded, but before the
    /// cooldown is set or any effect runs -- returns a failure message to reject the cast for free,
    /// or null to proceed. Covers the handful of per-skill rejections that don't fit an existing
    /// named boolean gate (Lay on Hands: already at full health; Purify: nothing to purify; Turn
    /// Undead: no eligible undead in context.AffectedActors; Exorcism: target isn't Undead). Null
    /// (the default) for every other skill.
    /// </summary>
    public Func<SpellCastingContext, string> PreconditionCheck { get; }

    public Skill(
        string name, string description, char displayCharacter,
        int level, IReadOnlyList<CharacterClass> allowedClasses,
        SkillCastingConfiguration casting = null, SpellTargetingConfiguration targeting = null,
        IReadOnlyList<SpellEffect> effects = null,
        bool isPassive = false, bool requiresShield = false, bool requiresMeleeWeapon = false,
        double? targetHealthPercentAtLeast = null, double? targetHealthPercentBelow = null,
        bool requiresUnalertedSneakTarget = false,
        Action<Player> onGrant = null,
        string proficiencyId = null, PrimaryAttribute? governingAttribute = null,
        IReadOnlyDictionary<CharacterClass, int> levelOverrides = null,
        string flavorCastMessage = null, Func<SpellCastingContext, string> preconditionCheck = null)
    {
        Name = name;
        Description = description;
        DisplayCharacter = displayCharacter;
        Level = level;
        LevelOverrides = levelOverrides;
        AllowedClasses = allowedClasses ?? Array.Empty<CharacterClass>();
        Casting = casting;
        Targeting = targeting;
        Effects = effects ?? Array.Empty<SpellEffect>();
        IsPassive = isPassive;
        RequiresShield = requiresShield;
        RequiresMeleeWeapon = requiresMeleeWeapon;
        TargetHealthPercentAtLeast = targetHealthPercentAtLeast;
        TargetHealthPercentBelow = targetHealthPercentBelow;
        RequiresUnalertedSneakTarget = requiresUnalertedSneakTarget;
        OnGrant = onGrant;
        ProficiencyId = proficiencyId;
        GoverningAttribute = governingAttribute;
        FlavorCastMessage = flavorCastMessage;
        PreconditionCheck = preconditionCheck;
    }
}
