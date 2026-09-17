namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>What happens to a CanBeThrown item once its throw resolves -- see GameLoop.HandleFireProjectile. Deliberately excludes "continues through target," which is just Penetration > 0 combined with DropsAtImpactPoint at the final tile, not a fourth distinct behavior.</summary>
public enum ThrownWeaponBehavior
{
    /// <summary>Consumed entirely on throw -- never appears on the ground afterward.</summary>
    Destroyed,

    /// <summary>Lands on the ground at wherever the projectile stopped (a hit creature's tile, or the wall/max-range tile) -- the default for CanBeThrown items, since nothing here prevents a future pickup/recovery feature.</summary>
    DropsAtImpactPoint,

    /// <summary>Reappears directly in the thrower's own inventory once the throw resolves.</summary>
    ReturnsToThrower
}
