using BENEATH_FORGOTTEN_STONE.Dungeon;

namespace BENEATH_FORGOTTEN_STONE.Persistence;

/// <summary>
/// Serializable snapshot of one visited Level -- SaveData holds one of
/// these per floor the player has ever entered, not just the current one,
/// so backtracking after a reload shows an earlier floor exactly as left.
/// TileTypes/TileExplored are flattened (index = x * Height + y) since
/// System.Text.Json doesn't serialize multi-dimensional arrays. IsWalkable/
/// IsTransparent/Symbol aren't stored -- Tile's own factory methods
/// (CreateWall/CreateFloor/...) already derive them from Type alone, and
/// IsVisible is transient (recomputed by FieldOfView.Compute every time
/// the floor becomes current again), so neither needs to round-trip.
/// </summary>
public class LevelData
{
    public int FloorIndex { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int TurnNumber { get; set; }
    public int StairsUpX { get; set; }
    public int StairsUpY { get; set; }
    public int StairsDownX { get; set; }
    public int StairsDownY { get; set; }
    public TileType[] TileTypes { get; set; }
    public bool[] TileExplored { get; set; }

    /// <summary>Flattened the same way as TileTypes -- see Tile.FloorType. Null for saves written before the floor-type system existed; SaveManager.FromLevelData treats that the same as every tile defaulting to FloorType.Normal, the same fallback Tile itself already uses.</summary>
    public FloorType[] TileFloorTypes { get; set; }

    /// <summary>Flattened the same way as TileTypes -- see Tile.IsDarkRoom. Null for saves written before the darkness system existed; SaveManager.FromLevelData treats that the same as every tile defaulting to IsDarkRoom = false. Tile.IsIlluminated is NOT saved -- transient, freshly recomputed from active light sources on load, same as IsVisible.</summary>
    public bool[] TileIsDarkRoom { get; set; }
    public List<MonsterData> Monsters { get; set; } = new();
    public List<TraderData> Traders { get; set; } = new();
    public List<GroundItemData> GroundItems { get; set; } = new();
    public List<ChestData> Chests { get; set; } = new();

    /// <summary>Corpse System: loot-bearing corpses only -- an emptied/portable one is already covered by GroundItems above. Default-initialized like RoomObjects below, so a save written before this feature existed simply deserializes to an empty list.</summary>
    public List<CorpseData> Corpses { get; set; } = new();
    public List<TrapData> Traps { get; set; } = new();
    public List<DoorData> Doors { get; set; } = new();

    /// <summary>Room Objects spec. Default-initialized like every other overlay list here, so a save written before this system existed simply deserializes to an empty list -- no null-check needed on load, unlike the array-typed TileFloorTypes/TileIsDarkRoom above.</summary>
    public List<RoomObjectData> RoomObjects { get; set; } = new();

    /// <summary>Ambient Sound / Hearing System spec -- the fixed environmental sound sources rolled at generation time (design spec section 3). Structural data (like TileFloorTypes), not the transient per-turn cooldown/silence state, which is deliberately never persisted -- see Dungeon/DungeonSoundState's own doc comment.</summary>
    public List<AmbientSoundSourceData> AmbientSoundSources { get; set; } = new();
}

/// <summary>Serializable form of Dungeon/AmbientSoundSource.</summary>
public class AmbientSoundSourceData
{
    public int X { get; set; }
    public int Y { get; set; }
    public FloorType FloorType { get; set; }
}
