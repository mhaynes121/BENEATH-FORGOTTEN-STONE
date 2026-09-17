namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

/// <summary>
/// Applies a stat delta directly via StatModifierEffect.ApplyStatDelta with
/// no ActiveEffect registered at all -- unlike StatModifierEffect, this
/// never expires or reverts. Used for Shatter Guard's permanent armor
/// reduction: the character sheet's "Active Effects" list correctly shows
/// no countdown for it, since it isn't a timed effect.
/// </summary>
public class PermanentStatModifierEffect : SpellEffect
{
    public Stat Stat { get; }
    public int Amount { get; }

    public PermanentStatModifierEffect(Stat stat, int amount)
    {
        Stat = stat;
        Amount = amount;
    }

    public override void Apply(SpellCastingContext context, SpellCastResult result)
    {
        foreach (var target in context.AffectedActors)
        {
            // A permanent stat drawback (Shatter Guard) is chance-gated by rank like any other
            // hostile secondary effect, but its magnitude never scales -- "skill drawbacks do
            // not scale" per the Ability Proficiency System spec.
            if (context.Rng.NextDouble() >= context.RankScaling.SecondaryEffectChance)
            {
                continue;
            }
            StatModifierEffect.ApplyStatDelta(target, Stat, Amount);
        }
    }
}
