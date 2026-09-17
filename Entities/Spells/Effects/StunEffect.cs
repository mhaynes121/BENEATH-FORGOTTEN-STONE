namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

/// <summary>
/// Sets Actor.StunnedUntilTurn rather than registering an ActiveEffect --
/// stun is a pass/fail state, not a magnitude, so there's no numeric delta
/// for EffectProcessor to revert later. GameLoop's turn loop checks this
/// field directly to skip the stunned actor's next turn. Resisted entirely
/// while the target is under Berserker Rage's CC immunity.
/// </summary>
public class StunEffect : SpellEffect
{
    public int Duration { get; }

    public StunEffect(int duration)
    {
        Duration = duration;
    }

    public override void Apply(SpellCastingContext context, SpellCastResult result)
    {
        foreach (var target in context.AffectedActors)
        {
            // Stun is a hostile secondary effect -- gated by the rank's application-chance
            // roll (Ability Proficiency System section 6) before it lands at all. Checked
            // FIRST, per the New Priest Skill Progression proposal's own canonical CC order
            // (application roll, then crowd-control immunity, then Steadfast) -- see
            // CrowdControlResolver.TryApplyStun for the remaining two steps.
            if (context.Rng.NextDouble() >= context.RankScaling.SecondaryEffectChance)
            {
                continue;
            }

            var outcome = CrowdControlResolver.TryApplyStun(target, context.RankScaling.ScaleDuration(Duration), context.TurnNumber, context.Rng);
            if (outcome == CrowdControlOutcome.ResistedBySteadfast)
            {
                result.FlavorMessages.Add("Your steadfast faith keeps your mind clear.");
            }
        }
    }
}
