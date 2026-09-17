namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

/// <summary>Berserker Rage: clears any current stun/silence and grants immunity to new ones for a duration -- StunEffect/SilenceEffect both check CcImmuneUntilTurn before applying.</summary>
public class CleanseAndImmunityEffect : SpellEffect
{
    public int ImmunityDuration { get; }

    public CleanseAndImmunityEffect(int immunityDuration)
    {
        ImmunityDuration = immunityDuration;
    }

    public override void Apply(SpellCastingContext context, SpellCastResult result)
    {
        foreach (var target in context.AffectedActors)
        {
            target.StunnedUntilTurn = 0;
            target.SilencedUntilTurn = 0;
            target.CcImmuneUntilTurn = Math.Max(target.CcImmuneUntilTurn, context.TurnNumber + context.RankScaling.ScaleDuration(ImmunityDuration));
        }
    }
}
