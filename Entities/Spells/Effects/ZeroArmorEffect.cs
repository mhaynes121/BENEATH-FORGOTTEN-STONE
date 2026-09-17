namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

/// <summary>
/// Captures the target's CURRENT DefensePower at cast-time and registers it
/// as a normal, self-reverting ActiveEffect (Stat.Armor, amount = -current)
/// -- unlike a catalog-fixed StatModifierEffect amount, this always zeroes
/// out whatever Armor the target actually has right now, then EffectProcessor's
/// existing expiry/revert restores exactly that amount when it wears off.
/// Used by Recklessness (self, "reduces defense values to 0").
/// </summary>
public class ZeroArmorEffect : SpellEffect
{
    public int Duration { get; }

    public ZeroArmorEffect(int duration)
    {
        Duration = duration;
    }

    public override void Apply(SpellCastingContext context, SpellCastResult result)
    {
        foreach (var target in context.AffectedActors)
        {
            int currentArmor = target.DefensePower;
            if (currentArmor <= 0)
            {
                continue;
            }

            target.ActiveEffects.Add(new ActiveEffect(context.CastName, context.TurnNumber + Duration)
            {
                ModifiedStat = Stat.Armor,
                StatAmount = -currentArmor
            });
            StatModifierEffect.ApplyStatDelta(target, Stat.Armor, -currentArmor);
        }
    }
}
