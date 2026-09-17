namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

public enum HealTarget
{
    /// <summary>Heals whoever the spell's targeting resolved as affected (e.g. Self-target Heal).</summary>
    AffectedActors,

    /// <summary>Always heals the caster regardless of who was targeted -- e.g. Drain Life healing off damage dealt to an enemy.</summary>
    Caster
}

public class HealEffect : SpellEffect
{
    public int FixedAmount { get; }
    public double PercentOfDamageDealt { get; }
    public HealTarget Target { get; }

    /// <summary>Lay on Hands: "Base healing = 10 + adjusted Wisdom" -- FixedAmount supplies the flat 10, this supplies the attribute term, read off the caster (always a Player -- this only ever makes sense for a player-cast heal). Null (every other heal) leaves FixedAmount as the entire base.</summary>
    public PrimaryAttribute? ScalesWithAttribute { get; }

    public HealEffect(int fixedAmount = 0, double percentOfDamageDealt = 0, HealTarget target = HealTarget.AffectedActors, PrimaryAttribute? scalesWithAttribute = null)
    {
        FixedAmount = fixedAmount;
        PercentOfDamageDealt = percentOfDamageDealt;
        Target = target;
        ScalesWithAttribute = scalesWithAttribute;
    }

    public override void Apply(SpellCastingContext context, SpellCastResult result)
    {
        int baseAmount = FixedAmount;
        if (ScalesWithAttribute.HasValue && context.Caster is Player player)
        {
            baseAmount += player.Stats.Adjusted(ScalesWithAttribute.Value);
        }

        // FixedAmount (+ the attribute term above) scales directly by rank; a percentOfDamageDealt
        // heal (Bloodthirst/Drain Life) needs no separate scaling here -- result.DamageDealt is
        // already the rank-scaled output of whichever WeaponDamageEffect/DamageEffect ran earlier
        // in the same ability's Effects list, so the heal inherits that scaling for free.
        int amount = context.RankScaling.ScaleMagnitude(baseAmount) + (int)(result.DamageDealt * PercentOfDamageDealt);
        if (amount <= 0)
        {
            return;
        }

        var recipients = Target == HealTarget.Caster
            ? new List<Actor> { context.Caster }
            : context.AffectedActors;

        foreach (var recipient in recipients)
        {
            int preHealCurrent = recipient.Health?.Current ?? 0;
            int missing = (recipient.Health?.Max ?? 0) - preHealCurrent;

            recipient.Health?.Heal(amount);
            result.HealingDone += amount;

            if (missing > 0)
            {
                result.HealInstances.Add((recipient, CombatMessages.ClassifyHealSeverity(Math.Min(amount, missing), missing)));
            }
        }
    }
}
