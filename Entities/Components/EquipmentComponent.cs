using BENEATH_FORGOTTEN_STONE.Entities;

namespace BENEATH_FORGOTTEN_STONE.Entities.Components;

/// <summary>
/// The single authoritative record of what's equipped in each of the 12
/// slots -- replaces the old EquippedWeapon/EquippedArmor fields on
/// Player. Doesn't apply stat effects itself (that's Player's job, since
/// it owns CharacterStats/derived combat stats); this is purely "what's
/// where."
/// </summary>
public class EquipmentComponent
{
    private readonly Dictionary<EquipmentSlot, Item> equipped = new();

    public Item Get(EquipmentSlot slot) => equipped.GetValueOrDefault(slot);

    public IEnumerable<KeyValuePair<EquipmentSlot, Item>> AllEquipped => equipped;

    /// <summary>
    /// First empty slot compatible with the item's EquipmentType. Fixed
    /// single-slot types (Head, Body, ...) always resolve -- occupied just
    /// means "replace it," no ambiguity. Multi-slot types (Hand, Ring)
    /// resolve to the first empty one, or null if both are occupied (the
    /// caller must ask the player which to replace).
    /// </summary>
    public EquipmentSlot? FindAutoEquipSlot(Item item)
    {
        var compatible = EquipmentCompatibility.GetCompatibleSlots(item);
        if (compatible.Count == 0)
        {
            return null;
        }
        if (compatible.Count == 1)
        {
            return compatible[0];
        }

        foreach (var slot in compatible)
        {
            if (!equipped.ContainsKey(slot))
            {
                return slot;
            }
        }
        return null;
    }

    /// <returns>The item previously in that slot, if any.</returns>
    public Item EquipInSlot(EquipmentSlot slot, Item item)
    {
        equipped.TryGetValue(slot, out var previous);
        equipped[slot] = item;
        return previous;
    }

    /// <returns>The item that was in that slot, or null if it was already empty.</returns>
    public Item Unequip(EquipmentSlot slot)
    {
        if (equipped.TryGetValue(slot, out var item))
        {
            equipped.Remove(slot);
            return item;
        }
        return null;
    }
}
