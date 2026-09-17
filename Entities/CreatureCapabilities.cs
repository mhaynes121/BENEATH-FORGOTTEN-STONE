namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Default CanCarryItems/CanEquipItems/CanUseItems per CreatureType,
/// centralized so a new CreatureType's defaults live in exactly one place
/// instead of being repeated at every Monster.Archetype declaration.
/// These are only DEFAULTS -- an individual archetype can still override
/// any of the three (see Monster.Archetype's CanCarryItemsOverride/
/// CanEquipItemsOverride/CanUseItemsOverride and Monster.ResolveCapabilities),
/// so an unusual animal or an unusually primitive humanoid isn't permanently
/// locked to its type's usual behavior.
/// </summary>
public static class CreatureCapabilities
{
    public static (bool CanCarryItems, bool CanEquipItems, bool CanUseItems) GetDefaults(CreatureType type) => type switch
    {
        // Humanoids carry, equip, and use items -- this is the primary reason
        // CreatureType exists (see LootGenerator, which gates enemy equipment
        // drops on CanEquipItems instead of ever checking CreatureType directly).
        CreatureType.Humanoid => (CanCarryItems: true, CanEquipItems: true, CanUseItems: true),

        // Animal and Other (conservative until a more specific future type --
        // Undead, Dragon, Demon, Elemental, Construct, ... -- gives it a
        // reason to differ) both default to no item capabilities at all.
        _ => (CanCarryItems: false, CanEquipItems: false, CanUseItems: false)
    };
}
