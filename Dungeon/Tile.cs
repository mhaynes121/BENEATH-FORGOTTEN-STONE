namespace BENEATH_FORGOTTEN_STONE.Dungeon;

public class Tile
{
    public TileType Type { get; set; }
    public bool IsWalkable { get; set; }
    public bool IsTransparent { get; set; }
    public bool IsExplored { get; set; }
    public bool IsVisible { get; set; }

    /// <summary>What this tile actually is, on top of Type -- only meaningful for TileType.Floor (a Wall/StairsUp/StairsDown tile's look never changes). Defaults to Normal, so every tile anywhere that never explicitly gets a special type (including every tile from a save written before this existed) behaves exactly as before. See FloorTypeCatalog for what each value looks like and does -- always branch on this field, never on Symbol/DisplayCharacter, since two FloorTypes can intentionally share a glyph (Water and Lava both use '~').</summary>
    public FloorType FloorType { get; set; } = FloorType.Normal;

    /// <summary>
    /// Whether this tile belongs to a room the dungeon generator marked naturally dark --
    /// persistent per-tile state assigned once at generation (see DungeonGenerator.AssignDarkRooms),
    /// never toggled during play. Defaults to false, so every ordinary room (and every tile from
    /// a save written before this system existed) behaves exactly as before. Meaningless on its
    /// own -- FieldOfView.Compute only actually hides a dark tile's contents when it's ALSO not
    /// IsIlluminated; see that gating logic for the real "can the player perceive this" rule.
    /// </summary>
    public bool IsDarkRoom { get; set; }

    /// <summary>
    /// Whether an active light source (a lit physical item, Arcane Orb, Divine Radiance, ...)
    /// currently reaches this tile -- transient, recomputed every turn by
    /// LightingSystem.RecomputeIllumination the same way IsVisible is recomputed by
    /// FieldOfView.Compute, and NOT persisted (light sources are saved/restored by their own
    /// state -- IsLit/RemainingLightDuration on Item, the ActiveEffect's own ExpiresOnTurn -- and
    /// illumination is always freshly rederived from that on load, never stored per-tile).
    /// Meaningless outside a dark room (see IsDarkRoom); an ordinary room's visibility never
    /// consults this at all.
    /// </summary>
    public bool IsIlluminated { get; set; }

    /// <summary>
    /// Whether this tile is part of the dungeon's actual connected graph -- reachable from
    /// StairsUpPosition by a chain of walkable floor tiles (a wall is reachable too if it borders
    /// at least one such tile, so a legitimately reachable room's own walls still render). NOT
    /// persisted -- always freshly recomputed by Level.RecomputeReachability from the current tile
    /// layout (see DungeonGenerator, SaveManager.FromLevelData, RoomObjectTriggerProcessor's
    /// RevealHiddenDoor case), the same "derive it, don't store it" approach IsIlluminated already
    /// uses. Exists to close off the "orphaned wall" artifact at its root: FieldOfView.
    /// ComputeVisibleCells refuses to ever mark an unreachable tile visible or illuminated,
    /// regardless of what shadowcasting's slope-interval sweep (or a diagonal sight/light leak
    /// through a wall corner) would otherwise compute -- a sealed-off pocket left behind by a
    /// generation bug, or one not yet connected by an undiscovered hidden door, simply never shows.
    /// Defaults to true (not the usual bool default) so every hand-built test level that never
    /// calls RecomputeReachability -- the vast majority of Diagnostics/SelfTest.cs's FOV/lighting/
    /// dark-room fixtures -- keeps behaving exactly as it did before this field existed; only a
    /// real generated or restored Level, which always calls RecomputeReachability at least once,
    /// ever gets the real computed value instead.
    /// </summary>
    public bool IsReachable { get; set; } = true;

    public char Symbol => Type switch
    {
        TileType.Wall => '#',
        TileType.Floor => '.',
        TileType.StairsDown => '>',
        TileType.StairsUp => '<',
        _ => '?'
    };

    public static Tile CreateWall() => new Tile
    {
        Type = TileType.Wall,
        IsWalkable = false,
        IsTransparent = false
    };

    public static Tile CreateFloor() => new Tile
    {
        Type = TileType.Floor,
        IsWalkable = true,
        IsTransparent = true
    };

    public static Tile CreateStairsDown() => new Tile
    {
        Type = TileType.StairsDown,
        IsWalkable = true,
        IsTransparent = true
    };

    public static Tile CreateStairsUp() => new Tile
    {
        Type = TileType.StairsUp,
        IsWalkable = true,
        IsTransparent = true
    };

    /// <summary>Dispatches to the matching factory above -- used when reconstructing a saved floor from its flattened TileType[] (see LevelData), where only Type/IsExplored are persisted.</summary>
    public static Tile Create(TileType type) => type switch
    {
        TileType.Wall => CreateWall(),
        TileType.Floor => CreateFloor(),
        TileType.StairsDown => CreateStairsDown(),
        TileType.StairsUp => CreateStairsUp(),
        _ => CreateWall()
    };
}
