namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Physical size tier for monsters and items. A monster can only drop loot
/// whose Size is less than or equal to its own (declaration order below is
/// the size order the comparison relies on). Deliberately its own type --
/// not merged into ItemType/EquipmentType/EquipmentCategory, which classify
/// different things entirely.
/// </summary>
public enum Size
{
    Small,
    Medium,
    Large
}
