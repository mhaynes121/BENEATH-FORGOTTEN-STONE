using BENEATH_FORGOTTEN_STONE.Core;
using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities;
using BENEATH_FORGOTTEN_STONE.Entities.Projectiles;
using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Entities.AI;

/// <summary>
/// Casts spells instead of always closing to melee: heals itself when
/// hurt, casts an offensive spell at the player when one is off cooldown
/// and affordable, and falls back to the same chase-and-melee behavior
/// as ChaseAI otherwise. Reuses SpellCaster -- the same engine and Spell
/// definitions the player uses -- so a Spell never needs to know who's
/// casting it.
/// </summary>
public class SpellCasterAI : IAIComponent
{
    private const double LowHealthThreshold = 0.5;

    private readonly ChaseAI meleeFallback = new();

    public string TakeTurn(Actor self, Level level, Random rng)
    {
        var player = level.Actors.OfType<Player>().FirstOrDefault(p => p.IsAlive);
        if (player == null)
        {
            return null;
        }

        // Kick's Silence -- can't cast at all while it's active, so skip straight to the
        // same chase-and-melee fallback used when no spell is ready anyway.
        if (level.TurnNumber < self.SilencedUntilTurn)
        {
            return meleeFallback.TakeTurn(self, level, rng);
        }

        if (self.Health != null && self.Health.Current < self.Health.Max * LowHealthThreshold)
        {
            var healSpell = FindReadySpell(self, level, s => s.Targeting.TargetType == SpellTargetType.Self && HasHealEffect(s));
            if (healSpell != null)
            {
                var context = new SpellCastingContext(self, level, level.TurnNumber, rng);
                var castResult = SpellCaster.Cast(healSpell, context);
                if (castResult.Success)
                {
                    return castResult.Message;
                }
            }
        }

        // Pet and Companion System: engages whichever of {the player, the player's own active
        // pet} is closer -- see HostileTargetSelector's own doc comment.
        var target = HostileTargetSelector.NearestPlayerSideTarget(level, self) ?? player;

        int dx = target.X - self.X;
        int dy = target.Y - self.Y;
        int detectionRadius = DetectionRules.EffectiveRadius(player);
        bool playerInRange = (dx * dx) + (dy * dy) <= detectionRadius * detectionRadius;

        if (playerInRange)
        {
            if (self is Monster monster)
            {
                monster.IsAlerted = true;
            }

            var offensiveSpell = FindReadySpell(self, level, s => s.Targeting.TargetType == SpellTargetType.SingleTarget);
            if (offensiveSpell != null)
            {
                var context = new SpellCastingContext(self, level, level.TurnNumber, rng) { TargetActor = target };

                Action<SpellCastingContext> onTargetsResolved = null;
                if (offensiveSpell.IsProjectile)
                {
                    int aimDx = Math.Sign(target.X - self.X);
                    int aimDy = Math.Sign(target.Y - self.Y);
                    onTargetsResolved = ctx => AnimateSpellProjectile(level, player, offensiveSpell, ctx, aimDx, aimDy);
                }

                var castResult = SpellCaster.Cast(offensiveSpell, context, onTargetsResolved: onTargetsResolved);
                if (castResult.Success)
                {
                    self.LastAttackTurn = level.TurnNumber;
                    return castResult.Message;
                }
            }
        }

        return meleeFallback.TakeTurn(self, level, rng);
    }

    private static Spell FindReadySpell(Actor self, Level level, Func<Spell, bool> matches)
    {
        foreach (var spell in self.KnownSpells)
        {
            if (!matches(spell))
            {
                continue;
            }
            if (self.Mana.Current < spell.Casting.ManaCost)
            {
                continue;
            }
            if (self.SpellCooldowns.TryGetValue(spell, out int readyTurn) && level.TurnNumber < readyTurn)
            {
                continue;
            }
            return spell;
        }
        return null;
    }

    /// <summary>
    /// Mirrors GameLoop.AnimateSpellProjectile for a monster's own offensive cast -- purely
    /// visual, since TargetResolver has already committed to the hit by the time this runs.
    /// Renders with an empty status-message list rather than threading GameLoop's private
    /// history through IAIComponent -- an acceptable cosmetic simplification: the player's
    /// last message just isn't visible for the few animation frames, and reappears (along
    /// with this cast's own message) the next time anything actually renders.
    /// </summary>
    private static void AnimateSpellProjectile(Level level, Player viewer, Spell spell, SpellCastingContext context, int dx, int dy)
    {
        var damageEffect = spell.Effects.OfType<DamageEffect>().FirstOrDefault();
        if (damageEffect == null)
        {
            return;
        }

        var (glyph, color) = ProjectileVisuals.ForDamageType(damageEffect.DamageType);
        var definition = new ProjectileDefinition(spell.Name, spell.Targeting.Range, penetration: 0, glyph, color, AttackType.Hit);

        (int X, int Y) destination = context.TargetTile ?? (context.TargetActor?.X ?? context.Caster.X, context.TargetActor?.Y ?? context.Caster.Y);
        ProjectileEngine.AnimateFlight(level, viewer, Array.Empty<MessageEntry>(), definition, context.Caster.X, context.Caster.Y, (dx, dy), destination.X, destination.Y);
    }

    private static bool HasHealEffect(Spell spell)
    {
        foreach (var effect in spell.Effects)
        {
            if (effect is HealEffect)
            {
                return true;
            }
        }
        return false;
    }
}
