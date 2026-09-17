using BENEATH_FORGOTTEN_STONE.Entities.Components;
using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Entities.Projectiles;

/// <summary>
/// Builds a ProjectileInstance from a fired weapon+ammo pair or a thrown item -- the shared
/// logic GameLoop.HandleFireProjectile (player) and RangedAttackAI (monster) both need, so a
/// fired shot behaves identically no matter who fired it (same damage formula, same elemental/
/// blessed handling as melee -- see Actor.PhysicalAttack's own shape). Damage type is always
/// Physical for a physical projectile; elemental flavor comes from the ammo/item's own
/// StatusEffects (a chance-based proc, mirroring how a Flaming Longsword already works in
/// melee), not from converting the base hit to a different DamageType.
/// </summary>
public static class ProjectileFactory
{
    /// <summary>Conservative placeholder penalty for firing a plain Rock from a Sling instead of a proper Sling Stone (Item.AmmunitionType.SlingStone matches both) -- reduces effective range rather than accuracy, since no ranged hit-chance roll exists yet (deferred to a future accuracy-centralization pass). Not a balancing effort, just enough to make a real Sling Stone worth carrying.</summary>
    private const int RockInSlingRangePenalty = 2;

    public static ProjectileInstance ForWeaponAndAmmo(Actor shooter, Item weapon, Item ammo, int startX, int startY, (int Dx, int Dy) direction)
    {
        // A proper Sling Stone (dedicated ammo, never CanBeThrown on its own) gets full range;
        // a Rock pressed into service as sling ammo (dual-natured -- CanBeThrown AND tagged
        // SlingStone) takes the penalty for lacking a Sling Stone's purpose-made shape.
        int range = weapon.ProjectileRange;
        if (weapon.WeaponType == WeaponType.Sling && ammo.CanBeThrown)
        {
            range = Math.Max(1, range - RockInSlingRangePenalty);
        }

        var definition = new ProjectileDefinition(
            ammo.Name.ToLowerInvariant(), range, penetration: 0,
            displayCharacter: null, color: ConsoleColor.Gray, attackType: weapon.AttackType ?? AttackType.Pierce);

        return new ProjectileInstance(definition, shooter, ProjectileSourceType.Weapon, startX, startY, direction)
        {
            // weapon.PhysicalAttackBonus is added explicitly here -- Player/Monster's own derived-stat
            // recalculation deliberately EXCLUDES RangedWeapon/Ammunition from BasePhysicalAttackPower
            // (see Player.RecalculateDerivedStats/Monster.ApplyEquipmentBonus) so a launcher never
            // buffs melee. This is the one place its own damage modifier actually applies, matching
            // the design doc's formula: shooter base + ammunition damage + launcher damage modifier.
            Damage = shooter.BasePhysicalAttackPower + weapon.PhysicalAttackBonus + ammo.PhysicalAttackBonus,
            DamageType = DamageType.Physical,
            StatusEffects = ammo.StatusEffects,
            IsBlessed = ammo.IsBlessed || weapon.IsBlessed,
            BlessedUndeadDamageBonus = ammo.BlessedUndeadDamageBonus + weapon.BlessedUndeadDamageBonus,
            PayloadItem = ammo
        };
    }

    public static ProjectileInstance ForThrownItem(Actor thrower, Item item, int startX, int startY, (int Dx, int Dy) direction)
    {
        var definition = new ProjectileDefinition(
            item.Name.ToLowerInvariant(), item.ProjectileRange, penetration: 0,
            displayCharacter: null, color: ConsoleColor.Gray, attackType: item.AttackType ?? AttackType.Pierce);

        return new ProjectileInstance(definition, thrower, ProjectileSourceType.Weapon, startX, startY, direction)
        {
            Damage = thrower.BasePhysicalAttackPower + item.PhysicalAttackBonus,
            DamageType = DamageType.Physical,
            StatusEffects = item.StatusEffects,
            IsBlessed = item.IsBlessed,
            BlessedUndeadDamageBonus = item.BlessedUndeadDamageBonus,
            PayloadItem = item,
            ThrownWeaponBehavior = item.ThrownWeaponBehavior
        };
    }
}
