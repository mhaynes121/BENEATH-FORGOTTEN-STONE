using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities;

namespace BENEATH_FORGOTTEN_STONE.Entities.AI;

public interface IAIComponent
{
    /// <returns>A player-facing message describing what happened this turn (e.g. an attack or spell result), or null if nothing worth reporting occurred.</returns>
    string TakeTurn(Actor self, Level level, Random rng);
}
