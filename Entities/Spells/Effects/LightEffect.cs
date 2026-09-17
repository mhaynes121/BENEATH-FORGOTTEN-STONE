namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

/// <summary>
/// Grants the caster magical/divine illumination for a fixed duration -- Arcane Orb, Divine
/// Radiance. Always affects the caster directly (both are Self-targeted spells) rather than
/// iterating context.AffectedActors, the same directness HealEffect's HealTarget.Caster branch
/// already uses. Unlike a physical light source, this never needs lighting, never randomly
/// extinguishes, and water never puts it out (design spec sections 16/17) -- it's just a plain
/// ActiveEffect with LightRadius set, so EffectProcessor.Tick's normal expiry handling (including
/// its atmospheric expiration message) already covers it with zero extra plumbing.
/// </summary>
public class LightEffect : SpellEffect
{
    public int LightRadius { get; }
    public int Duration { get; }

    public LightEffect(int lightRadius, int duration)
    {
        LightRadius = lightRadius;
        Duration = duration;
    }

    public override void Apply(SpellCastingContext context, SpellCastResult result)
    {
        context.Caster.ActiveEffects.Add(new ActiveEffect(context.CastName, context.TurnNumber + context.RankScaling.ScaleDuration(Duration))
        {
            LightRadius = context.RankScaling.ScaleDuration(LightRadius)
        });
    }
}
