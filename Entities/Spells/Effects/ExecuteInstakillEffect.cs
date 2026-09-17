using BENEATH_FORGOTTEN_STONE.Entities.Proficiency;

namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

/// <summary>
/// Execution Call: an apply-time conditional, not a pre-cast gate like
/// Skill.TargetHealthPercentBelow -- "attempt it, it either works or
/// fizzles" rather than "can only be used on...". Instantly kills the
/// target if their current health is below the caster's Agility * a
/// per-rank threshold multiplier (Ability Proficiency System's bespoke
/// Execution Call table; the old fixed value of 10 is exactly the
/// Proficient-rank number).
/// </summary>
public class ExecuteInstakillEffect : SpellEffect
{
    public override void Apply(SpellCastingContext context, SpellCastResult result)
    {
        foreach (var target in context.AffectedActors)
        {
            if (target.Health == null)
            {
                continue;
            }

            double thresholdMultiplier = ProficiencyScaling.ExecutionCallThresholdMultiplier(context.RankScaling.Rank);
            int threshold = (int)Math.Round(context.Caster.Agility * thresholdMultiplier);
            if (target.Health.Current >= threshold)
            {
                continue; // fizzles -- doesn't meet the threshold
            }

            int preDamageHealth = target.Health.Current;
            // Requesting exactly the target's own current HP (rather than SetCurrent(0) directly)
            // routes this through the same CombatStatsTracker bookkeeping every other damage
            // source uses -- TakeDamage's own clamp-at-0 behavior makes the two operations
            // identical for an already-alive target.
            CombatStatsTracker.ApplyDamage(target, preDamageHealth, context.Caster);
            target.LastDamageSource = $"{CombatMessages.WithArticle(context.Caster)} using {context.CastName}";
            target.LastDamageOwner = context.Caster;
            result.DamageDealt += preDamageHealth;
            result.DamageInstances.Add((target, HitSeverity.Mortal));
        }
    }
}
