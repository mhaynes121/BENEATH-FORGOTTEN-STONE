namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

/// <summary>
/// Shadowstep: teleports the caster to a free tile next to the target
/// (Level.FindFreeAdjacentTile), not specifically its "back" -- no facing
/// concept exists in this game, so any adjacent tile is the accepted
/// stand-in. Also grants Sneak, matching the skill's "immediately gaining a
/// Sneak status" text.
/// </summary>
public class TeleportAdjacentEffect : SpellEffect
{
    public override void Apply(SpellCastingContext context, SpellCastResult result)
    {
        var target = context.TargetActor;
        if (target == null)
        {
            return;
        }

        var tile = context.Level.FindFreeAdjacentTile(target.X, target.Y);
        if (tile == null)
        {
            return; // fizzles -- no free tile beside the target
        }

        context.Caster.MoveTo(tile.Value.X, tile.Value.Y);
        if (context.Caster is Player player)
        {
            player.IsSneaking = true;
        }
    }
}
