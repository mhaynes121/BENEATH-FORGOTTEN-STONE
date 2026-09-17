using BENEATH_FORGOTTEN_STONE.Entities;

namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

/// <summary>
/// A live status/buff/debuff currently applied to an actor -- as opposed
/// to SpellEffect, which is the reusable, stateless definition carried by
/// a Spell. EffectProcessor ticks and expires these once per turn.
/// </summary>
public class ActiveEffect
{
    public string SourceSpellName { get; }
    public int ExpiresOnTurn { get; }

    public Stat? ModifiedStat { get; init; }
    public int StatAmount { get; init; }

    /// <summary>Temporary resistance buff/debuff (design spec sections 31/32) -- e.g. a "Resist Fire" spell or a "Curse of Flame" debuff. Unlike ModifiedStat/StatAmount above, this needs no apply-now/revert-on-expiry bookkeeping: ResistanceCalculator sums every currently-active effect's contribution live on each call, so it simply stops counting the moment EffectProcessor.Tick removes the expired effect from ActiveEffects.</summary>
    public ResistanceType? ModifiedResistanceType { get; init; }
    public int ResistanceAmount { get; init; }

    public int TickDamage { get; init; }
    public DamageType TickDamageType { get; init; }

    /// <summary>Human-readable "who applied this" for cause-of-death tracking on tick damage -- e.g. "a goblin shaman's Burning". Only meaningful when TickDamage > 0; see EffectProcessor.Tick and Actor.LastDamageSource.</summary>
    public string DamageSourceDescription { get; init; }

    /// <summary>
    /// Who applied this effect -- propagated to Actor.LastDamageOwner on each TickDamage tick so
    /// a delayed kill (poison, burning) still credits whoever cast/procced it. Only meaningful
    /// when TickDamage > 0; null for pure stat-modifier buffs/debuffs, which never kill anything.
    /// Deliberately NOT persisted by SaveManager/ActiveEffectData -- like DungeonSoundState, an
    /// Actor reference doesn't round-trip through JSON without a full ID-based resolution layer,
    /// and the worst case of skipping it is a save/load mid-DoT losing kill credit for that one
    /// remaining tick, not a lasting bug.
    /// </summary>
    public Actor Owner { get; init; }

    /// <summary>Non-null makes this effect ALSO an active light source, centered on its owning actor's current position -- see Core/LightingSystem.CollectActiveLightSources. Set by LightEffect for Arcane Orb/Divine Radiance; null for every other effect. Fully persisted (a plain int, unlike Owner above) -- see Persistence/ActiveEffectData.</summary>
    public int? LightRadius { get; init; }

    /// <summary>
    /// New Priest Skill Progression (Purify): true for a condition Purify is allowed to strip
    /// early -- every hostile DoT/status (see StatusEffect, always hostile in current usage) and
    /// every hostile secondary stat debuff (Crippling Strike/Blind/Sap Strength -- see
    /// StatModifierEffect, which reuses its own IsHostileSecondaryEffect flag as this value
    /// directly). False (the default) for every self-applied buff -- Purify only ever cleanses
    /// affliction, never a beneficial effect the player cast on themselves.
    /// </summary>
    public bool CanBePurified { get; init; }

    public ActiveEffect(string sourceSpellName, int expiresOnTurn)
    {
        SourceSpellName = sourceSpellName;
        ExpiresOnTurn = expiresOnTurn;
    }
}
