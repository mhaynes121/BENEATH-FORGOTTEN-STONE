namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

/// <summary>Moves the caster to the resolved target tile. Ignores AffectedActors -- Teleport acts on the caster, not on whoever's standing at the destination.</summary>
public class TeleportEffect : SpellEffect
{
    public override void Apply(SpellCastingContext context, SpellCastResult result)
    {
        if (context.TargetTile is { } tile && !context.Level.IsBlockedForActorMovement(tile.X, tile.Y))
        {
            context.Caster.MoveTo(tile.X, tile.Y);
        }
    }
}
