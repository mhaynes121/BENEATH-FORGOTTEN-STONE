namespace BENEATH_FORGOTTEN_STONE.Entities.Components;

/// <summary>
/// No count cap -- carry capacity is governed entirely by weight now (see
/// EncumbranceCalculator), checked by the caller (GameLoop.HandlePickUp)
/// before adding a newly-acquired item. Moving an item between here and
/// EquipmentComponent (equip/unequip) never changes total carried weight,
/// so those callers never need to check anything before calling AddItem.
/// </summary>
public class InventoryComponent
{
    public List<Item> Items { get; } = new();

    public void AddItem(Item item) => Items.Add(item);

    public bool RemoveItem(Item item) => Items.Remove(item);
}
