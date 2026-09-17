namespace BENEATH_FORGOTTEN_STONE.Entities.Proficiency;

/// <summary>Every tunable number the Ability Proficiency System uses, in one place, per this codebase's established config-class convention (RegenerationConfig, SleepConfig, ItemComparisonConfig, ...).</summary>
public static class ProficiencyConfig
{
    /// <summary>Hidden proficiency total -> rank. Novice is implicitly [0, PracticedThreshold).</summary>
    public const int PracticedThreshold = 20;
    public const int ProficientThreshold = 60;
    public const int ExpertThreshold = 140;
    public const int MasterThreshold = 280;

    /// <summary>Base points awarded per legitimate use, before aptitude/catalog-level/challenge scaling.</summary>
    public const double FailurePoints = 1.0;
    public const double SuccessPoints = 2.0;

    /// <summary>Flat bonus from a successful lucky insight -- never multiplied by aptitude or the catalog-level modifier.</summary>
    public const double LuckyInsightBonusPoints = 3.0;

    /// <summary>LuckyInsightChance = clamp(Base + (AdjustedLuck-10) * PerLuckPoint, Min, Max).</summary>
    public const double LuckyInsightBase = 0.02;
    public const double LuckyInsightPerLuckPoint = 0.005;
    public const double LuckyInsightMin = 0.01;
    public const double LuckyInsightMax = 0.08;

    /// <summary>Challenge eligibility (spec section 17): playerLevel - targetLevel gap -> proficiency multiplier. A negative gap (target at or above player level) also falls under the first bucket.</summary>
    public const int ChallengeFullCreditMaxGap = 5;
    public const int ChallengeHalfCreditMaxGap = 10;
    public const double ChallengeHalfCreditMultiplier = 0.5;
}
