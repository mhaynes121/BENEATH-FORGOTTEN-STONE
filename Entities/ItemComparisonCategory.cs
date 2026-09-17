namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// The gameplay role two items must share to be meaningfully comparable -- deliberately finer
/// than ItemType and independent of any single EquipmentSlot, since the SAME item can belong to
/// a different category depending on which slot it occupies (a hybrid Spear is a MeleeWeapon in
/// a hand slot but Ammunition in the Ammo/Thrown slot). See ItemComparisonCompatibility.CategoryFor.
/// None means "not classifiable as equipment for comparison purposes" -- never comparable with anything.
/// </summary>
public enum ItemComparisonCategory
{
    None,
    MeleeWeapon,
    RangedWeapon,
    Shield,
    Wand,
    Ring,
    Gloves,
    Wrists,
    Arms,
    BodyArmor,
    Neck,
    Head,
    Legs,
    Feet,
    Ammunition
}
