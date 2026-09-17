using BENEATH_FORGOTTEN_STONE.Dungeon;

namespace BENEATH_FORGOTTEN_STONE.Entities.AI;

/// <summary>
/// ChaseAI plus terrain awareness for a tile-attuned monster (Monster.IsFloorAttuned) -- reuses
/// ChaseAI's healing-item check, detection radius, and adjacent-melee-attack (including
/// Riposte) entirely unchanged via inheritance, and only substitutes two things: the movement
/// step (MoveTowardTarget, overridden to route through TerrainPathfinder instead of a plain
/// greedy step -- design spec sections 11/12/41), and what happens when the player isn't
/// detected at all, where a normal ChaseAI would just idle (TakeTurn, overridden to path back
/// toward the nearest reachable tile of the monster's PreferredFloorType instead -- section 13).
/// Every ordinary monster keeps using plain ChaseAI, untouched.
/// </summary>
public class FloorAttunedAI : ChaseAI
{
    public override string TakeTurn(Actor self, Level level, Random rng)
    {
        var player = level.Actors.OfType<Player>().FirstOrDefault(p => p.IsAlive);
        if (player != null)
        {
            int dx = player.X - self.X;
            int dy = player.Y - self.Y;
            int detectionRadius = DetectionRules.EffectiveRadius(player);
            if ((dx * dx) + (dy * dy) <= detectionRadius * detectionRadius)
            {
                // Delegates to ChaseAI's own healing-item/detect/attack/move logic -- its move
                // step still polymorphically calls THIS class's overridden MoveTowardTarget
                // below, so chasing the player already routes preferentially through preferred
                // terrain (section 12) without duplicating any combat logic here.
                return base.TakeTurn(self, level, rng);
            }
        }

        // Player not detected (or dead/absent) -- a plain ChaseAI would simply idle here.
        // Section 13: strongly prefer returning to the nearest reachable preferred tile instead.
        return TryReturnToPreferredFloor(self, level);
    }

    protected override void MoveTowardTarget(Actor self, Level level, int targetX, int targetY)
    {
        if (self is not Monster { IsFloorAttuned: true } monster)
        {
            base.MoveTowardTarget(self, level, targetX, targetY);
            return;
        }

        var step = TerrainPathfinder.FindNextStep(level, (self.X, self.Y), (targetX, targetY), monster.PreferredFloorType.Value, mover: self);
        if (step.HasValue && ChaseAI.TryMove(self, level, step.Value.X, step.Value.Y))
        {
            return;
        }

        // No cost-weighted path found (or the chosen tile is currently occupied/blocked) --
        // fall back to the same plain greedy step every other monster uses, rather than
        // freezing in place for the turn.
        base.MoveTowardTarget(self, level, targetX, targetY);
    }

    private static string TryReturnToPreferredFloor(Actor self, Level level)
    {
        if (self is not Monster { IsFloorAttuned: true } monster)
        {
            return null;
        }

        if (level.Tiles[self.X, self.Y].FloorType == monster.PreferredFloorType)
        {
            return null; // already home -- nothing to do
        }

        var nearestPreferredTile = TerrainPathfinder.FindNearestTileOfType(level, (self.X, self.Y), monster.PreferredFloorType.Value, mover: self);
        if (nearestPreferredTile == null)
        {
            return null; // no reachable (hazard-free) preferred tile on this level at all
        }

        var step = TerrainPathfinder.FindNextStep(level, (self.X, self.Y), nearestPreferredTile.Value, monster.PreferredFloorType.Value, mover: self);
        if (step.HasValue)
        {
            ChaseAI.TryMove(self, level, step.Value.X, step.Value.Y);
        }
        return null;
    }
}
