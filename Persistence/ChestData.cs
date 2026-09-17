namespace BENEATH_FORGOTTEN_STONE.Persistence;

/// <summary>
/// Serializable mirror of Dungeon.Chest. `Container` is null only for a save written before the
/// Persistent Containers feature shipped -- see SaveManager.FromChestData for the old-save
/// fallback (a fresh ContainerComponent synthesized from ChestConfig.DefaultSlotCapacity, with
/// IsOpen carried over from the deprecated top-level field below).
/// </summary>
public class ChestData
{
    public int X { get; set; }
    public int Y { get; set; }
    public bool IsLocked { get; set; }
    public int Difficulty { get; set; }
    public ContainerData Container { get; set; }

    /// <summary>Deprecated -- superseded by Container.Contents. Only ever populated by (and read back from) a pre-Persistent-Containers save file.</summary>
    public List<ItemData> Contents { get; set; } = new();

    /// <summary>Deprecated -- superseded by Container.IsOpen. Only ever populated by (and read back from) a pre-Persistent-Containers save file.</summary>
    public bool IsOpen { get; set; }
}
