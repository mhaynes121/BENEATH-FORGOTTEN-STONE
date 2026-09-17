using BENEATH_FORGOTTEN_STONE.Entities;
using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Dungeon;

/// <summary>
/// A persistent world container placed by DungeonGenerator.SpawnChests -- opened/closed via
/// GameLoop.HandleOpenChest's toggle, contents managed via Core/Screens/ContainerScreen.cs. Not
/// every instance is actually locked -- see IsLocked. An unlocked one opens for free; a locked
/// one is gated on the player knowing SkillCatalog.PickLock. Renamed from the historical
/// "LockedChest" now that this class is a full persistent container, not a one-shot spill-and-
/// forget object. Kept as a Level-level overlay list (mirroring how GroundItems already overlays
/// tiles) rather than a new Tile subtype, so Tile's own factory pattern (CreateWall/CreateFloor/
/// ...) stays untouched.
/// </summary>
public class Chest
{
    public int X { get; }
    public int Y { get; }

    public bool IsLocked { get; set; }

    /// <summary>Compared against a Knowledge-based roll in GameLoop.HandleOpenChest. Meaningless when IsLocked is false.</summary>
    public int Difficulty { get; }

    /// <summary>ContainerKind.Chest, ChestConfig.DefaultSlotCapacity slots, 0 weight reduction (chest contents are never being carried, so there's nothing to discount). See ContainerComponent.</summary>
    public ContainerComponent Container { get; } = new(ContainerKind.Chest, default, ChestConfig.DefaultSlotCapacity, 0);

    /// <summary>True once opened -- persists indefinitely afterward (toggled, not one-shot). See ContainerComponent.IsOpen's own doc comment for how this differs from a bag's transient IsOpen.</summary>
    public bool IsOpen
    {
        get => Container.IsOpen;
        set => Container.IsOpen = value;
    }

    public Chest(int x, int y, bool isLocked, int difficulty, List<Item> contents)
    {
        X = x;
        Y = y;
        IsLocked = isLocked;
        Difficulty = difficulty;
        foreach (var item in contents)
        {
            Container.Contents.AddItem(item);
        }
    }
}
