namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Which kind of training a CanBeThrown item requires -- independent of WeaponType/
/// AllowedWeaponTypes, which only govern MELEE equip eligibility. A class can be allowed to
/// throw something it could never wield in melee (a Thief can sling a Spear without being able
/// to fight with one) -- see CharacterClass.AllowedThrowableCategories and
/// GameLoop.HandleFireProjectile's throwables filter.
/// </summary>
public enum ThrowableCategory
{
    /// <summary>No training needed -- a Dart or a Rock, anyone can chuck one. See Items.Dart/Rock.</summary>
    Light,

    /// <summary>A real thrown weapon (knife, axe, spear) -- martial training required. See Items.ThrowingKnife/ThrowingAxe/Spear.</summary>
    Martial,

    /// <summary>Specialized rogue tradecraft -- a Shuriken. See Items.Shuriken.</summary>
    Exotic
}
