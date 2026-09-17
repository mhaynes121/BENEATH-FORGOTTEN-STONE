namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// The six resistance slots a character/monster/item can have a value for -- deliberately its
/// own small enum rather than reusing DamageType directly: DamageType also has Physical/Arcane/
/// Shadow/Holy/Earth/Air, several of which either already have their own defense mechanism
/// (Physical uses Armor/Defense, never resistance -- see the Resistance System spec's own
/// section 1) or have no resistance concept at all yet. See ResistanceTypeMapping for how an
/// incoming DamageType maps (or doesn't) onto one of these.
/// </summary>
public enum ResistanceType
{
    Fire,
    Water,
    Ice,
    Shock,
    Poison,
    Magic
}
