namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

/// <summary>
/// Covers both flavor statuses and damage-over-time in one type -- Burning
/// and Poisoned are both "deal TickDamage once per turn for Duration turns,"
/// so this subsumes what the original notes called DamageOverTimeEffect
/// rather than keeping two parallel systems. TickDamage of 0 means a pure
/// status with no direct damage.
/// </summary>
public class StatusEffect : SpellEffect
{
    public string StatusName { get; }
    public int Duration { get; }
    public int TickDamage { get; }
    public DamageType TickDamageType { get; }

    public StatusEffect(string statusName, int duration, int tickDamage = 0, DamageType tickDamageType = DamageType.Physical)
    {
        StatusName = statusName;
        Duration = duration;
        TickDamage = tickDamage;
        TickDamageType = tickDamageType;
    }

    public override void Apply(SpellCastingContext context, SpellCastResult result)
    {
        foreach (var target in context.AffectedActors)
        {
            // A hostile DoT/status is always a "secondary effect" in current usage -- gated by
            // the rank's application-chance roll before it lands at all, per Ability Proficiency
            // System section 6.
            if (context.Rng.NextDouble() >= context.RankScaling.SecondaryEffectChance)
            {
                continue;
            }

            // Resistance shortens an elemental status's duration (Resistance System spec
            // section 28) -- a no-op for TickDamageType.Physical (the default for a pure,
            // non-elemental status) since ResistanceTypeMapping has no entry for Physical.
            int duration = ResistanceCalculator.AdjustStatusDuration(context.Level, target, TickDamageType, Duration);
            duration = context.RankScaling.ScaleDuration(duration);
            int tickDamage = context.RankScaling.ScaleMagnitude(TickDamage);
            target.ActiveEffects.Add(new ActiveEffect(StatusName, context.TurnNumber + duration)
            {
                TickDamage = tickDamage,
                TickDamageType = TickDamageType,
                // Lowercased + "magic" rather than the bare adjective on its own ("...'s Burning") --
                // a spell has no AttackType verb to pair it with the way ItemEffectApplier's melee/
                // projectile version does, so "magic" fills that role instead.
                DamageSourceDescription = TickDamage > 0 ? $"{CombatMessages.WithArticle(context.Caster)}'s {StatusName.ToLowerInvariant()} magic" : null,
                Owner = TickDamage > 0 ? context.Caster : null,
                // A StatusEffect is always hostile in current usage (Burning/Poisoned/Shocked) --
                // see this class's own doc comment -- so Purify may always strip it early.
                CanBePurified = true
            });
        }
    }
}
