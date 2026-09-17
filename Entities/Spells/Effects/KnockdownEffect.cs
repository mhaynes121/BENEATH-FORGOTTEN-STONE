namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

/// <summary>
/// Prone/Knockdown System: knocks each affected actor down via KnockdownResolver.TryApply, gated
/// by the Ability Proficiency System's hostile-secondary-effect application chance (the same
/// 75/90/100/100/100% table StunEffect/SilenceEffect already roll against) -- this doubles as
/// Bash's "secondary effect on top of a landed hit" chance AND as Trip/Tremor's own success roll,
/// since for those two abilities the knockdown IS the entire effect, not a bonus layered onto a
/// separate attack.
/// </summary>
public class KnockdownEffect : SpellEffect
{
    /// <summary>Bash only: the parent WeaponDamageEffect must have actually connected (result.AttackHit == true) before the knockdown even rolls. False for Trip/Tremor, which have no attack roll of their own to gate on -- see SkillCaster/SpellCaster's shared effect pipeline and SpellCastResult.AttackHit's own doc comment.</summary>
    public bool RequireAttackHit { get; }

    /// <summary>Optional flavor text for a landed knockdown, "{target}" replaced with the target's label (see CombatMessages.Label) -- null (Bash, Trip) keeps today's silent-application convention that StunEffect already established; Tremor sets one for its earthquake flavor.</summary>
    public string SuccessMessageTemplate { get; }

    /// <summary>Optional flavor text when the roll fails or the target resists -- same "{target}" substitution, same null-means-silent default.</summary>
    public string FailureMessageTemplate { get; }

    public KnockdownEffect(bool requireAttackHit = false, string successMessageTemplate = null, string failureMessageTemplate = null)
    {
        RequireAttackHit = requireAttackHit;
        SuccessMessageTemplate = successMessageTemplate;
        FailureMessageTemplate = failureMessageTemplate;
    }

    public override void Apply(SpellCastingContext context, SpellCastResult result)
    {
        if (RequireAttackHit && result.AttackHit != true)
        {
            return;
        }

        foreach (var target in context.AffectedActors)
        {
            if (context.Rng.NextDouble() >= context.RankScaling.SecondaryEffectChance)
            {
                if (FailureMessageTemplate != null)
                {
                    result.FlavorMessages.Add(FormatTemplate(FailureMessageTemplate, target));
                }
                continue;
            }

            var outcome = KnockdownResolver.TryApply(target, context.TurnNumber, context.Level.Scheduler, context.Rng);
            if (outcome == KnockdownOutcome.Applied && SuccessMessageTemplate != null)
            {
                result.FlavorMessages.Add(FormatTemplate(SuccessMessageTemplate, target));
            }
            else if (outcome == KnockdownOutcome.ResistedBySteadfast)
            {
                result.FlavorMessages.Add("Your steadfast faith keeps you on your feet.");
            }
            else if (outcome != KnockdownOutcome.Applied && FailureMessageTemplate != null)
            {
                result.FlavorMessages.Add(FormatTemplate(FailureMessageTemplate, target));
            }
        }
    }

    private static string FormatTemplate(string template, Actor target) =>
        template.Replace("{target}", CombatMessages.Label(target, capitalized: false));
}
