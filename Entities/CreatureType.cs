namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// What KIND of creature this is -- separate from Size (how physically
/// large) and from combat stats. Exists only to hand out sensible default
/// capabilities (see CreatureCapabilities) so the rest of the codebase
/// doesn't have to branch on type directly -- prefer checking
/// Monster.CanEquipItems/CanCarryItems/CanUseItems, not CreatureType,
/// wherever a behavior decision is needed (see LootGenerator).
///
/// Deliberately shallow for now -- only what the current capability system
/// needs. More values (Dragon, Demon, Elemental, Construct, Beast,
/// Insect, Plant, Divine, ...) can be added later without touching the
/// existing members or any code that switches on them, since
/// CreatureCapabilities.GetDefaults already falls back to the conservative
/// Other defaults for any type it doesn't explicitly special-case -- Undead
/// relies on exactly that fallback too.
///
/// Undead exists specifically so a blessed weapon's bonus damage (see
/// Actor.PhysicalAttack, Item.IsBlessed/BlessedUndeadDamageBonus) has
/// something concrete to check against instead of a raw name/Type check.
///
/// Not a stand-in for faction/allegiance -- a Goblin and a Human Guard can
/// both be Humanoid while being hostile/friendly respectively; that's a
/// separate concern this enum deliberately doesn't model.
/// </summary>
public enum CreatureType
{
    Humanoid,
    Giant,
    Undead,
    God,
    Other,
    Animal,
    /// <summary>
    ///  Some creature types for when we add a Ranger class and have animal specific spells / skills
    ///  These should replace the generic 'Animal' creaturetype once Rangers are implemented. 
    /// </summary>
    Mammal,
    Reptile,
    Amphibian
}
