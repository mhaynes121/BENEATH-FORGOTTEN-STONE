using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities;

namespace BENEATH_FORGOTTEN_STONE.Core;

/// <summary>Everything SpawnAmbush actually placed -- Monsters (in placement order; index 0 is the one replaced by a boss, if any) plus the boss reference itself for message-building.</summary>
public class AmbushResult
{
    public List<Monster> Monsters { get; init; }
    public Monster Boss { get; init; }
    public bool IncludesBoss => Boss != null;
}

/// <summary>
/// Pure ambush decision logic for the Sleep command -- no Console/GameLoop coupling, same split as
/// GameLoop.DecideFireProjectileAction/ResolveEquipOffer, so it's directly unit-testable. See
/// GameLoop.HandleStartSleeping (rolls the initial ambush chance/delay) and
/// GameLoop.CheckSleepInterrupts (calls SpawnAmbush once the scheduled turn arrives).
/// </summary>
public static class SleepAmbushSystem
{
    public static bool RollAmbushChance(int luck, Random rng) =>
        rng.NextDouble() < Math.Clamp(
            SleepConfig.AmbushBaseChance + ((luck - SleepConfig.AmbushBaselineLuck) * SleepConfig.AmbushLuckScalingPerPoint),
            SleepConfig.AmbushMinChance, SleepConfig.AmbushMaxChance);

    public static int RollAmbushDelay(Random rng) =>
        rng.Next(SleepConfig.AmbushMinDelayTurns, SleepConfig.AmbushMaxDelayTurns + 1);

    /// <summary>Rolled only once an ordinary ambush has already succeeded -- see SpawnAmbush.</summary>
    public static bool RollBossChance(int luck, Random rng) =>
        rng.NextDouble() < Math.Clamp(
            SleepConfig.BossAmbushBaseChance + ((luck - SleepConfig.AmbushBaselineLuck) * SleepConfig.BossAmbushLuckScalingPerPoint),
            SleepConfig.BossAmbushMinChance, SleepConfig.BossAmbushMaxChance);

    /// <summary>
    /// Candidate tiles within Chebyshev distance 6 of the player, valid via IsBlockedForActorMovement
    /// (the existing single source of truth for "can an actor stand here"), scored to prefer outside
    /// the player's current sight and not directly adjacent, then randomly tiebroken within each
    /// score tier. Returns up to `count` -- fewer if the level genuinely doesn't have that many
    /// valid tiles nearby, so a spawn is never forced onto something unsafe. Safe Monster Spawning
    /// and Hazard-Aware Movement: also excludes Fire/Lava tiles -- every ambush monster comes from
    /// Monster.CreateRandom (SpawnAmbush never calls CreateFloorAttuned), which never produces a
    /// floor-attuned archetype, so a plain tile-level hazard check (no actor needed yet) already
    /// correctly answers "safe for whatever gets placed here" without knowing which monster that
    /// will be.
    /// </summary>
    public static List<(int X, int Y)> FindAmbushPositions(Level level, Player player, int count, Random rng)
    {
        var candidates = new List<(int X, int Y, int Score)>();
        for (int dx = -6; dx <= 6; dx++)
        {
            for (int dy = -6; dy <= 6; dy++)
            {
                int x = player.X + dx;
                int y = player.Y + dy;
                if (!level.IsInBounds(x, y) || level.IsBlockedForActorMovement(x, y) || FloorTypeCatalog.IsHazardous(level.Tiles[x, y].FloorType))
                {
                    continue;
                }

                int score = (level.Tiles[x, y].IsVisible ? 0 : 2) + (Math.Max(Math.Abs(dx), Math.Abs(dy)) > 1 ? 1 : 0);
                candidates.Add((x, y, score));
            }
        }

        return candidates
            .OrderByDescending(c => c.Score)
            .ThenBy(_ => rng.Next())
            .Take(count)
            .Select(c => (c.X, c.Y))
            .ToList();
    }

    /// <summary>
    /// Rolls an encounter size (EncounterSizeCalculator -- the same depth-scaled pack size ordinary
    /// rooms use), places that many ordinary monsters, then rolls the separate boss chance and, on
    /// success, replaces the first placed monster with a floor-appropriate boss at the same
    /// position -- never exceeding the normal group-size limit. Every monster is freshly registered
    /// with 0 starting energy, which is exactly "no free attack" -- it still has to earn its first
    /// turn through the scheduler like anything else.
    /// </summary>
    public static AmbushResult SpawnAmbush(Level level, Player player, int difficultyLevel, Random rng)
    {
        int count = EncounterSizeCalculator.RollGroupSize(difficultyLevel, rng);
        var positions = FindAmbushPositions(level, player, count, rng);

        var monsters = new List<Monster>();
        foreach (var (x, y) in positions)
        {
            var monster = Monster.CreateRandom(x, y, difficultyLevel, rng);
            level.Actors.Add(monster);
            level.Scheduler.Register(monster);
            monsters.Add(monster);
        }

        Monster boss = null;
        if (monsters.Count > 0 && RollBossChance(player.Stats.Adjusted(PrimaryAttribute.Luck), rng))
        {
            var replaced = monsters[0];
            level.Actors.Remove(replaced);
            level.Scheduler.Unregister(replaced);

            boss = Monster.CreateBoss(replaced.X, replaced.Y, difficultyLevel, rng);
            level.Actors.Add(boss);
            level.Scheduler.Register(boss);
            monsters[0] = boss;
        }

        return new AmbushResult { Monsters = monsters, Boss = boss };
    }
}
