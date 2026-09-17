using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Centralized floor-elemental-interaction check -- the target's current tile can amplify or
/// dampen an incoming attack based on the attack's own DamageType (a Fire attack hits harder
/// against someone standing in Fire/Lava, weaker against someone in Water; a Lightning attack
/// hits harder in Water; and so on -- see FloorTypeDefinition.MultiplierFor). Called from every
/// damage-application site that already carries an explicit DamageType (spell damage, on-hit
/// elemental item procs, damage-over-time ticks, projectile damage); melee weapon attacks have
/// no DamageType of their own today (implicitly Physical), which the multiplier table already
/// always resolves to 1.0 anyway, so wiring it in there would be a pure no-op.
///
/// Deliberately NOT applied to EnvironmentalFloorEffects' own Fire/Lava tick damage -- that
/// damage IS the floor reacting to itself, and running the FLOOR'S OWN multiplier back through
/// this would double-count it (EnvironmentalFloorEffects calls ResistanceCalculator.ApplyResistance
/// directly instead, skipping this floor-multiplier step but still getting resistance).
///
/// Resistance (Resistance System spec) is applied AFTER the floor's own multiplier above,
/// sequentially -- this used to also fold in the tile-attuned monster system's own special
/// elemental-terrain multiplier here, but that concept is now represented entirely as an
/// environmental resistance modifier instead (see Monster.GetEffectiveResistance), per that
/// spec's own section 37 ("do not leave both representations active"). For a target whose
/// PreferredFloorType tile also carries its own elemental multiplier (e.g. a Water-attuned
/// monster standing on Water, where the floor itself already halves incoming Fire), the floor
/// multiplier and the monster's resistance deliberately still compound rather than being
/// deduplicated: standing in your own element while also being innately suited to it is meant
/// to read as doubly protected, not the same protection counted once.
/// </summary>
public static class FloorDamageCalculator
{
    public static int ApplyFloorMultiplier(Level level, Actor target, DamageType damageType, int damage)
    {
        if (damage <= 0 || !level.IsInBounds(target.X, target.Y))
        {
            return damage;
        }

        double multiplier = FloorTypeCatalog.Get(level.Tiles[target.X, target.Y].FloorType).MultiplierFor(damageType);
        int afterFloor = multiplier == 1.0
            ? damage
            // Same floor-of-1 guarantee resistance itself uses below -- a dampening multiplier
            // (e.g. 0.5x) should never accidentally zero out a hit that otherwise landed.
            : Math.Max(1, (int)Math.Round(damage * multiplier));

        return ResistanceCalculator.ApplyResistance(level, target, damageType, afterFloor);
    }
}
