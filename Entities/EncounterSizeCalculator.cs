namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Shared "how many monsters spawn together" roll -- extracted from DungeonGenerator.SpawnMonsters
/// so Core/SleepAmbushSystem.cs's ambush encounter can reuse the exact same difficulty-scaled pack
/// size instead of a second copy that could drift apart. Capped rather than growing unbounded with
/// depth -- otherwise very deep floors rack up enough live actors that per-turn scheduler/AI/render
/// overhead becomes noticeable, even though no single call is individually slow. Distinct from
/// BossConfig.BossMinEntourage/BossMaxEntourage, which is a flat boss-room-only set piece (4-8),
/// not a depth-scaled ordinary encounter.
/// </summary>
public static class EncounterSizeCalculator
{
    /// <returns>1 to 5, scaling up slightly with difficultyLevel.</returns>
    public static int RollGroupSize(int difficultyLevel, Random rng) =>
        rng.Next(1, Math.Min(6, 2 + (difficultyLevel / 2)));
}
