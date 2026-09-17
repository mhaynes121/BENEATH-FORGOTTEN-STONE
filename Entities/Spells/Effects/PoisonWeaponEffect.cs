namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

/// <summary>Poison Weapon: a self-targeted window during which GameLoop.HandleMove rolls a chance, on each successful player melee hit, to also apply a poison DoT to the target.</summary>
public class PoisonWeaponEffect : SpellEffect
{
    public int Duration { get; }

    public PoisonWeaponEffect(int duration)
    {
        Duration = duration;
    }

    public override void Apply(SpellCastingContext context, SpellCastResult result)
    {
        if (context.Caster is Player player)
        {
            player.PoisonWeaponUntilTurn = context.TurnNumber + context.RankScaling.ScaleDuration(Duration);
        }
    }
}
