namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

/// <summary>
/// Scoped to what's actually backed by a real field on Actor today.
/// Extend this when a fuller attributes system (Strength/Dex/etc.) exists
/// -- adding unused enum values ahead of that just creates dead options.
/// </summary>
public enum Stat
{
    PhysicalAttack,
    MagicalAttack,
    Armor,
    MagicResistance,
    MovementSpeed,
    CarryCapacity,

    /// <summary>Folded into Actor.PhysicalAttack's hit-chance formula as a bonus to the DEFENDER'S effective Agility -- Evade.</summary>
    Dodge,

    /// <summary>Folded into Actor.PhysicalAttack's hit-chance formula as a bonus to the ATTACKER'S effective Agility -- Blind lowers the blinded actor's own future accuracy.</summary>
    Accuracy
}
