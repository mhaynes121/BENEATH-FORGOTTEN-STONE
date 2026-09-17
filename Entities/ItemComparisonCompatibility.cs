using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Centralizes "are these two items meaningfully comparable" -- category is always a function of
/// item + the slot it occupies or would occupy, never the item alone, so a hybrid Spear correctly
/// classifies as a MeleeWeapon in a hand slot but as Ammunition in the Ammo/Thrown slot. See the
/// Item Comparison proposal's own worked example (a Spear in Ammunition must never be offered as
/// a sword's replacement).
/// </summary>
public static class ItemComparisonCompatibility
{
    public static ItemComparisonCategory CategoryFor(Item item, EquipmentSlot slot)
    {
        if (!EquipmentCompatibility.IsCompatible(item, slot))
        {
            return ItemComparisonCategory.None;
        }

        return slot switch
        {
            EquipmentSlot.PrimaryHand or EquipmentSlot.OffHand => item.EquipmentCategory switch
            {
                EquipmentCategory.Shield => ItemComparisonCategory.Shield,
                EquipmentCategory.Wand => ItemComparisonCategory.Wand,
                EquipmentCategory.Weapon => ItemComparisonCategory.MeleeWeapon,
                _ => ItemComparisonCategory.None
            },
            EquipmentSlot.RangedWeapon => ItemComparisonCategory.RangedWeapon,
            EquipmentSlot.Ammunition => ItemComparisonCategory.Ammunition,
            EquipmentSlot.PrimaryRing or EquipmentSlot.OffHandRing => ItemComparisonCategory.Ring,
            EquipmentSlot.Gloves => ItemComparisonCategory.Gloves,
            EquipmentSlot.Wrists => ItemComparisonCategory.Wrists,
            EquipmentSlot.Arms => ItemComparisonCategory.Arms,
            EquipmentSlot.Body => ItemComparisonCategory.BodyArmor,
            EquipmentSlot.Neck => ItemComparisonCategory.Neck,
            EquipmentSlot.Head => ItemComparisonCategory.Head,
            EquipmentSlot.Legs => ItemComparisonCategory.Legs,
            EquipmentSlot.Feet => ItemComparisonCategory.Feet,
            _ => ItemComparisonCategory.None
        };
    }

    /// <summary>
    /// The slot an UNEQUIPPED item would be judged by for comparison purposes: its actual
    /// equipped slot if the player currently has this exact instance equipped, else its
    /// first-preference compatible slot (EquipmentCompatibility.GetCompatibleSlots already orders
    /// a hybrid Spear Ammo-first or Hand-first per ItemStacking.IsStackable -- reusing that same
    /// ordering here keeps this consistent with how the item would actually get auto-equipped).
    /// </summary>
    public static EquipmentSlot? EffectiveSlot(Item item, Player player)
    {
        foreach (var kvp in player.Equipment.AllEquipped)
        {
            if (ReferenceEquals(kvp.Value, item))
            {
                return kvp.Key;
            }
        }

        var slots = EquipmentCompatibility.GetCompatibleSlots(item);
        return slots.Count > 0 ? slots[0] : null;
    }

    /// <summary>Whether two owned items (equipped or not) share a comparison category -- used by the Inventory/Trader Compare screens. Identification never affects this; category comes entirely from EquipmentType/EquipmentCategory, which are known internally regardless of IsIdentified.</summary>
    public static bool CanCompare(Item first, Item second, Player player)
    {
        var slotA = EffectiveSlot(first, player);
        var slotB = EffectiveSlot(second, player);
        if (slotA == null || slotB == null)
        {
            return false;
        }

        var categoryA = CategoryFor(first, slotA.Value);
        return categoryA != ItemComparisonCategory.None && categoryA == CategoryFor(second, slotB.Value);
    }

    /// <summary>
    /// Full replacement legality for a FIXED equipped item+slot -- same category at that slot,
    /// the equipped item isn't cursed (EquipmentCompatibility.CanEquip does not check IsCursed;
    /// that gate lives only in InventoryScreen.TryUnequip today, so it's applied explicitly here
    /// too, or an automatic upgrade prompt could silently swap out a cursed item), and the
    /// candidate actually passes the full equip-legality gate for that slot (level/class/race/
    /// weapon-armor restrictions/category conflicts/Dual Wield -- everything CanEquip already
    /// checks, correctly excluding the slot's own current occupant from the conflict check).
    /// </summary>
    public static bool CanReplaceInContext(Player player, Item candidate, Item equipped, EquipmentSlot slot) =>
        CategoryFor(equipped, slot) != ItemComparisonCategory.None &&
        CategoryFor(candidate, slot) == CategoryFor(equipped, slot) &&
        !equipped.IsCursed &&
        EquipmentCompatibility.CanEquip(player, candidate, slot, out _);
}
