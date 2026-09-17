namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

/// <summary>
/// What an ItemStatusEffect entry on a weapon/armor does -- offensive types
/// (Fire/Frost/Poison/Bleed/Shock/Corrode/Stun) proc on a successful hit by
/// the wielder (see ItemEffectApplier); defensive types (Regeneration,
/// Protection, Thorns) are passive while equipped. Elemental resistance is
/// NOT one of these -- see Item.ResistanceModifiers (a ResistanceSet) and
/// the Resistance System spec; this enum used to also carry flat
/// *Resistance values here, replaced entirely by that percentage-based,
/// centralized system.
/// </summary>
public enum ItemEffectType
{
    Fire,
    Frost,
    Poison,
    Bleed,
    Stun,
    Shock,
    Corrode,

    Regeneration,
    Protection,
    Thorns
}
