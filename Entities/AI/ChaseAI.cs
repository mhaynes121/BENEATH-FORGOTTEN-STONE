using BENEATH_FORGOTTEN_STONE.Core;
using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities;
using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Entities.AI;

/// <summary>
/// Minimal chase-and-melee AI: if the player is within detection range, step toward them (with
/// simple wall-sliding), attacking instead of moving once adjacent. Movement is a cheap greedy
/// step for the common case, with a real shortest-path fallback (see MoveTowardTarget) for the
/// rarer case where something -- a boulder, shrine, statue, stationary trader, or a re-entrant
/// wall corner -- sits directly in the way, so an alerted/chasing monster routes around an
/// obstacle instead of stalling in place as if it had lost track of its target.
/// </summary>
public class ChaseAI : IAIComponent
{
    /// <summary>Matches SpellCasterAI's own self-heal threshold, kept as a separate constant since the two classes don't share a base type.</summary>
    private const double LowHealthThreshold = 0.5;

    public virtual string TakeTurn(Actor self, Level level, Random rng)
    {
        // Checked before anything else, and regardless of whether the player has even
        // been spotted yet -- mirrors SpellCasterAI's own self-heal check, which is
        // unconditional in the same way. SpellCasterAI falls back to this method when it
        // doesn't cast a heal/offensive spell that turn, so this also covers spellcasters.
        string itemMessage = TryUseHealingItem(self);
        if (itemMessage != null)
        {
            return itemMessage;
        }

        var player = level.Actors.OfType<Player>().FirstOrDefault(p => p.IsAlive);
        if (player == null)
        {
            return null;
        }

        // Pet and Companion System: engages whichever of {the player, the player's own active
        // pet} is closer -- see HostileTargetSelector's own doc comment. Falls back to the
        // player when no pet exists, so every pre-existing scenario is unaffected.
        var target = HostileTargetSelector.NearestPlayerSideTarget(level, self) ?? player;

        int dx = target.X - self.X;
        int dy = target.Y - self.Y;
        int detectionRadius = DetectionRules.EffectiveRadius(player);

        if ((dx * dx) + (dy * dy) > detectionRadius * detectionRadius)
        {
            return null;
        }

        // Noticing the player at all is the accepted stand-in for "has seen you" -- see
        // Monster.IsAlerted, checked by Backstab/Shadowstep since this game has no facing.
        if (self is Monster monster)
        {
            monster.IsAlerted = true;
        }

        if (Math.Abs(dx) <= 1 && Math.Abs(dy) <= 1)
        {
            self.LastAttackTurn = level.TurnNumber;
            var result = self.PhysicalAttack(target, rng);
            // Design spec section 15: an attack from an unilluminated dark tile must not
            // accidentally reveal the attacker's identity just because it swung. FormatAttack
            // (Pet and Companion System) falls back to third-party phrasing on its own when the
            // target is the pet rather than the player.
            bool attackerVisible = level.Tiles[self.X, self.Y].IsVisible;
            string message = CombatMessages.FormatAttack(result, otherPartyVisible: attackerVisible);

            string itemEffectMessage = ItemEffectApplier.ApplyOnHitEffects(result, level, rng);
            if (itemEffectMessage != null)
            {
                message += " " + itemEffectMessage;
            }

            // Riposte: a free counter-attack whenever a monster's swing misses the player.
            // This engine has no separate dodge outcome distinct from a plain miss, so
            // Riposte fires on any miss -- the same simplification already accepted for
            // Recklessness's crit reinterpretation. Only the player itself knows Riposte, so
            // this never fires when the pet was the one missed.
            if (!result.Hit && target is Player riposter && riposter.HasSkill(SkillCatalog.Riposte))
            {
                riposter.LastAttackTurn = level.TurnNumber;
                var riposte = riposter.PhysicalAttack(self, rng);
                message += " " + CombatMessages.Format(riposte, attackerIsPlayer: true, otherPartyVisible: attackerVisible);
            }

            return message;
        }

        MoveTowardTarget(self, level, target.X, target.Y);
        return null;
    }

    /// <summary>
    /// Plain greedy step toward (targetX, targetY) with basic wall-sliding (try the diagonal,
    /// then each axis alone) -- cheap, and correct for the overwhelming majority of turns where
    /// nothing sits directly in the way. Only when all three of those attempts fail does this
    /// fall back to a real shortest-path search (TerrainPathfinder, uniform cost -- no floor
    /// preference) to route around whatever's actually blocking the direct line: a boulder,
    /// shrine, statue, stationary trader, or a re-entrant wall corner. Before this fallback
    /// existed, a monster in that situation would simply stall in place every turn, looking like
    /// it had lost track of an alerted/chasing target instead of working its way around the
    /// obstacle. Virtual so FloorAttunedAI can substitute a cost-weighted step for a floor-attuned
    /// monster while every ordinary monster keeps this exact behavior (including the fallback).
    /// Safe Monster Spawning and Hazard-Aware Movement: every candidate step (greedy or
    /// pathfound) must pass ActorTerrainSafety -- if every route to the target is hazardous, this
    /// simply does nothing this turn rather than ever accepting a damaging tile as a last resort.
    /// </summary>
    protected virtual void MoveTowardTarget(Actor self, Level level, int targetX, int targetY)
    {
        int stepX = Math.Sign(targetX - self.X);
        int stepY = Math.Sign(targetY - self.Y);

        if (TryMove(self, level, self.X + stepX, self.Y + stepY))
        {
            return;
        }
        if (TryMove(self, level, self.X + stepX, self.Y))
        {
            return;
        }
        if (TryMove(self, level, self.X, self.Y + stepY))
        {
            return;
        }

        var step = TerrainPathfinder.FindNextStep(level, (self.X, self.Y), (targetX, targetY), mover: self);
        if (step.HasValue)
        {
            TryMove(self, level, step.Value.X, step.Value.Y);
        }
    }

    /// <summary>
    /// A monster capable of using items (CanUseItems -- see CreatureCapabilities) drinks a
    /// carried healing potion once hurt, instead of only ever using items as death loot.
    /// The Player never reaches this path (driven by input, not an IAIComponent). Only
    /// Consumable items with HealthRestore count as "a healing item" for now -- wands/
    /// scrolls/spellbooks are a separate interaction model (cast/read) this simple check
    /// doesn't attempt to replicate for monster AI.
    /// </summary>
    private static string TryUseHealingItem(Actor self)
    {
        if (self is not Monster monster || !monster.CanUseItems)
        {
            return null;
        }
        if (self.Health == null || self.Health.Current >= self.Health.Max * LowHealthThreshold)
        {
            return null;
        }

        var potion = self.Inventory.Items.FirstOrDefault(i => i.Type == ItemType.Consumable && i.HealthRestore > 0);
        if (potion == null)
        {
            return null;
        }

        int missing = self.Health.Max - self.Health.Current;
        self.Health.Heal(potion.HealthRestore);
        if (potion.ManaRestore > 0)
        {
            self.Mana.Restore(potion.ManaRestore);
        }
        self.Inventory.RemoveItem(potion);

        var severity = CombatMessages.ClassifyHealSeverity(Math.Min(potion.HealthRestore, missing), missing);
        return $"{monster.DisplayName} drinks a {potion.DisplayName}, healing for a {CombatMessages.SeverityWord(severity)} amount.";
    }

    /// <summary>Internal (not private) so FloorAttunedAI's own TryStepOnto can reuse the exact same physical-plus-hazard gate rather than re-deriving it.</summary>
    internal static bool TryMove(Actor self, Level level, int x, int y)
    {
        // Monsters never open/bash/key a Door themselves -- a closed one is simply
        // impassable to them, same as a wall, until the player gets it open. Same for a
        // movement-blocking RoomObject -- monsters never push objects out of their own way.
        // Safe Monster Spawning and Hazard-Aware Movement: CanActorOccupy also refuses a tile
        // that would damage `self` (Fire/Lava, off-preferred-terrain attrition) -- never merely
        // expensive, fully impassable for voluntary movement.
        if (!ActorTerrainSafety.CanActorOccupy(level, self, x, y))
        {
            return false;
        }
        self.MoveTo(x, y);
        return true;
    }
}
