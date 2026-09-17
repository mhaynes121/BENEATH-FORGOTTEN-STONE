namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

/// <summary>
/// Sets Actor.SilencedUntilTurn -- the accepted stand-in for "interrupts
/// spellcasting" (Kick), since every cast in this engine resolves instantly
/// with no multi-turn cast bar to interrupt. Checked by SpellCasterAI before
/// it attempts to cast. Same pass/fail-not-magnitude reasoning as StunEffect.
/// </summary>
public class SilenceEffect : SpellEffect
{
    public int Duration { get; }

    public SilenceEffect(int duration)
    {
        Duration = duration;
    }

    public override void Apply(SpellCastingContext context, SpellCastResult result)
    {
        foreach (var target in context.AffectedActors)
        {
            if (context.TurnNumber < target.CcImmuneUntilTurn)
            {
                continue;
            }
            // Silence is a hostile secondary effect -- gated by the rank's application-chance
            // roll (Ability Proficiency System section 6) before it lands at all.
            if (context.Rng.NextDouble() >= context.RankScaling.SecondaryEffectChance)
            {
                continue;
            }
            target.SilencedUntilTurn = Math.Max(target.SilencedUntilTurn, context.TurnNumber + context.RankScaling.ScaleDuration(Duration));
        }
    }
}
