namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Tunable knobs for the Sleep command's Luck-based ambush/boss rolls -- see GameLoop's
/// HandleStartSleeping/CheckSleepInterrupts and Core/SleepAmbushSystem.cs. Same
/// BaseChance + (stat - Baseline) * ScalingPerPoint, clamped shape GameLoop.DetectTrapsChanceForStat
/// already uses; the design doc specifies the behavior (higher Luck lowers both chances) but no
/// numbers, so these are a starting point, easy to retune here without touching the roll logic.
/// </summary>
public static class SleepConfig
{
    public const double AmbushBaseChance = 0.25;
    public const int AmbushBaselineLuck = 10;
    public const double AmbushLuckScalingPerPoint = -0.015;
    public const double AmbushMinChance = 0.10;
    public const double AmbushMaxChance = 0.40;

    /// <summary>Inclusive range for how many turns of sleep pass before a successful ambush roll actually arrives -- "a short, randomized period of sleep" per the design doc.</summary>
    public const int AmbushMinDelayTurns = 3;
    public const int AmbushMaxDelayTurns = 7;

    /// <summary>Rolled only once an ordinary ambush has already succeeded -- "a second, very small Luck-based roll."</summary>
    public const double BossAmbushBaseChance = 0.08;
    public const double BossAmbushLuckScalingPerPoint = -0.005;
    public const double BossAmbushMinChance = 0.02;
    public const double BossAmbushMaxChance = 0.15;
}
