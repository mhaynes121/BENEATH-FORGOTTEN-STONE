namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// A finer-grained classification used ONLY for equipment combination
/// rules (e.g. "can't wear two weapons") -- deliberately separate from
/// EquipmentType, which drives slot compatibility (section 13/16 of the
/// equipment spec: slot compatibility and combination compatibility are
/// different questions and must not be merged into one system). None
/// means "no conflict rules apply" -- true for rings, armor, and generic
/// hand items (e.g. a torch) alike, so it doesn't need its own value.
/// </summary>
public enum EquipmentCategory
{
    None,
    Weapon,
    Shield,
    Wand
}
