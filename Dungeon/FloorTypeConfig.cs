namespace BENEATH_FORGOTTEN_STONE.Dungeon;

/// <summary>Tunable values for the floor-type system -- mirrors BossConfig/TraderConfig/RegenerationConfig's plain-constants style, so every number the design calls out as "should be easy to tune" lives in one place instead of scattered through DungeonGenerator/EnvironmentalFloorEffects.</summary>
public static class FloorTypeConfig
{
    // --- Room-level special-floor generation -- see DungeonGenerator.AssignSpecialFloors ---

    public const int MinimumAffectedRooms = 0;
    public const int MaximumAffectedRooms = 3;

    public const double MinimumSpecialFloorCoverage = 0.25;
    public const double MaximumSpecialFloorCoverage = 0.75;

    /// <summary>Chance a special-floor room also gets one small, disconnected secondary cluster of the same FloorType, in addition to its main region.</summary>
    public const double SecondaryClusterChance = 0.15;

    // --- Environmental damage scaling -- see EnvironmentalFloorEffects ---

    public const int BaseFireDamage = 2;
    public const int FireDamagePerDungeonLevel = 2;

    public const int BaseLavaDamage = 4;
    public const int LavaDamagePerDungeonLevel = 3;
}
