namespace BENEATH_FORGOTTEN_STONE.Dungeon;

/// <summary>
/// Recursive shadowcasting FOV (the classic 8-octant algorithm, e.g. as
/// described on RogueBasin). This is the "real" algorithm called out in
/// the design notes as risk #1 -- naive raycasting from the player to
/// every tile produces visible gaps/artifacts along walls, which this
/// avoids by sweeping each octant as a set of slopes instead of rays.
///
/// A per-tile raycasting rewrite was tried and reverted (see
/// feedback-shadowcasting-keyhole-artifact in memory) -- it changed how sight lines render
/// broadly enough that the user preferred shadowcasting's original feel. Instead, the rare
/// "orphaned wall" keyhole artifact (a tile's slope-interval sweep marking it visible without
/// ever validating the connecting tile between it and the observer) is fixed by
/// FilterToConnected: a post-pass that keeps only the tiles reachable from the origin by walking
/// through OTHER already-visible, non-opaque tiles. A tile shadowcasting marks visible through a
/// keyhole -- without the connecting gap tile also being visible -- has no such path and gets
/// pruned; every ordinary line of sight (which always has a fully visible chain of tiles leading
/// to it) is completely unaffected.
/// </summary>
public static class FieldOfView
{
    // Per octant: (xx, xy, yx, yy) transform matrices mapping the
    // "canonical" octant (down-left, sweeping counter-clockwise) onto
    // each of the 8 real octants around the origin.
    private static readonly int[,] Octants =
    {
        { 1, 0, 0, 1 },
        { 0, 1, 1, 0 },
        { 0, -1, 1, 0 },
        { -1, 0, 0, 1 },
        { -1, 0, 0, -1 },
        { 0, -1, -1, 0 },
        { 0, 1, -1, 0 },
        { 1, 0, 0, -1 }
    };

    private static readonly (int Dx, int Dy)[] EightDirections =
    {
        (0, -1), (0, 1), (-1, 0), (1, 0), (-1, -1), (1, -1), (-1, 1), (1, 1)
    };

    /// <summary>
    /// The player's own field of view -- raw line-of-sight from ComputeVisibleCells, then gated
    /// by darkness one of two different ways depending on whether the OBSERVER (not the tile
    /// being looked at) currently stands somewhere dark and unlit ("blind"):
    ///
    /// Not blind (normal vision, from a lit tile) -- exactly the original per-tile rule (design
    /// spec section 3): an ordinary (non-dark) tile only needs line of sight, same as before this
    /// system existed; a dark-room tile additionally needs Tile.IsIlluminated. This is why a door
    /// on a dark room's own boundary is plainly visible when approached from a normal, lit
    /// corridor -- the DOOR's own tile isn't marked IsDarkRoom (see DungeonGenerator.
    /// PaintDarkRoomTiles), so from a sighted observer it's just an ordinary tile.
    ///
    /// Blind (standing on a dark, unilluminated tile right now) -- darkness doesn't just hide
    /// what's dark, it blinds the OBSERVER: nothing new can be discovered purely by geometry
    /// while you yourself can't see. A tile only becomes visible if it's actually illuminated
    /// right now (regardless of its own IsDarkRoom -- a torch lights up an ordinary corridor tile
    /// just as much as a dark-room one), OR if it was already Explored from an earlier, sighted
    /// encounter, in which case it stays remembered (rendered dim by Renderer, matching every
    /// other explored-but-not-currently-visible tile) without becoming freshly IsVisible again.
    /// This is what stops a player standing blind inside a dark room from "seeing" every door on
    /// its boundary via plain unobstructed geometry the instant they walk in -- only a door
    /// they've genuinely laid eyes on before (from its lit approach) or one a current light source
    /// actually reaches is perceivable; an undiscovered one stays exactly as hidden as the wall
    /// around it.
    /// </summary>
    public static void Compute(Level level, int originX, int originY, int radius)
    {
        for (int x = 0; x < level.Width; x++)
        {
            for (int y = 0; y < level.Height; y++)
            {
                level.Tiles[x, y].IsVisible = false;
            }
        }

        var originTile = level.Tiles[originX, originY];
        bool observerIsBlind = originTile.IsDarkRoom && !originTile.IsIlluminated;

        // A SIGHTED observer's own line of sight stops at the first unlit dark-room tile it
        // meets, the same as it would at a wall -- darkness itself is what's stopping the eye,
        // not solid geometry, but the effect on what can be seen BEYOND it is identical: you can
        // see the black doorway of a dark room from its lit approach, but not clear across its
        // unlit interior to the far wall or a door on its opposite side (that far geometry isn't
        // "behind a wall", it's just past more darkness the eye never gets any light back from).
        // A BLIND observer (see below) is deliberately exempted -- their own visibility is
        // already fully gated by the per-tile check right below (only currently-illuminated or
        // already-remembered tiles ever show), and geometry must still be free to reach a
        // distant active light source (another actor's torch, etc.) across the same dark room for
        // that discovery to work at all; only a SIGHTED onlooker's unobstructed view is what
        // needed this extra stop.
        foreach (var (x, y) in ComputeVisibleCells(level, originX, originY, radius, treatUnlitDarknessAsOpaque: !observerIsBlind))
        {
            var tile = level.Tiles[x, y];

            if (observerIsBlind)
            {
                if (tile.IsIlluminated)
                {
                    tile.IsVisible = true;
                    tile.IsExplored = true;
                }
                // Already explored: left exactly as it was (IsExplored stays true, IsVisible
                // stays false from the reset above) -- remembered, not freshly seen. Never
                // explored and not illuminated: stays fully hidden, same as an untouched wall.
            }
            else if (!(tile.IsDarkRoom && !tile.IsIlluminated))
            {
                tile.IsVisible = true;
                tile.IsExplored = true;
            }
        }
    }

    /// <summary>
    /// Every cell visible from (originX, originY) out to radius, respecting walls/opaque terrain
    /// -- the origin itself is always included. Shared by the player's own FOV (Compute above,
    /// which further gates the result by darkness) and by light-source illumination (see
    /// Core/LightingSystem.cs), so both use the identical shadowcasting algorithm instead of two
    /// parallel implementations -- this is also what makes light "just work" against walls (design
    /// spec section 4) with no extra code: a light source's reach is computed exactly the same
    /// way the player's own eyes are. The raw shadowcast result is passed through
    /// FilterToConnected before returning -- see the class doc comment.
    /// </summary>
    /// <param name="treatUnlitDarknessAsOpaque">
    /// Only ever passed true by Compute's own sighted-observer call -- see its doc comment. Must
    /// stay false (the default) for every other caller, in particular LightingSystem.
    /// RecomputeIllumination's own use of this method: that pass runs BEFORE Tile.IsIlluminated
    /// has been recomputed for the current turn (every tile was just reset to false), so treating
    /// "not currently illuminated" as opaque there would make a light source's own shadowcast
    /// block on the very first dark tile next to it, unable to ever light more than one tile deep
    /// into a dark room.
    /// </param>
    public static HashSet<(int X, int Y)> ComputeVisibleCells(Level level, int originX, int originY, int radius, bool treatUnlitDarknessAsOpaque = false)
    {
        var visible = new HashSet<(int X, int Y)>();
        if (!level.IsInBounds(originX, originY))
        {
            return visible;
        }

        visible.Add((originX, originY));

        for (int octant = 0; octant < 8; octant++)
        {
            CastLight(
                level, originX, originY,
                row: 1, start: 1.0, end: 0.0, radius,
                xx: Octants[octant, 0], xy: Octants[octant, 1],
                yx: Octants[octant, 2], yy: Octants[octant, 3],
                visible, treatUnlitDarknessAsOpaque);
        }

        var connected = FilterToConnected(level, visible, originX, originY, treatUnlitDarknessAsOpaque);

        // Regardless of what the shadowcasting sweep and the keyhole-connectivity filter above
        // computed, a tile that isn't part of the dungeon's actual connected graph (Tile.
        // IsReachable) never shows -- this is what actually closes off the "orphaned wall"
        // artifact (a diagonal sight/light leak through a wall corner into a sealed-off pocket),
        // for both the player's own FOV (FieldOfView.Compute) and every light source
        // (LightingSystem.RecomputeIllumination), since both funnel through this one method.
        connected.RemoveWhere(pos => !level.Tiles[pos.X, pos.Y].IsReachable);
        return connected;
    }

    /// <summary>
    /// Keeps only the tiles in `raw` reachable from the origin by a chain of other tiles that are
    /// ALSO in `raw` (8-directional adjacency, matching shadowcasting's own diagonal sight) --
    /// walking stops at (but still includes) an opaque tile, since sight itself stops there too;
    /// nothing beyond an opaque tile can extend the chain further. A tile shadowcasting's
    /// slope-interval sweep marked visible without ever validating the tile connecting it back to
    /// the origin (the "keyhole" artifact) has no such chain and is dropped here; every ordinary
    /// line of sight -- which always has a fully-visible run of tiles leading to it -- passes
    /// through untouched.
    /// </summary>
    private static HashSet<(int X, int Y)> FilterToConnected(Level level, HashSet<(int X, int Y)> raw, int originX, int originY, bool treatUnlitDarknessAsOpaque)
    {
        var connected = new HashSet<(int X, int Y)> { (originX, originY) };
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue((originX, originY));

        while (queue.Count > 0)
        {
            var (cx, cy) = queue.Dequeue();

            // An opaque tile is a dead end for further propagation -- it's still included in
            // `connected` itself (you can see the wall you're looking at), but sight doesn't
            // continue past it, so it can never vouch for anything beyond it.
            if ((cx, cy) != (originX, originY) && IsOpaque(level, cx, cy, treatUnlitDarknessAsOpaque))
            {
                continue;
            }

            foreach (var (dx, dy) in EightDirections)
            {
                var neighbor = (cx + dx, cy + dy);
                if (raw.Contains(neighbor) && connected.Add(neighbor))
                {
                    queue.Enqueue(neighbor);
                }
            }
        }

        return connected;
    }

    private static bool IsOpaque(Level level, int x, int y, bool treatUnlitDarknessAsOpaque)
    {
        if (!level.IsTransparent(x, y))
        {
            return true;
        }
        var tile = level.Tiles[x, y];
        return treatUnlitDarknessAsOpaque && tile.IsDarkRoom && !tile.IsIlluminated;
    }

    private static void CastLight(
        Level level, int originX, int originY,
        int row, double start, double end, int radius,
        int xx, int xy, int yx, int yy, HashSet<(int X, int Y)> visible, bool treatUnlitDarknessAsOpaque)
    {
        if (start < end)
        {
            return;
        }

        double newStart = 0;

        for (int distance = row; distance <= radius && start >= end; distance++)
        {
            int deltaY = -distance;
            bool blocked = false;

            for (int deltaX = -distance; deltaX <= 0; deltaX++)
            {
                int currentX = originX + (deltaX * xx) + (deltaY * xy);
                int currentY = originY + (deltaX * yx) + (deltaY * yy);

                double leftSlope = (deltaX - 0.5) / (deltaY + 0.5);
                double rightSlope = (deltaX + 0.5) / (deltaY - 0.5);

                if (!level.IsInBounds(currentX, currentY) || start < rightSlope)
                {
                    continue;
                }

                if (end > leftSlope)
                {
                    break;
                }

                if ((deltaX * deltaX) + (deltaY * deltaY) <= radius * radius)
                {
                    visible.Add((currentX, currentY));
                }

                if (blocked)
                {
                    if (IsOpaque(level, currentX, currentY, treatUnlitDarknessAsOpaque))
                    {
                        newStart = rightSlope;
                        continue;
                    }

                    blocked = false;
                    start = newStart;
                }
                else if (IsOpaque(level, currentX, currentY, treatUnlitDarknessAsOpaque) && distance < radius)
                {
                    blocked = true;
                    CastLight(level, originX, originY, distance + 1, start, leftSlope, radius, xx, xy, yx, yy, visible, treatUnlitDarknessAsOpaque);
                    newStart = rightSlope;
                }
            }

            if (blocked)
            {
                break;
            }
        }
    }
}
