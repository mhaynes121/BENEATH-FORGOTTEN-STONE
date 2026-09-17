namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

/// <summary>
/// A spell definition: identity + composed configuration + a list of
/// effects. Immutable and shared -- the same instance is cast by every
/// caster who knows it, the same way CharacterClass/Race are shared
/// templates rather than per-character copies.
/// </summary>
public class Spell
{
    public string Name { get; }
    public string Description { get; }
    public char DisplayCharacter { get; }
    public SpellCastingConfiguration Casting { get; }
    public SpellTargetingConfiguration Targeting { get; }
    public IReadOnlyList<SpellEffect> Effects { get; }

    /// <summary>Minimum character level required to cast this (a known-but-not-yet-castable spell can still be granted early -- see Player.StartingSpellsFor). Not checked for monster or item-triggered casts, which have no character level/class of their own. See SpellRequirementValidator.</summary>
    public int Level { get; }

    /// <summary>Classes permitted to cast this, unless AllSpellcasters is set. Ignored (and may be empty) when AllSpellcasters is true.</summary>
    public IReadOnlyList<CharacterClass> AllowedClasses { get; }

    /// <summary>When true, any class with CharacterClass.IsSpellcaster may cast this regardless of AllowedClasses -- future spellcasting classes qualify automatically, with no change needed here.</summary>
    public bool AllSpellcasters { get; }

    /// <summary>
    /// Overrides SpellCaster.BuildMessage's generic "{caster} casts {name}." opening line with
    /// something more evocative -- e.g. Arcane Orb's "A pale orb of arcane light flickers into
    /// existence." Null (the default) for every existing spell, which keeps the generic line
    /// exactly as before; damage/heal/identify clauses still append normally either way.
    /// </summary>
    public string FlavorCastMessage { get; }

    /// <summary>
    /// Ability Proficiency System (spec section 25): the stable, persistence-safe key into
    /// Player.Proficiencies, e.g. "spell.magic_missile" -- never the display Name. Null/empty
    /// means this spell never ranks up -- the shared Identify spell "remains unranked because it
    /// always succeeds and has no scalable outcome" (spec section 21).
    /// </summary>
    public string ProficiencyId { get; }

    /// <summary>
    /// The primary attribute that governs how fast this spell's proficiency accrues (spec
    /// section 9) -- null exactly when ProficiencyId is null, OR when AllSpellcasters is true
    /// (a shared spell resolves its governing attribute dynamically as the caster's own
    /// CharacterClass.ManaStat at cast time instead -- spec section 20 -- since a Mage and a
    /// Priest casting the same shared spell learn it off different stats).
    /// </summary>
    public PrimaryAttribute? GoverningAttribute { get; }

    public Spell(
        string name, string description, char displayCharacter,
        SpellCastingConfiguration casting, SpellTargetingConfiguration targeting,
        IReadOnlyList<SpellEffect> effects,
        int level = 1, IReadOnlyList<CharacterClass> allowedClasses = null, bool allSpellcasters = false,
        string flavorCastMessage = null,
        string proficiencyId = null, PrimaryAttribute? governingAttribute = null)
    {
        Name = name;
        Description = description;
        DisplayCharacter = displayCharacter;
        Casting = casting;
        Targeting = targeting;
        Effects = effects;
        Level = level;
        AllowedClasses = allowedClasses ?? Array.Empty<CharacterClass>();
        AllSpellcasters = allSpellcasters;
        FlavorCastMessage = flavorCastMessage;
        ProficiencyId = proficiencyId;
        GoverningAttribute = governingAttribute;
    }

    /// <summary>Class-eligibility only -- level, mana, cooldown, and targeting are validated separately (see SpellRequirementValidator, SpellCaster.Cast).</summary>
    public bool CanBeCastBy(CharacterClass casterClass) =>
        AllSpellcasters ? casterClass.IsSpellcaster : AllowedClasses.Contains(casterClass);

    /// <summary>
    /// Whether this spell should show a flying projectile animation when cast -- computed
    /// rather than a stored flag, so every existing damage spell (Fireball, Frost Bolt, Magic
    /// Missile, ...) qualifies automatically with zero catalog edits. A Self-targeted spell
    /// (a heal, a buff) never qualifies -- there's nowhere for a projectile to travel to. See
    /// SpellCaster.Cast's onTargetsResolved hook and GameLoop.HandleCastSpell/SpellCasterAI,
    /// which use this to decide whether to animate a flight before the spell's effects apply.
    /// </summary>
    public bool IsProjectile => Targeting.TargetType != SpellTargetType.Self && Effects.OfType<DamageEffect>().Any();
}
