using BENEATH_FORGOTTEN_STONE.Entities;
using BENEATH_FORGOTTEN_STONE.Entities.Components;
using BENEATH_FORGOTTEN_STONE.Entities.Projectiles;

namespace BENEATH_FORGOTTEN_STONE.Core;

/// <summary>
/// Whether a fired/thrown physical projectile breaks on impact -- checked by ItemLandingResolver
/// AFTER environmental destruction (fire/lava always wins, see ItemDestructionRules) and BEFORE
/// the permanent-loss/misplaced roll (see ItemLossRules). Deliberately its own outcome
/// (ItemLandingOutcome.Broken), never conflated with PermanentlyLost internally even though both
/// remove the item the same way -- see the Misplaced Items design doc's explicit "don't interpret
/// broken and permanently lost as the same outcome" instruction. Mirrors ItemLossRules' own shape
/// (a pure chance getter, separate from the actual roll) so the math is testable without a seeded
/// Random.
/// </summary>
public static class ItemBreakageRules
{
    /// <summary>A returning magical weapon is protected from breakage the same way it's protected from permanent loss (see ItemLossRules.CanBeLost) -- it reappears in the thrower's hand, never landing at all, so breakage never applies to it in the first place.</summary>
    public static bool CanBreak(Item item) => item.ThrownWeaponBehavior != ThrownWeaponBehavior.ReturnsToThrower;

    /// <summary>Which of an item's three configured break chances applies to how this landing actually happened -- HitWall (usually highest), HitCreature (usually middle), or Miss/ran-out-of-range (an ordinary landing, usually lowest/zero). 0 for a protected item regardless of its configured values.</summary>
    public static double GetBreakChance(Item item, ProjectileTerminationReason reason)
    {
        if (!CanBreak(item))
        {
            return 0.0;
        }

        return reason switch
        {
            ProjectileTerminationReason.HitWall => item.BreakChanceOnWallImpact,
            ProjectileTerminationReason.HitCreature => item.BreakChanceOnCreatureHit,
            _ => item.BreakChanceOnOrdinaryLanding
        };
    }

    /// <summary>Rolls whether this landing breaks the item -- a pure roll against GetBreakChance, no placement/Adventure-Record side effects (ItemLandingResolver owns those, same division of labor ItemLossRules already uses).</summary>
    public static (bool Broke, string Message) Resolve(Item item, ProjectileTerminationReason reason, Random rng)
    {
        double chance = GetBreakChance(item, reason);
        if (chance <= 0.0 || rng.NextDouble() >= chance)
        {
            return (false, null);
        }

        string message = reason switch
        {
            ProjectileTerminationReason.HitWall => $"The {item.DisplayName} breaks against the wall.",
            ProjectileTerminationReason.HitCreature => $"The {item.DisplayName} shatters on impact.",
            _ => $"The {item.DisplayName} snaps on landing."
        };
        return (true, message);
    }
}
