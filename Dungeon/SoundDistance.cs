namespace BENEATH_FORGOTTEN_STONE.Dungeon;

/// <summary>
/// Bounded flood-distance (plain BFS over walkable tiles, walls impassable) rather than a full
/// pathfinder or straight-line distance -- the design spec explicitly wants path distance
/// ("walls should naturally reduce sound propagation because path-distance around them is
/// longer") but explicitly warns against a real acoustic simulation. Every call is capped at
/// the caller's own maxRange, and a cheap Chebyshev prefilter skips the BFS entirely for
/// anything already too far in a straight line (design spec section 72: filter cheaply before
/// doing any real distance work) -- dungeon floors are small, but sound checks are frequent
/// enough (once per eligible player turn) that this still matters.
/// </summary>
public static class SoundDistance
{
    private static readonly (int Dx, int Dy)[] Directions =
    {
        (0, -1), (0, 1), (-1, 0), (1, 0), (-1, -1), (-1, 1), (1, -1), (1, 1)
    };

    /// <returns>True with the actual path distance if `to` is reachable from `from` within maxRange steps; false (distance left at 0) otherwise.</returns>
    public static bool TryGetDistance(Level level, (int X, int Y) from, (int X, int Y) to, int maxRange, out int distance)
    {
        distance = 0;
        if (Math.Max(Math.Abs(from.X - to.X), Math.Abs(from.Y - to.Y)) > maxRange)
        {
            return false; // path distance can only be >= this straight-line distance
        }
        if (from == to)
        {
            return true;
        }

        var visited = new HashSet<(int X, int Y)> { from };
        var frontier = new Queue<((int X, int Y) Position, int Distance)>();
        frontier.Enqueue((from, 0));

        while (frontier.Count > 0)
        {
            var (position, stepsSoFar) = frontier.Dequeue();
            if (stepsSoFar >= maxRange)
            {
                continue; // never expand past the range we actually care about
            }

            foreach (var (dx, dy) in Directions)
            {
                var next = (position.X + dx, position.Y + dy);
                if (visited.Contains(next) || !level.IsInBounds(next.Item1, next.Item2) || !level.IsWalkable(next.Item1, next.Item2))
                {
                    continue;
                }
                if (next == to)
                {
                    distance = stepsSoFar + 1;
                    return true;
                }
                visited.Add(next);
                frontier.Enqueue((next, stepsSoFar + 1));
            }
        }

        return false;
    }
}
