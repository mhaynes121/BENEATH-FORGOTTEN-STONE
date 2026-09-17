namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Broad weapon category used only for class-equipment restrictions (see
/// CharacterClass.AllowedWeaponTypes, EquipmentCompatibility.IsAllowedForClass)
/// -- separate from AttackType, which drives combat-message verbs, not who's
/// allowed to wield the thing. Scoped to what this game's actual weapon
/// roster contains; add a new value (and give it to whichever classes should
/// use it) when a new category of weapon is actually added, rather than
/// pre-declaring types nothing implements yet.
/// </summary>
public enum WeaponType
{
    Dagger,
    Sword,
    Axe,
    Blunt,
    Wand,

    /// <summary>Fires ammo from Item.RequiredAmmunitionType -- see Core/ProjectileEngine.cs and GameLoop.HandleFireProjectile.</summary>
    Bow,
    Crossbow,

    /// <summary>Fires AmmunitionType.SlingStone -- see Items.Sling/SlingStone.</summary>
    Sling,

    /// <summary>Melee-capable (Pierce) and CanBeThrown -- see Item.CanBeThrown, Item.ThrowableCapability.MeleeOrProjectile.</summary>
    Spear
}
