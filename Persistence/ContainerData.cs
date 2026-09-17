using BENEATH_FORGOTTEN_STONE.Entities;

namespace BENEATH_FORGOTTEN_STONE.Persistence;

/// <summary>Serializable mirror of Entities.ContainerComponent -- shared by a portable bag (via ItemData.Container) and a treasure Chest (via ChestData.Container), matching how both use the same ContainerComponent type in memory.</summary>
public class ContainerData
{
    public ContainerKind Kind { get; set; }
    public ContainerSizeClass SizeClass { get; set; }
    public int SlotCapacity { get; set; }
    public double WeightReduction { get; set; }
    public bool IsOpen { get; set; }
    public List<ItemData> Contents { get; set; } = new();
}
