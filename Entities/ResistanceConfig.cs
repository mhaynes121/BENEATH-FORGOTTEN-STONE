namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>Centralized resistance-system tuning knobs -- mirrors BossConfig/TraderConfig/RegenerationConfig/FloorAttunementConfig's plain-constants style.</summary>
public static class ResistanceConfig
{
    /// <summary>Effective resistance is clamped to this range after every source is summed -- see ResistanceCalculator.GetEffectiveResistance. Never applied to an individual source, only the final total.</summary>
    public const int MinimumResistance = -75;
    public const int MaximumResistance = 75;

    /// <summary>Resistance reduces an associated status effect's duration at only half the strength it reduces damage/application chance -- see ResistanceCalculator.AdjustStatusDuration.</summary>
    public const double StatusDurationResistanceFactor = 0.50;
}
