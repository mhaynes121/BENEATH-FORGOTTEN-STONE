using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities;

namespace BENEATH_FORGOTTEN_STONE.Entities.AI;

/// <summary>
/// New Priest Skill Progression (Turn Undead): governs a Frightened actor's entire turn --
/// called from GameLoop.ResolveNonPlayerTurn INSTEAD of the actor's own AI.TakeTurn, the same
/// dispatch-level short-circuit Stun/Prone already use. Only the player can currently cause
/// Frightened (Turn Undead is Priest-only), so "the source" is resolved as the nearest living
/// Player rather than a stored per-actor reference -- see Actor.FrightenedUntilTurn's own doc
/// comment for why that's a safe simplification in this single-player game.
/// </summary>
public static class FrightenedBehavior
{
    public static string TakeTurn(Actor self, Level level, Random rng)
    {
        var source = level.Actors.OfType<Player>().FirstOrDefault(p => p.IsAlive);
        if (source == null)
        {
            return null;
        }

        bool adjacentToSource = Math.Max(Math.Abs(self.X - source.X), Math.Abs(self.Y - source.Y)) <= 1;
        if (adjacentToSource)
        {
            // "May defend itself or attack an adjacent threat, but must not advance" -- an
            // in-place attack never advances, so the actor's own normal AI is safe to run
            // completely unmodified here: ChaseAI/RangedAttackAI/SpellCasterAI all attack (or
            // fall back to ChaseAI's attack) without moving once already adjacent to the player.
            return self.AI?.TakeTurn(self, level, rng);
        }

        var fleeStep = FindFleeStep(self, level, source);
        if (fleeStep.HasValue)
        {
            self.MoveTo(fleeStep.Value.X, fleeStep.Value.Y);
            return $"{CombatMessages.Label(self, capitalized: true)} flees in terror!";
        }

        // Cannot retreat any further (boxed in by walls/other actors/room objects) --
        // "retreat before resuming ranged attacks" implies retreat is always tried first; only
        // once it's genuinely impossible does a ranged attacker fall back to firing from where it
        // stands. Deliberately never falls back to ChaseAI's own TakeTurn here (unlike
        // RangedAttackAI's own TakeTurn) -- that would walk the actor toward the player, exactly
        // what Frightened forbids.
        if (self.AI is RangedAttackAI rangedAi)
        {
            string fireResult = rangedAi.TryFireAtPlayer(self, level, rng);
            if (fireResult != null)
            {
                return fireResult;
            }
        }

        return $"{CombatMessages.Label(self, capitalized: true)} is frozen in terror, unable to retreat.";
    }

    /// <summary>
    /// The single neighboring tile (of the 8 around self) that increases squared distance from
    /// the source the most, provided it's walkable and unoccupied -- null when every neighbor is
    /// equally close or closer than the current tile (already backed into a corner, or the
    /// source is diagonally adjacent to every remaining opening). Squared distance avoids a
    /// needless Math.Sqrt just to compare magnitudes.
    /// </summary>
    private static (int X, int Y)? FindFleeStep(Actor self, Level level, Actor source)
    {
        int currentDistanceSquared = DistanceSquared(self.X, self.Y, source.X, source.Y);
        (int X, int Y)? best = null;
        int bestDistanceSquared = currentDistanceSquared;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                {
                    continue;
                }

                int nx = self.X + dx, ny = self.Y + dy;
                if (level.IsBlockedForActorMovement(nx, ny))
                {
                    continue;
                }

                int distanceSquared = DistanceSquared(nx, ny, source.X, source.Y);
                if (distanceSquared > bestDistanceSquared)
                {
                    bestDistanceSquared = distanceSquared;
                    best = (nx, ny);
                }
            }
        }

        return best;
    }

    private static int DistanceSquared(int x1, int y1, int x2, int y2)
    {
        int dx = x1 - x2, dy = y1 - y2;
        return (dx * dx) + (dy * dy);
    }
}
