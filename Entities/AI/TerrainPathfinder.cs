using BENEATH_FORGOTTEN_STONE.Core;
using BENEATH_FORGOTTEN_STONE.Dungeon;

namespace BENEATH_FORGOTTEN_STONE.Entities.AI;

/// <summary>
/// Cost-weighted single-source Dijkstra over the level grid -- FloorAttunedAI uses this every
/// turn for its own terrain-preference routing; ordinary ChaseAI only falls back to it (with no
/// floor preference, i.e. uniform cost) when its cheap greedy step is blocked from every
/// direction, so a monster routes around an obstacle (a boulder, shrine, statue, or stationary
/// trader) directly between it and its target instead of stalling in place turn after turn as if
/// it had lost the target. Grid sizes here are small (a dungeon floor is well under 2000 tiles),
/// so a full recompute is cheap enough to run per monster turn with no caching needed.
///
/// Walls, closed doors, movement-blocking RoomObjects, and a Trader's own tile are the only truly
/// impassable nodes -- a non-preferred FloorType is just expensive (see FloorAttunementConfig.
/// NonPreferredFloorPathCost), never blocked, so a floor-attuned monster can still cross it when
/// the AI decides that's necessary (design spec section 41). Every other actor's current tile is
/// deliberately NOT treated as blocked in the cost field itself -- it's a transient, per-turn
/// fact that would otherwise distort the shape of an otherwise-open path; the caller (ChaseAI/
/// FloorAttunedAI) still separately checks the chosen next step against GetActorAt before
/// actually moving there. A Trader is the one exception, baked in as a permanent obstacle here
/// like a boulder: TraderNeverActsInTheScheduler means its position never changes, so treating it
/// as static is always correct, and doing so here (rather than only discovering the block at the
/// final step) is what actually lets a chasing monster route around a trader planted in its path.
/// </summary>
public static class TerrainPathfinder
{
    private static readonly (int Dx, int Dy)[] Directions =
    {
        (0, -1), (0, 1), (-1, 0), (1, 0),
        (-1, -1), (-1, 1), (1, -1), (1, 1)
    };

    /// <summary>Null preferredFloorType (ordinary ChaseAI's obstacle-routing fallback) means every step costs the same -- the classic unweighted shortest path, since Dijkstra with uniform edge weights degenerates to plain BFS distance.</summary>
    private static int StepCost(Level level, int x, int y, FloorType? preferredFloorType) =>
        preferredFloorType.HasValue
            ? (level.Tiles[x, y].FloorType == preferredFloorType.Value ? FloorAttunementConfig.PreferredFloorPathCost : FloorAttunementConfig.NonPreferredFloorPathCost)
            : 1;

    // Deliberately NOT level.IsBlockedForActorMovement -- that also treats another actor's
    // current tile as blocked, which this cost field's own doc comment explains would distort
    // the shape of an otherwise-open path (ordinary actor occupancy is transient and checked
    // separately by the caller before actually stepping). Walls, closed doors, movement-blocking
    // RoomObjects, and a Trader's own tile (permanent, unlike any other actor -- see the class
    // doc comment) are the only truly impassable nodes here. Safe Monster Spawning and
    // Hazard-Aware Movement: when `mover` is given, a tile that would damage it (Fire/Lava, or
    // off-preferred-terrain attrition -- see ActorTerrainSafety) is ALSO treated as fully
    // impassable here rather than merely expensive, so an all-hazardous route is correctly
    // reported as "no path" (FindNextStep returns null) instead of ever being selected as the
    // "cheapest available" option. `mover` is null only for the handful of pre-existing test call
    // sites with no actor context; every real caller passes one.
    private static bool IsTraversable(Level level, int x, int y, Actor mover) =>
        level.IsWalkable(x, y) && level.GetDoorAt(x, y) == null && level.GetRoomObjectAt(x, y) is not { BlocksMovement: true }
        && level.GetActorAt(x, y) is not Trader
        && (mover == null || ActorTerrainSafety.IsSafeForActor(level, mover, x, y));

    /// <summary>
    /// Every reachable tile's total cost to walk there from `source`, weighted so a step onto
    /// `preferredFloorType` is cheap and every other step is expensive -- the classic "compute a
    /// cost field rooted at the goal, then have each mover descend its own local gradient"
    /// shape, which lets one Dijkstra run answer "what's my best next step" from any position
    /// without re-running per mover.
    /// </summary>
    private static Dictionary<(int X, int Y), int> ComputeCostField(Level level, (int X, int Y) source, FloorType? preferredFloorType, Actor mover)
    {
        var costs = new Dictionary<(int X, int Y), int> { [source] = 0 };
        var frontier = new PriorityQueue<(int X, int Y), int>();
        frontier.Enqueue(source, 0);

        while (frontier.TryDequeue(out var current, out int currentCost))
        {
            if (currentCost > costs[current])
            {
                continue; // a cheaper route to `current` was already processed
            }

            foreach (var (dx, dy) in Directions)
            {
                int nx = current.X + dx;
                int ny = current.Y + dy;
                if (!level.IsInBounds(nx, ny) || !IsTraversable(level, nx, ny, mover))
                {
                    continue;
                }

                int candidateCost = currentCost + StepCost(level, nx, ny, preferredFloorType);
                var neighbor = (nx, ny);
                if (!costs.TryGetValue(neighbor, out int existingCost) || candidateCost < existingCost)
                {
                    costs[neighbor] = candidateCost;
                    frontier.Enqueue(neighbor, candidateCost);
                }
            }
        }

        return costs;
    }

    /// <summary>
    /// The next tile to step onto from `from` to make progress toward `to`, preferring a route
    /// through `preferredFloorType` over a shorter one that isn't -- null if `to` is unreachable
    /// from `from` at all (including "every route is hazardous for `mover`" -- see IsTraversable),
    /// or `from` already equals `to`. `preferredFloorType` defaults to null (plain unweighted
    /// shortest path) for ChaseAI's own obstacle-routing fallback, which has no terrain preference
    /// at all -- only FloorAttunedAI ever passes a real value. `mover` defaults to null (no
    /// hazard-awareness -- purely physical passability, today's original behavior) for the
    /// handful of pre-existing call sites with no actor context; every real AI caller should pass
    /// the actor that's actually about to move.
    /// </summary>
    public static (int X, int Y)? FindNextStep(Level level, (int X, int Y) from, (int X, int Y) to, FloorType? preferredFloorType = null, Actor mover = null)
    {
        if (from == to)
        {
            return null;
        }

        // Rooted at the destination so the field's cost at `from` (and at every neighbor)
        // already reflects "total cost of the rest of the trip from here" -- the neighbor with
        // the lowest cost is the correct next step toward `to`.
        var costs = ComputeCostField(level, to, preferredFloorType, mover);
        if (!costs.ContainsKey(from))
        {
            return null;
        }

        (int X, int Y)? best = null;
        int bestCost = int.MaxValue;
        foreach (var (dx, dy) in Directions)
        {
            var candidate = (from.X + dx, from.Y + dy);
            if (costs.TryGetValue(candidate, out int cost) && cost < bestCost)
            {
                bestCost = cost;
                best = candidate;
            }
        }
        return best;
    }

    /// <summary>
    /// The geometrically nearest reachable tile of `floorType` from `from` (plain unweighted BFS
    /// distance, not terrain-cost-weighted -- the monster isn't on preferred terrain yet, so
    /// there's nothing to prefer along the way here), or null if none is reachable at all on this
    /// level. `mover` defaults to null for the same reason FindNextStep's does; FloorAttunedAI's
    /// own "return to my preferred floor" search passes itself, so a route that would require
    /// crossing e.g. Lava to reach a Fire patch is correctly excluded rather than walked anyway.
    /// </summary>
    public static (int X, int Y)? FindNearestTileOfType(Level level, (int X, int Y) from, FloorType floorType, Actor mover = null)
    {
        var visited = new HashSet<(int X, int Y)> { from };
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue(from);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current != from && level.Tiles[current.X, current.Y].FloorType == floorType)
            {
                return current;
            }

            foreach (var (dx, dy) in Directions)
            {
                var next = (current.X + dx, current.Y + dy);
                if (visited.Contains(next) || !level.IsInBounds(next.Item1, next.Item2) || !IsTraversable(level, next.Item1, next.Item2, mover))
                {
                    continue;
                }
                visited.Add(next);
                queue.Enqueue(next);
            }
        }

        return null;
    }
}
