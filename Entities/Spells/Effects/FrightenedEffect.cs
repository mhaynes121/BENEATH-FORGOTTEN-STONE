namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

/// <summary>
/// New Priest Skill Progression (Turn Undead): frightens each affected actor via
/// CrowdControlResolver.TryApplyFrightened, gated by the same rank-scaled application-chance
/// roll StunEffect/KnockdownEffect already use. Turn Undead's own targeting already restricts
/// AffectedActors to visible CreatureType.Undead within radius (see SpellTargetType.SelfCenteredArea),
/// so this effect itself never checks CreatureType -- it would apply to anyone AffectedActors
/// happens to contain, same as StunEffect never checking who its own caller decided to target.
/// </summary>
public class FrightenedEffect : SpellEffect
{
    public int Duration { get; }

    /// <summary>Optional flavor text for a landed fright, "{target}" replaced with the target's label -- Turn Undead sets "The skeleton recoils in terror!"-style text.</summary>
    public string SuccessMessageTemplate { get; }

    /// <summary>Optional flavor text when the roll fails or the target resists -- Turn Undead sets "The wight defies your command."-style text.</summary>
    public string FailureMessageTemplate { get; }

    /// <summary>Turn Undead: "reduce the final application chance against an undead boss by 30 percentage points" -- applied only when the target's Monster.IsBoss is true, clamped to the system's normal [0, 1] probability range.</summary>
    public double BossResistancePenalty { get; }

    public FrightenedEffect(int duration, string successMessageTemplate = null, string failureMessageTemplate = null, double bossResistancePenalty = 0)
    {
        Duration = duration;
        SuccessMessageTemplate = successMessageTemplate;
        FailureMessageTemplate = failureMessageTemplate;
        BossResistancePenalty = bossResistancePenalty;
    }

    public override void Apply(SpellCastingContext context, SpellCastResult result)
    {
        foreach (var target in context.AffectedActors)
        {
            double chance = context.RankScaling.SecondaryEffectChance;
            if (BossResistancePenalty > 0 && target is Monster { IsBoss: true })
            {
                chance = Math.Clamp(chance - BossResistancePenalty, 0, 1);
            }

            if (context.Rng.NextDouble() >= chance)
            {
                if (FailureMessageTemplate != null)
                {
                    result.FlavorMessages.Add(FormatTemplate(FailureMessageTemplate, target));
                }
                continue;
            }

            var outcome = CrowdControlResolver.TryApplyFrightened(target, context.RankScaling.ScaleDuration(Duration), context.TurnNumber, context.Rng);
            if (outcome == CrowdControlOutcome.Applied && SuccessMessageTemplate != null)
            {
                result.FlavorMessages.Add(FormatTemplate(SuccessMessageTemplate, target));
            }
            else if (outcome == CrowdControlOutcome.ResistedBySteadfast)
            {
                result.FlavorMessages.Add("Your steadfast faith keeps your courage steady.");
            }
            else if (outcome != CrowdControlOutcome.Applied && FailureMessageTemplate != null)
            {
                result.FlavorMessages.Add(FormatTemplate(FailureMessageTemplate, target));
            }
        }
    }

    // capitalized: true -- unlike KnockdownEffect's identical helper, every current template
    // (Turn Undead's "{target} recoils in terror!"/"{target} defies your command.") opens with
    // {target}, so the substituted label needs to start the sentence.
    private static string FormatTemplate(string template, Actor target) =>
        template.Replace("{target}", CombatMessages.Label(target, capitalized: true));
}
