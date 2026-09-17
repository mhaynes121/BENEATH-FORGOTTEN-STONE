using BENEATH_FORGOTTEN_STONE.Dungeon;

namespace BENEATH_FORGOTTEN_STONE.Entities.AI;

/// <summary>
/// Generalizes monster AI's target acquisition beyond "always the player" -- ChaseAI,
/// RangedAttackAI, and SpellCasterAI previously hardcoded the player as the only possible
/// target; now they engage whichever of {the player, the player's own active pet} is closer, per
/// the Pet and Companion System proposal's section 14 ("monster target selection... must be
/// updated to use allegiance checks"). Returns exactly the player when no pet exists or the pet
/// isn't currently active, so every pre-existing monster/test scenario (no Player.Pet set) is
/// completely unaffected.
/// </summary>
public static class HostileTargetSelector
{
    public static Actor NearestPlayerSideTarget(Level level, Actor self)
    {
        var player = level.Actors.OfType<Player>().FirstOrDefault(p => p.IsAlive);
        if (player == null)
        {
            return null;
        }

        Actor best = player;
        int bestDistanceSq = DistanceSquared(self, player);

        if (player.Pet is { LifecycleState: PetLifecycleState.Active } pet && pet.IsAlive && level.Actors.Contains(pet))
        {
            int petDistanceSq = DistanceSquared(self, pet);
            if (petDistanceSq < bestDistanceSq)
            {
                best = pet;
            }
        }

        return best;
    }

    private static int DistanceSquared(Actor a, Actor b)
    {
        int dx = a.X - b.X;
        int dy = a.Y - b.Y;
        return (dx * dx) + (dy * dy);
    }
}
