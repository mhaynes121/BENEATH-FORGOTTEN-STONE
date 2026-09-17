namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

/// <summary>
/// One generic stat-buff/debuff type instead of dedicated spell properties
/// like PhysicalAttackBonus/ArmorBonus/etc. Flat amounts only for now, not
/// percentages -- no required spell needs a percentage modifier, and
/// "percentage of what" (current vs. base) isn't a decision worth making
/// speculatively.
/// </summary>
public class StatModifierEffect : SpellEffect
{
    public Stat Stat { get; }
    public int Amount { get; }
    public int Duration { get; }

    /// <summary>
    /// Ability Proficiency System: true for a hostile debuff applied to an enemy (Crippling
    /// Strike, Blind, Sap Strength) -- gated by a per-rank application-chance roll before it
    /// applies at all, per spec section 6 ("secondary hostile effects"). False (the default) for
    /// every beneficial self-buff (Shield Block, Recklessness, Bless, Haste, ...), which always
    /// applies once the parent skill/spell itself resolves -- a buff is never chance-gated.
    /// </summary>
    public bool IsHostileSecondaryEffect { get; }

    public StatModifierEffect(Stat stat, int amount, int duration, bool isHostileSecondaryEffect = false)
    {
        Stat = stat;
        Amount = amount;
        Duration = duration;
        IsHostileSecondaryEffect = isHostileSecondaryEffect;
    }

    public override void Apply(SpellCastingContext context, SpellCastResult result)
    {
        foreach (var target in context.AffectedActors)
        {
            if (IsHostileSecondaryEffect && context.Rng.NextDouble() >= context.RankScaling.SecondaryEffectChance)
            {
                continue; // the parent attack/spell still resolved -- only this secondary effect failed to land
            }

            int amount = context.RankScaling.ScaleMagnitude(Amount);
            int duration = context.RankScaling.ScaleDuration(Duration);

            // The casting spell/skill's own name (e.g. "Bless"), not the raw Stat enum name --
            // that's what EffectProcessor's expiry message and the character sheet's
            // active-effects list actually want to show. Falls back to the stat name only if
            // somehow applied outside SpellCaster.Cast/SkillCaster.Cast, the only places that
            // set context.CastName.
            target.ActiveEffects.Add(new ActiveEffect(context.CastName ?? Stat.ToString(), context.TurnNumber + duration)
            {
                ModifiedStat = Stat,
                StatAmount = amount,
                // Reuses IsHostileSecondaryEffect directly as Purify's own eligibility flag -- a
                // hostile debuff (Crippling Strike/Blind/Sap Strength) is purifiable; a self-buff
                // (Shield Block, Bless, Haste, ...) never is.
                CanBePurified = IsHostileSecondaryEffect
            });
            ApplyStatDelta(target, Stat, amount);
        }
    }

    /// <summary>Public so EffectProcessor can apply the inverse delta when the ActiveEffect expires.</summary>
    public static void ApplyStatDelta(Actor actor, Stat stat, int amount)
    {
        switch (stat)
        {
            case Stat.PhysicalAttack:
                actor.BasePhysicalAttackPower += amount;
                break;
            case Stat.MagicalAttack:
                actor.BaseMagicalAttackPower += amount;
                break;
            case Stat.Armor:
                actor.DefensePower += amount;
                break;
            case Stat.MagicResistance:
                actor.MagicResistance += amount;
                break;
            case Stat.MovementSpeed:
                actor.Speed += amount;
                break;
            case Stat.CarryCapacity:
                actor.CarryCapacityBonus += amount;
                break;
            case Stat.Dodge:
                actor.DodgeModifier += amount;
                break;
            case Stat.Accuracy:
                actor.AccuracyModifier += amount;
                break;
        }
    }
}
