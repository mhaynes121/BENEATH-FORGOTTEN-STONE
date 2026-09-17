using BENEATH_FORGOTTEN_STONE.Core;
using BENEATH_FORGOTTEN_STONE.Dungeon;

namespace BENEATH_FORGOTTEN_STONE.Entities.AI;

/// <summary>
/// The test pet dog's behavior -- target priority (proposal section 5) and pursuit take priority
/// over noncombat movement, which no longer tries to stay adjacent to the owner (Revised Pet
/// Movement Proposal): an idle pet instead roams randomly within a PetConfig.RoamingRadius-tile
/// Chebyshev area centered on its owner, and returns toward the owner only once it ends up
/// outside that area. Reads Pet.Owner throughout, never "the player" directly, so this same AI
/// works unmodified for any future non-player owner. Combat "notifications" are deliberately not
/// a separate event bus: Actor.LastCombatTarget/LastAttacker are already stamped at
/// CombatStatsTracker.ApplyDamage, the one choke point every damage source in the game already
/// shares (melee, ranged, thrown, spells, skills, DoTs), so this AI just reads those two fields
/// off its Owner every turn instead of subscribing to anything.
/// </summary>
public class PetAI : IAIComponent
{
    public string TakeTurn(Actor self, Level level, Random rng)
    {
        var pet = (Pet)self;
        var owner = pet.Owner;
        if (owner == null || !owner.IsAlive || !level.Actors.Contains(owner))
        {
            return null; // orphaned or separated from its owner's level -- nothing sensible to do
        }

        PruneThreatMemory(pet, level);
        NoteCombatNotifications(pet, owner);

        var target = SelectTarget(pet, owner, level);
        pet.PreferredTarget = target;

        if (target != null)
        {
            // Combat overrides roaming entirely -- a pet may leave (and stay outside) its
            // owner's roaming area for as long as a valid target exists (proposal section 9).
            if (IsAdjacent(pet, target))
            {
                return Attack(pet, target, level, rng);
            }
            MoveToward(pet, level, target.X, target.Y);
            return null;
        }

        return RoamOrReturn(pet, owner, level, rng);
    }

    private static string Attack(Pet pet, Actor target, Level level, Random rng)
    {
        pet.LastAttackTurn = level.TurnNumber;
        var result = pet.PhysicalAttack(target, rng);
        bool attackerVisible = level.Tiles[pet.X, pet.Y].IsVisible;
        return CombatMessages.FormatAttack(result, attackerVisible);
    }

    /// <summary>
    /// Recomputed fresh every turn from the owner's CURRENT position (the roaming area is not a
    /// fixed region -- proposal section 7), so "stop returning" and "resume roaming" both fall
    /// out naturally from re-checking this same distance on the very next scheduled turn, with no
    /// separate return-mode flag needed.
    /// </summary>
    private static string RoamOrReturn(Pet pet, Actor owner, Level level, Random rng)
    {
        if (ChebyshevDistance(pet, owner) > PetConfig.RoamingRadius)
        {
            // Section 5/6: moves one step toward the owner's own position. This naturally lands
            // inside the roaming area (the owner's tile is always its own center) without needing
            // to separately search for "the nearest reachable tile in the area" -- and if the
            // owner's exact tile isn't fully reachable, MoveToward's own pathfind fallback still
            // makes whatever partial progress is possible and retries on the next scheduled turn,
            // matching section 6's "no tile in the area is reachable" fallback.
            MoveToward(pet, level, owner.X, owner.Y);
        }
        else
        {
            RoamRandomly(pet, owner, level, rng);
        }
        return null;
    }

    /// <summary>
    /// Proposal section 4: builds every SAFE adjacent destination that stays inside the roaming
    /// area, plus the pet's own current tile as a "remain still" option, and picks uniformly
    /// among them. Safe Monster Spawning and Hazard-Aware Movement: hazard-awareness now goes
    /// through the same centralized ActorTerrainSafety every other AI uses, replacing the local
    /// Fire/Lava-only check this used to carry; a hazardous neighbor is never a candidate at all
    /// here (idle roaming never NEEDS to move anywhere in particular -- "remain still" is always
    /// on the table, so there's no "accept it anyway" fallback to remove, unlike MoveToward).
    /// </summary>
    private static void RoamRandomly(Pet pet, Actor owner, Level level, Random rng)
    {
        var candidates = new List<(int X, int Y)> { (pet.X, pet.Y) };

        foreach (var (dx, dy) in EightDirections)
        {
            int x = pet.X + dx;
            int y = pet.Y + dy;
            if (!ActorTerrainSafety.CanActorOccupy(level, pet, x, y))
            {
                continue;
            }
            if (Math.Max(Math.Abs(x - owner.X), Math.Abs(y - owner.Y)) > PetConfig.RoamingRadius)
            {
                continue; // never intentionally leaves the area while idle
            }
            candidates.Add((x, y));
        }

        var choice = candidates[rng.Next(candidates.Count)];
        pet.MoveTo(choice.X, choice.Y);
    }

    private static readonly (int Dx, int Dy)[] EightDirections =
    {
        (0, -1), (0, 1), (-1, 0), (1, 0), (-1, -1), (1, -1), (-1, 1), (1, 1)
    };

    private static int ChebyshevDistance(Actor a, Actor b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

    private static Actor SelectTarget(Pet pet, Actor owner, Level level)
    {
        var shortlist = new List<Actor>();
        void Consider(Actor candidate)
        {
            if (candidate != null && IsValidThreat(candidate, level) && !shortlist.Contains(candidate))
            {
                shortlist.Add(candidate);
            }
        }

        // Priority order per the proposal's section 5.
        Consider(owner.LastCombatTarget);
        Consider(owner.LastAttacker);
        Consider(pet.PreferredTarget);
        Consider(pet.ThreatMemory.Where(a => IsValidThreat(a, level)).OrderBy(a => DistanceSquared(pet, a)).FirstOrDefault());

        int detectionRadius = DetectionRules.EffectiveRadius(owner);
        Consider(level.Actors.OfType<Monster>()
            .Where(m => IsValidThreat(m, level) && DistanceSquared(pet, m) <= detectionRadius * detectionRadius)
            .OrderBy(m => DistanceSquared(pet, m))
            .FirstOrDefault());

        // The pet must not chase a target it can't actually reach -- fall through to the next
        // priority candidate, then finally to following the owner, rather than stalling in place.
        return shortlist.FirstOrDefault(candidate => HasReachablePath(pet, candidate, level));
    }

    private static bool IsValidThreat(Actor candidate, Level level) =>
        candidate is Monster && candidate.IsAlive && candidate.CanBeTargeted && level.Actors.Contains(candidate);

    private static bool HasReachablePath(Pet pet, Actor target, Level level) =>
        IsAdjacent(pet, target) || TerrainPathfinder.FindNextStep(level, (pet.X, pet.Y), (target.X, target.Y), mover: pet) != null;

    private static void NoteCombatNotifications(Pet pet, Actor owner)
    {
        AddThreat(pet, owner.LastCombatTarget);
        AddThreat(pet, owner.LastAttacker);
        AddThreat(pet, pet.LastAttacker);
    }

    private static void AddThreat(Pet pet, Actor candidate)
    {
        if (candidate is Monster && candidate.IsAlive)
        {
            pet.ThreatMemory.Add(candidate);
        }
    }

    private static void PruneThreatMemory(Pet pet, Level level) =>
        pet.ThreatMemory.RemoveWhere(a => !IsValidThreat(a, level));

    private static bool IsAdjacent(Actor a, Actor b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y)) <= 1;

    private static int DistanceSquared(Actor a, Actor b)
    {
        int dx = a.X - b.X;
        int dy = a.Y - b.Y;
        return (dx * dx) + (dy * dy);
    }

    /// <summary>
    /// Mirrors ChaseAI.MoveTowardTarget's own greedy-step-with-wall-slide-then-pathfind-fallback
    /// shape -- see that method's doc comment for why. Duplicated rather than shared: the two AIs
    /// have no common base type, and the logic is short enough that a shared helper would cost
    /// more in indirection than it saves. Safe Monster Spawning and Hazard-Aware Movement: every
    /// candidate step (greedy or pathfound) must pass ActorTerrainSafety, with NO fallback that
    /// accepts a hazardous tile when nothing safe is available -- if every route to the target is
    /// hazardous, this simply does nothing this turn and the pet remains still, exactly like
    /// ChaseAI's own monsters.
    /// </summary>
    private static void MoveToward(Actor self, Level level, int targetX, int targetY)
    {
        int stepX = Math.Sign(targetX - self.X);
        int stepY = Math.Sign(targetY - self.Y);

        if (TryMove(self, level, self.X + stepX, self.Y + stepY)) return;
        if (TryMove(self, level, self.X + stepX, self.Y)) return;
        if (TryMove(self, level, self.X, self.Y + stepY)) return;

        var step = TerrainPathfinder.FindNextStep(level, (self.X, self.Y), (targetX, targetY), mover: self);
        if (step.HasValue)
        {
            TryMove(self, level, step.Value.X, step.Value.Y);
        }
    }

    private static bool TryMove(Actor self, Level level, int x, int y)
    {
        if (!ActorTerrainSafety.CanActorOccupy(level, self, x, y))
        {
            return false;
        }
        self.MoveTo(x, y);
        return true;
    }
}
