using BENEATH_FORGOTTEN_STONE.Entities;

namespace BENEATH_FORGOTTEN_STONE.Dungeon;

/// <summary>
/// A substantial environmental feature occupying one tile -- a fountain, shrine, boulder, or
/// statue (see RoomObjectType/RoomObjectCatalog). Kept as a Level-level overlay list, the same
/// established pattern Chest/Trap/Door already use, rather than a new Tile subtype --
/// nothing here needs Tile's own factory pattern touched. Mobility (IsMovable) and every
/// blocking flag are set PER INSTANCE, not solely by Type -- the design spec's own example is a
/// puzzle statue that can be pushed while every other statue in the dungeon stays fixed, so
/// RoomObjectCatalog's per-type defaults are just a starting point a specific spawn can override
/// (see DungeonGenerator.SpawnRoomObjects).
/// </summary>
public class RoomObject : IInspectable
{
    public int X { get; set; }
    public int Y { get; set; }

    public RoomObjectType Type { get; }
    public string DisplayName { get; }
    public string ShortDescription { get; }
    public string LongDescription { get; }
    public char Symbol { get; }
    public ConsoleColor Color { get; }

    public bool BlocksMovement { get; }

    /// <summary>Whether this object blocks FOV/line-of-sight (Level.IsTransparent) -- independent of BlocksMovement, e.g. a boulder blocks both while a low fountain or shrine blocks only movement (design spec: "a boulder can block both while a fountain or shrine could use different behavior").</summary>
    public bool BlocksVision { get; }

    /// <summary>Whether a fired projectile stops here instead of flying past -- see ProjectileEngine.Launch. Independent of the other two flags for the same reason as BlocksVision.</summary>
    public bool BlocksProjectiles { get; }

    public bool IsMovable { get; }

    /// <summary>Null for the vast majority of objects -- see RoomObjectTrigger's own doc comment. Settable (not just gettable) so DungeonGenerator can wire a puzzle's statue and its trigger together as one coordinated placement.</summary>
    public RoomObjectTrigger Trigger { get; set; }

    public RoomObject(
        int x, int y, RoomObjectType type, string displayName, string shortDescription, string longDescription,
        char symbol, ConsoleColor color, bool blocksMovement, bool blocksVision, bool blocksProjectiles, bool isMovable,
        RoomObjectTrigger trigger = null)
    {
        X = x;
        Y = y;
        Type = type;
        DisplayName = displayName;
        ShortDescription = shortDescription;
        LongDescription = longDescription;
        Symbol = symbol;
        Color = color;
        BlocksMovement = blocksMovement;
        BlocksVision = blocksVision;
        BlocksProjectiles = blocksProjectiles;
        IsMovable = isMovable;
        Trigger = trigger;
    }
}
