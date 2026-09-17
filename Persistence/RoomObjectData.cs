using BENEATH_FORGOTTEN_STONE.Dungeon;

namespace BENEATH_FORGOTTEN_STONE.Persistence;

/// <summary>
/// Serializable mirror of Dungeon.RoomObject. Cosmetic fields (DisplayName/description/symbol/
/// color/blocking flags) are never stored directly -- they're fully determined by Type+IsMovable
/// via RoomObjectCatalog.Create, so storing them again would just be redundant text that could
/// drift out of sync with the catalog. Only what the catalog can't re-derive (current position,
/// whether this particular instance is movable, and any trigger's activation state) round-trips.
/// </summary>
public class RoomObjectData
{
    public RoomObjectType Type { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public bool IsMovable { get; set; }
    public RoomObjectTriggerData Trigger { get; set; }
}

/// <summary>
/// Serializable mirror of Dungeon.RoomObjectTrigger. Tuple-shaped fields (TrackedPosition,
/// HiddenDoorPosition, SpawnPositions) are flattened into plain ints/lists since
/// System.Text.Json has no built-in support for ValueTuple.
/// </summary>
public class RoomObjectTriggerData
{
    public RoomObjectTriggerCondition Condition { get; set; }
    public RoomObjectTriggerEffect Effect { get; set; }
    public bool Repeatable { get; set; }
    public bool HasActivated { get; set; }
    public string Message { get; set; }
    public int? TrackedX { get; set; }
    public int? TrackedY { get; set; }
    public int? HiddenDoorX { get; set; }
    public int? HiddenDoorY { get; set; }
    public List<int> SpawnPositionsX { get; set; } = new();
    public List<int> SpawnPositionsY { get; set; } = new();
    public int SpawnDifficultyLevel { get; set; }
}
