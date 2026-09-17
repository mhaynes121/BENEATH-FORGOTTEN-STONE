namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

/// <summary>Not used by any catalog spell yet, but implemented and ready -- see Spells.cs.</summary>
public class ManaEffect : SpellEffect
{
    /// <summary>Positive restores mana; negative drains it.</summary>
    public int Amount { get; }
    public HealTarget Target { get; }

    public ManaEffect(int amount, HealTarget target = HealTarget.AffectedActors)
    {
        Amount = amount;
        Target = target;
    }

    public override void Apply(SpellCastingContext context, SpellCastResult result)
    {
        var recipients = Target == HealTarget.Caster
            ? new List<Actor> { context.Caster }
            : context.AffectedActors;

        int amount = context.RankScaling.ScaleMagnitude(Amount);
        foreach (var recipient in recipients)
        {
            if (amount >= 0)
            {
                recipient.Mana.Restore(amount);
            }
            else
            {
                recipient.Mana.TrySpend(Math.Min(-amount, recipient.Mana.Current));
            }
        }
    }
}
