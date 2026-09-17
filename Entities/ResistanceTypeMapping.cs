using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// The one centralized answer to "which resistance (if any) mitigates this incoming damage" --
/// every resistance-aware call site (ResistanceCalculator, ItemEffectApplier's status-chance/
/// duration adjustment, StatusEffect) goes through this instead of re-deriving the mapping.
/// Lightning maps to Shock and Arcane maps to Magic -- this game's existing naming already
/// treats them as the same concepts (see FloorTypeDefinition's own doc comment on
/// ShockDamageMultiplier). Physical, Shadow, Holy, Earth, and Air have no resistance mapping at
/// all (null) -- Physical by explicit design (Armor/Defense already covers it), the rest simply
/// have no resistance type defined yet.
/// </summary>
public static class ResistanceTypeMapping
{
    public static ResistanceType? ForDamageType(DamageType damageType) => damageType switch
    {
        DamageType.Fire => ResistanceType.Fire,
        DamageType.Water => ResistanceType.Water,
        DamageType.Ice => ResistanceType.Ice,
        DamageType.Lightning => ResistanceType.Shock,
        DamageType.Poison => ResistanceType.Poison,
        DamageType.Arcane => ResistanceType.Magic,
        _ => null
    };
}
