namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Weight class for worn armor material -- used only for class equipment
/// restrictions (see CharacterClass.AllowedArmorWeights,
/// EquipmentCompatibility.IsAllowedForClass). Applied by material, not
/// slot: cloth/silk/robe are Light, leather/chain/padded are Medium, and
/// iron/plate/steel are Heavy, consistently across Body, Head, Gloves,
/// Wrists, Arms, Legs, and Feet. Deliberately NOT applied to Ring or Neck
/// items (rings, amulets) -- those are jewelry, not armor material, so
/// weight-gating them wouldn't model anything real.
/// </summary>
public enum ArmorWeight
{
    Light,
    Medium,
    Heavy
}
