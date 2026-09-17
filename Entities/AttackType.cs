namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// How an attack is physically delivered -- shared by monsters (Bite/Sting/Hit,
/// their innate attack) and weapons (Crush/Slash/Pierce, on top of Hit for an
/// unarmed player). Purely descriptive: it drives the combat-message verb via
/// CombatMessages.AttackWord/AttackVerb and nothing else -- see Actor.AttackType,
/// Item.AttackType. Adding a new type (Claw, Breath, ...) is just a new enum
/// value plus a verb in CombatMessages; no other code needs to change.
/// </summary>
public enum AttackType
{
    Bite,
    Sting,
    Hit,
    Crush,
    Slash,
    Pierce
}
