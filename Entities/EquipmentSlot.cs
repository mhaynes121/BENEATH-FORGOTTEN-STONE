namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// A physical location on the character. Distinct from EquipmentType,
/// which describes what kind of equipment an Item is -- one EquipmentType
/// (Hand, Ring) can be compatible with more than one slot; see
/// EquipmentCompatibility.
/// </summary>
public enum EquipmentSlot
{
    PrimaryHand,
    OffHand,
    PrimaryRing,
    OffHandRing,
    Gloves,
    Wrists,
    Arms,
    Body,
    Neck,
    Head,
    Legs,
    Feet,

    /// <summary>A Bow/Crossbow/Sling -- see EquipmentType.Ranged. Deliberately separate from PrimaryHand/OffHand so a launcher never contributes to melee (Player.AttackType only ever reads the hand slots) and never competes with a held weapon for hand space.</summary>
    RangedWeapon,

    /// <summary>The "readied" ammo-or-throwable slot ("Ammo / Thrown") -- see EquipmentType.AmmoThrown. Unlike every other slot, this one can hold a STACK (Item.Charges > 1) rather than exactly one unit; see ItemStacking.ConsumeOneFromEquippedSlot.</summary>
    Ammunition
}
