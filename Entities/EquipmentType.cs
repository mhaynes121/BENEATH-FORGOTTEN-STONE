namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// What kind of equipment an Item is -- separate from ItemType (which
/// distinguishes Consumable/Weapon/Armor/Wand for interaction purposes,
/// e.g. "drink it" vs "equip it") and from EquipmentSlot (the actual
/// physical location on the character). None = not equippable.
/// </summary>
public enum EquipmentType
{
    None,
    Hand,
    Ring,
    Gloves,
    Wrists,
    Arms,
    Body,
    Neck,
    Head,
    Legs,
    Feet,

    /// <summary>Bow/Crossbow/Sling -- EquipmentSlot.RangedWeapon only, never Hand. See EquipmentCompatibility.</summary>
    Ranged,

    /// <summary>Ammunition and dedicated/hybrid throwables -- EquipmentSlot.Ammunition only. A hybrid melee-or-thrown item (see Item.ThrowableCapability) still uses the ordinary Hand type for its melee compatibility; AmmoThrown is only for items whose SOLE non-inventory home is the Ammo/Thrown slot.</summary>
    AmmoThrown
}
