using BENEATH_FORGOTTEN_STONE.Entities;

namespace BENEATH_FORGOTTEN_STONE.Dungeon;

/// <summary>
/// Shared straight-line tile walker: steps outward from an origin until it
/// hits an actor, a wall, an unseen tile (if requireVisible), or maxSteps.
/// Originally duplicated inline for spell targeting (single-target actor
/// pick, tile/AoE impact point); extracted so the Look command can reuse
/// the exact same walk with one added rule (respect FOV) instead of a
/// third copy of the same loop.
/// </summary>
public static class RayTracer
{
    /// <returns>The first actor encountered, or null if the ray hits a wall/blocking RoomObject/unseen tile/maxSteps first.</returns>
    public static Actor FindActorAlongRay(Level level, int x, int y, int dx, int dy, int maxSteps, bool requireVisible = false)
    {
        int cx = x, cy = y;
        for (int step = 1; step <= maxSteps; step++)
        {
            cx += dx;
            cy += dy;
            if (!level.IsInBounds(cx, cy))
            {
                break;
            }
            if (requireVisible && !level.Tiles[cx, cy].IsVisible)
            {
                break;
            }

            var actor = level.GetActorAt(cx, cy);
            if (actor != null)
            {
                return actor;
            }

            // A movement-blocking RoomObject is exactly as solid as a wall for ray-based
            // targeting/Look purposes -- a fireball doesn't sail past a boulder just because the
            // boulder "doesn't count" as terrain.
            if (!level.IsWalkable(cx, cy) || level.GetRoomObjectAt(cx, cy) is { BlocksMovement: true })
            {
                break;
            }
        }
        return null;
    }

    /// <returns>The last tile the ray reaches before a wall/blocking RoomObject/unseen tile/maxSteps -- stops early at the first actor's or blocking RoomObject's own tile too, so an impact point always lands on whoever/whatever is standing there.</returns>
    public static (int X, int Y) FindTileAlongRay(Level level, int x, int y, int dx, int dy, int maxSteps, bool requireVisible = false)
    {
        int cx = x, cy = y;
        for (int step = 1; step <= maxSteps; step++)
        {
            int nx = cx + dx, ny = cy + dy;
            if (!level.IsInBounds(nx, ny) || !level.IsWalkable(nx, ny))
            {
                break;
            }
            if (requireVisible && !level.Tiles[nx, ny].IsVisible)
            {
                break;
            }
            cx = nx;
            cy = ny;

            if (level.GetActorAt(cx, cy) != null)
            {
                break;
            }
            // Settle ON a blocking RoomObject's own tile, same as an actor's -- see
            // FindActorAlongRay's identical reasoning.
            if (level.GetRoomObjectAt(cx, cy) is { BlocksMovement: true })
            {
                break;
            }
        }
        return (cx, cy);
    }
}
