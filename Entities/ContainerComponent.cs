using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>A Chest (stationary, root-level) or a PortableBag (carried, cannot hold another container) -- see ContainerRules.CanInsert, which is the only place this distinction actually matters.</summary>
public enum ContainerKind
{
    PortableBag,
    Chest
}

/// <summary>Meaningful only when ContainerComponent.Kind is PortableBag -- a Chest accepts any ordinary item (including bags) regardless of this value. See ContainerRules.CanInsert.</summary>
public enum ContainerSizeClass
{
    Small,
    Medium,
    Large
}

/// <summary>
/// Shared contents/capacity/weight-reduction model for both a portable bag (held on an Item via
/// Item.Container) and a treasure Chest (held on Chest.Container) -- see the Persistent
/// Containers and Magical Bags proposal. Contents reuses InventoryComponent (the same type
/// Player/Monster/Trader already carry) so ItemStacking's existing stack-merge/consume/
/// consolidate logic works against a container's contents with no new stacking code.
/// </summary>
public class ContainerComponent
{
    public ContainerKind Kind { get; }
    public ContainerSizeClass SizeClass { get; }
    public int SlotCapacity { get; }

    /// <summary>0.0-0.75 -- fraction of RAW content weight that is NOT counted toward the carrying player's weight. Always 0 for a Chest (its contents aren't being carried at all). See ContainerWeightCalculator.</summary>
    public double WeightReduction { get; }

    public InventoryComponent Contents { get; } = new();

    /// <summary>
    /// A Chest's open/closed state persists indefinitely and is toggled by GameLoop's
    /// open-chest interaction (see Core/GameLoop.cs HandleOpenChest). A bag's is set true only
    /// while its ContainerScreen is actually on screen and reset false the moment that screen
    /// is exited (see Core/Screens/InventoryScreen.cs's 'O' handler) -- so for a bag this is
    /// essentially always false except mid-visit; it exists mainly for save/load fidelity and
    /// the InventoryScreen summary line, not as meaningful standing state the way a Chest's is.
    /// </summary>
    public bool IsOpen { get; set; }

    public ContainerComponent(ContainerKind kind, ContainerSizeClass sizeClass, int slotCapacity, double weightReduction)
    {
        Kind = kind;
        SizeClass = sizeClass;
        SlotCapacity = slotCapacity;
        WeightReduction = weightReduction;
    }

    /// <summary>A fresh, independently-mutable, EMPTY container with the same shape -- see Item.Clone(). Every spawned bag must get its own instance; two spawned bags must never share a Contents list.</summary>
    public ContainerComponent CloneEmpty() => new(Kind, SizeClass, SlotCapacity, WeightReduction);
}
