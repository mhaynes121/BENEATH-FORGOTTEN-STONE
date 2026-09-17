namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Centralized boss-system tuning knobs -- mirrors ItemTypeWeights' style (plain constants,
/// no behavior). Referenced by DungeonGenerator (spawn chance, entourage size), Monster
/// (scaling multipliers, guaranteed-item count), and GameLoop (gold multiplier), so every
/// number the design calls "should be easy to modify during balancing" lives in one place.
/// </summary>
public static class BossConfig
{
    public const bool BossSpawnEnabled = true;

    /// <summary>Per-floor chance of generating a boss room at all -- see DungeonGenerator.TrySpawnBossRoom.</summary>
    public const double BossRoomSpawnChance = 0.45;

    public const int BossMinEntourage = 4;
    public const int BossMaxEntourage = 8;

    public const int BossLevelBonus = 1;
    public const double BossHealthMultiplier = 1.50;
    public const double BossDamageMultiplier = 1.25;
    public const int BossDefenseBonus = 2;

    public const double BossGoldMultiplier = 2.0;

    public const int BossMinItems = 1;
    public const int BossMaxItems = 2;
}
