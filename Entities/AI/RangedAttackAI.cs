using BENEATH_FORGOTTEN_STONE.Core;
using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities.Components;
using BENEATH_FORGOTTEN_STONE.Entities.Projectiles;
using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Entities.AI;

/// <summary>
/// Fires an equipped Bow/Crossbow (with matching ammo) at the player when they're detected,
/// aligned on one of the 8 fireable directions, in range, and in line of sight -- mirrors
/// SpellCasterAI's exact detect/range/LOS/fallback shape, just for a physical ranged weapon
/// instead of a spell, and reuses Core.ProjectileEngine.Launch, the exact same engine the
/// player's own fired shots go through. "Aligned" mirrors the same 8-directional aiming
/// constraint the player is under (see GameLoop.HandleFireProjectile) -- when the player isn't
/// on a clean line, this falls back to chasing/melee instead, same as when no shot is ready.
/// </summary>
public class RangedAttackAI : IAIComponent
{
    private readonly ChaseAI meleeFallback = new();

    public string TakeTurn(Actor self, Level level, Random rng)
    {
        string fireResult = TryFireAtPlayer(self, level, rng);
        return fireResult != null ? fireResult : meleeFallback.TakeTurn(self, level, rng);
    }

    /// <summary>
    /// Just the fire-a-shot decision, with no melee/chase fallback of its own -- extracted so
    /// FrightenedBehavior can ask "can this monster fire from where it's currently standing"
    /// without risking falling through to meleeFallback's own TakeTurn, which would walk the
    /// actor toward the player (exactly what Frightened forbids). Returns null when no shot is
    /// currently possible at all (no weapon/ammo, out of range, unaligned, no line of sight, or
    /// no living player) -- TakeTurn's own melee fallback covers that case; a Frightened caller
    /// instead treats null as "cannot act this turn." A shot that WAS fired but produced no
    /// projectile message returns "" (not null), so TakeTurn never mistakenly also runs
    /// meleeFallback for a turn that already fired.
    /// </summary>
    public string TryFireAtPlayer(Actor self, Level level, Random rng)
    {
        var player = level.Actors.OfType<Player>().FirstOrDefault(p => p.IsAlive);
        if (player == null)
        {
            return null;
        }

        // Pet and Companion System: aims at whichever of {the player, the player's own active
        // pet} is closer -- see HostileTargetSelector's own doc comment. ProjectileEngine.Launch
        // still resolves the actual hit generically against whatever occupies each tile crossed,
        // so this only decides aim direction/alignment/range/line-of-sight.
        var target = HostileTargetSelector.NearestPlayerSideTarget(level, self) ?? player;

        int dx = target.X - self.X;
        int dy = target.Y - self.Y;
        int chebyshevDistance = Math.Max(Math.Abs(dx), Math.Abs(dy));
        bool aligned = dx == 0 || dy == 0 || Math.Abs(dx) == Math.Abs(dy);

        // RangedAttackAI is only ever assigned to a Monster (see Monster.CreateRandom) -- EquipmentComponent
        // lives on Player/Monster individually, not on the shared Actor base, so this cast is the
        // same pattern already used just below for IsAlerted.
        var equipment = (self as Monster)?.Equipment;
        var weapon = equipment?.Get(EquipmentSlot.RangedWeapon);
        var ammo = equipment?.Get(EquipmentSlot.Ammunition);
        bool ammoCompatible = weapon != null && ammo != null && ammo.AmmunitionType == weapon.RequiredAmmunitionType;
        if (!ammoCompatible)
        {
            ammo = null;
        }

        int detectionRadius = DetectionRules.EffectiveRadius(player);
        bool playerDetected = (dx * dx) + (dy * dy) <= detectionRadius * detectionRadius;

        if (weapon != null && ammo != null && playerDetected && aligned && chebyshevDistance > 1
            && chebyshevDistance <= weapon.ProjectileRange
            && TargetResolver.HasLineOfSight(level, (self.X, self.Y), (target.X, target.Y)))
        {
            if (self is Monster monster)
            {
                monster.IsAlerted = true;
            }

            self.LastAttackTurn = level.TurnNumber;
            var direction = (Math.Sign(dx), Math.Sign(dy));

            // Not routed through ItemLandingResolver -- that pipeline's Adventure Record
            // bookkeeping (permanent loss/misplaced/broken counters) belongs to the PLAYER's own
            // run, and crediting it for a monster's own arrow would be attributing the wrong
            // actor's item. A monster's shot is consumed and simply gone, same as before this
            // feature (see the design doc's section 19, which scopes monster ranged combat to
            // slot structure + firing behavior, not recovery).
            var projectile = ProjectileFactory.ForWeaponAndAmmo(self, weapon, ammo, self.X, self.Y, direction);
            var outcome = ProjectileEngine.Launch(level, player, Array.Empty<MessageEntry>(), projectile, rng);
            ConsumeAmmo(equipment, ammo);

            return outcome.Projectile.Messages.Count > 0 ? string.Join(" ", outcome.Projectile.Messages) : "";
        }

        return null;
    }

    /// <summary>Decrements the readied stack and clears EquipmentSlot.Ammunition once exhausted -- the Monster-side equivalent of ItemStacking.ConsumeOneFromEquippedSlot, which is typed to Player. No stat-bonus removal needed: RangedWeapon/Ammunition never contribute one in the first place (see Monster.ApplyEquipmentBonus's own exclusion).</summary>
    private static void ConsumeAmmo(EquipmentComponent equipment, Item ammo)
    {
        int remaining = (ammo.Charges ?? 1) - 1;
        if (remaining <= 0)
        {
            equipment.Unequip(EquipmentSlot.Ammunition);
        }
        else
        {
            ammo.Charges = remaining;
        }
    }
}
