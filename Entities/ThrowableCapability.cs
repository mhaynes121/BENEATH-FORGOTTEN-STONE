namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// What roles an item can fill -- distinct from EquipmentType (which slot(s) it occupies) and
/// CanBeThrown/ThrownWeaponBehavior (what happens once it's actually thrown/fired). An ordinary
/// melee weapon or piece of armor is MeleeOnly; ammunition and dedicated throwables (Arrow, Dart,
/// Throwing Knife, ...) are ProjectileOnly; a hybrid spear is MeleeOrProjectile. See
/// EquipmentCompatibility.GetCompatibleSlots(Item), which uses this to decide whether Ammunition
/// belongs in a MeleeOrProjectile item's compatible-slot list at all, and in what order.
/// </summary>
public enum ThrowableCapability
{
    MeleeOnly,
    ProjectileOnly,
    MeleeOrProjectile
}
