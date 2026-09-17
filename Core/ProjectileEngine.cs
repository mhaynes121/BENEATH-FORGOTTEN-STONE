using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities;
using BENEATH_FORGOTTEN_STONE.Entities.Projectiles;
using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Core;

/// <summary>
/// One shared move/collide/render loop for every projectile in the game -- physical (arrows,
/// bolts, thrown weapons) and monster-fired ones all travel through Launch; spell projectiles
/// use the separate AnimateFlight (see its own doc comment for why). Nothing here knows or
/// cares whether the source was a Weapon/Spell/Skill/MonsterAbility (see ProjectileSourceType) --
/// that's exactly the "one engine, not one per projectile type" requirement this exists to satisfy.
/// GameLoop.Run's single-threaded turn loop means the Thread.Sleep below genuinely pauses
/// everything else in the game -- nothing can advance while this synchronous call hasn't returned.
/// </summary>
public static class ProjectileEngine
{
    /// <summary>Moves one tile per iteration, resolving collisions as it goes, until it exits the level, hits a wall, exhausts its penetration, or reaches max range. Damage/status effects/messages are written onto the returned outcome's Projectile -- the caller is responsible for queuing Projectile.Messages into its own status-message history.</summary>
    public static ProjectileOutcome Launch(Level level, Player viewer, IReadOnlyList<MessageEntry> statusMessages, ProjectileInstance projectile, Random rng)
    {
        while (projectile.DistanceTraveled < projectile.Definition.Range)
        {
            projectile.X += projectile.Direction.Dx;
            projectile.Y += projectile.Direction.Dy;
            projectile.DistanceTraveled++;

            var blockingRoomObject = level.IsInBounds(projectile.X, projectile.Y) ? level.GetRoomObjectAt(projectile.X, projectile.Y) : null;
            if (!level.IsInBounds(projectile.X, projectile.Y) || !level.IsWalkable(projectile.X, projectile.Y)
                || blockingRoomObject is { BlocksProjectiles: true })
            {
                // Step back onto the last valid tile so a dropped thrown weapon (or a future
                // "landed at the wall" message) reports where the projectile actually rested,
                // not the wall/object tile it never actually entered.
                projectile.X -= projectile.Direction.Dx;
                projectile.Y -= projectile.Direction.Dy;
                string obstacle = blockingRoomObject != null ? $"the {blockingRoomObject.DisplayName}" : "the wall";
                projectile.Messages.Add($"{CombatMessages.WithArticle(projectile.Definition.Name)} hits {obstacle}.");
                return ProjectileOutcome.HitWall(projectile);
            }

            RenderFrame(level, viewer, statusMessages, projectile.Definition, projectile.X, projectile.Y, projectile.Direction);
            Thread.Sleep(ProjectileConfig.AnimationDelayMs);

            var occupant = level.GetActorAt(projectile.X, projectile.Y);
            if (occupant != null && occupant != projectile.Source && !projectile.AlreadyHit.Contains(occupant)
                && IsValidTarget(projectile.Source, occupant))
            {
                ResolveCollision(projectile, occupant, level, rng);
                projectile.AlreadyHit.Add(occupant);

                if (projectile.RemainingPenetration <= 0)
                {
                    return ProjectileOutcome.HitCreature(projectile);
                }
                projectile.RemainingPenetration--;
            }
        }

        projectile.Messages.Add($"{CombatMessages.WithArticle(projectile.Definition.Name)} flies out of range.");
        return ProjectileOutcome.Miss(projectile);
    }

    /// <summary>
    /// Plays the visual flight of a spell projectile from (startX, startY) to (destX, destY)
    /// WITHOUT resolving any collision -- TargetResolver has already committed to the affected
    /// actors before this runs (see SpellCaster.Cast's onTargetsResolved hook), and DamageEffect
    /// applies the actual damage right after this returns. This only draws the travel; it can
    /// never change what the spell hits.
    /// </summary>
    public static void AnimateFlight(Level level, Player viewer, IReadOnlyList<MessageEntry> statusMessages,
        ProjectileDefinition definition, int startX, int startY, (int Dx, int Dy) direction, int destX, int destY)
    {
        if (direction.Dx == 0 && direction.Dy == 0)
        {
            return;
        }

        int x = startX, y = startY;
        int safety = definition.Range + 1;
        while ((x != destX || y != destY) && safety-- > 0)
        {
            x += direction.Dx;
            y += direction.Dy;
            RenderFrame(level, viewer, statusMessages, definition, x, y, direction);
            Thread.Sleep(ProjectileConfig.AnimationDelayMs);
        }
    }

    private static void RenderFrame(Level level, Player viewer, IReadOnlyList<MessageEntry> statusMessages,
        ProjectileDefinition definition, int x, int y, (int Dx, int Dy) direction)
    {
        if (!level.IsInBounds(x, y) || !level.Tiles[x, y].IsVisible)
        {
            return;
        }

        Renderer.Render(level, viewer, statusMessages, (x, y, definition.GlyphFor(direction), definition.Color));
    }

    /// <summary>Data-driven friendly-fire rule: a player-side-sourced projectile only ever hits a non-player-side actor and vice versa -- no monster-vs-monster friendly fire, matching melee combat's existing behavior. "Player-side" now includes the player's own active pet (Pet and Companion System), so a monster's arrow can hit the pet, and the player's own shot never can. CanBeTargeted (already false for NPC/Trader) protects non-combat actors for free; Allegiance.AreFriendly additionally guards a hypothetical same-owner pet-vs-pet case.</summary>
    private static bool IsValidTarget(Actor source, Actor candidate)
    {
        if (!candidate.IsAlive || !candidate.CanBeTargeted || Allegiance.AreFriendly(source, candidate))
        {
            return false;
        }

        return IsPlayerSide(source) != IsPlayerSide(candidate);
    }

    private static bool IsPlayerSide(Actor actor) => actor is Player || (actor is Pet { Owner: Player });

    private static void ResolveCollision(ProjectileInstance projectile, Actor target, Level level, Random rng)
    {
        int resistance = projectile.DamageType == DamageType.Physical ? target.DefensePower : target.MagicResistance;
        int variance = rng.Next(-1, 2);
        int damage = Math.Max(1, projectile.Damage - resistance + variance);

        if (projectile.IsBlessed && target is Monster { CreatureType: CreatureType.Undead })
        {
            damage += projectile.BlessedUndeadDamageBonus;
        }

        damage = FloorDamageCalculator.ApplyFloorMultiplier(level, target, projectile.DamageType, damage);

        int preDamageHealth = target.Health?.Current ?? 0;
        CombatStatsTracker.ApplyDamage(target, damage, projectile.Source);
        target.LastDamageSource = $"{CombatMessages.WithArticle(projectile.Source)}'s {projectile.Definition.Name}";
        target.LastDamageOwner = projectile.Source;

        var severity = CombatMessages.ClassifySeverity(damage, preDamageHealth);
        var result = new AttackResult
        {
            Attacker = projectile.Source,
            Defender = target,
            Hit = true,
            Damage = damage,
            Severity = severity,
            AttackType = projectile.Definition.AttackType
        };

        // Design spec section 15: a shot must not accidentally reveal a monster's identity just
        // because it fired from (or landed on) an unilluminated dark tile -- the "other party" is
        // always whichever combatant isn't the player, regardless of which direction the shot
        // went, mirroring GameLoop.HandleMove/ChaseAI's identical melee-side checks. FormatAttack
        // (Pet and Companion System) falls back to third-party phrasing on its own when neither
        // side is actually the player (a monster's shot landing on the pet).
        bool sourceIsPlayer = projectile.Source is Player;
        Actor otherParty = sourceIsPlayer ? target : projectile.Source;
        bool otherPartyVisible = level.Tiles[otherParty.X, otherParty.Y].IsVisible;
        projectile.Messages.Add(CombatMessages.FormatAttack(result, otherPartyVisible));

        string statusMessage = ItemEffectApplier.ApplyStatusEffects(projectile.Source, target, projectile.StatusEffects, level, rng);
        if (statusMessage != null)
        {
            projectile.Messages.Add(statusMessage);
        }
    }
}
