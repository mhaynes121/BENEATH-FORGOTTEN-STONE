namespace BENEATH_FORGOTTEN_STONE.Entities.Proficiency;

/// <summary>
/// Every rank-indexed lookup table the spec defines. Despite the spec listing six named
/// categories ("Offensive physical skills," "Spell and non-weapon potency," "Secondary-effect
/// application," "Duration," "Range," "Utility checks"), the actual numbers collapse to only
/// four distinct tables -- potency/duration/range are the identical 90/95/100/110/120% table, and
/// physical-skill accuracy is the identical +-10/+-5/0 percentage-point table as utility checks.
/// Six abilities (Pick Lock, Sneak, Pick Pocket, Poison Weapon, Execution Call) have their own
/// bespoke per-rank numbers that don't fit any generic table -- see the bottom of this class.
/// </summary>
public static class ProficiencyScaling
{
    public static ProficiencyRank RankFor(double proficiency) => proficiency switch
    {
        >= ProficiencyConfig.MasterThreshold => ProficiencyRank.Master,
        >= ProficiencyConfig.ExpertThreshold => ProficiencyRank.Expert,
        >= ProficiencyConfig.ProficientThreshold => ProficiencyRank.Proficient,
        >= ProficiencyConfig.PracticedThreshold => ProficiencyRank.Practiced,
        _ => ProficiencyRank.Novice
    };

    /// <summary>Damage/healing/mana/buff/debuff magnitude, duration, and range all share this exact table.</summary>
    public static double StandardMultiplier(ProficiencyRank rank) => rank switch
    {
        ProficiencyRank.Novice => 0.90,
        ProficiencyRank.Practiced => 0.95,
        ProficiencyRank.Proficient => 1.00,
        ProficiencyRank.Expert => 1.10,
        ProficiencyRank.Master => 1.20,
        _ => 1.00
    };

    /// <summary>Physical-skill accuracy and percentage-based utility checks share this exact table.</summary>
    public static double PercentagePointAdjustment(ProficiencyRank rank) => rank switch
    {
        ProficiencyRank.Novice => -0.10,
        ProficiencyRank.Practiced => -0.05,
        ProficiencyRank.Proficient => 0.0,
        ProficiencyRank.Expert => 0.05,
        ProficiencyRank.Master => 0.10,
        _ => 0.0
    };

    /// <summary>Secondary hostile-effect application chance (Stun/Silence/Burning/Poisoned/Shocked/hostile stat reductions) -- Expert and Master both cap at 100%, per spec.</summary>
    public static double SecondaryEffectChance(ProficiencyRank rank) => rank switch
    {
        ProficiencyRank.Novice => 0.75,
        ProficiencyRank.Practiced => 0.90,
        _ => 1.00
    };

    /// <summary>d20-form of the utility-check adjustment (Pick Lock's roll).</summary>
    public static int D20Adjustment(ProficiencyRank rank) => rank switch
    {
        ProficiencyRank.Novice => -2,
        ProficiencyRank.Practiced => -1,
        ProficiencyRank.Proficient => 0,
        ProficiencyRank.Expert => 1,
        ProficiencyRank.Master => 2,
        _ => 0
    };

    /// <summary>Governing-attribute learning rate (spec section 9) -- how quickly proficiency accrues, off the aptitude-only adjusted stat value (clamp(BaseValue+ClassModifier,1,18)+EquipmentModifier, excluding OtherModifier).</summary>
    public static double AptitudeLearningRate(int adjustedAttribute) => adjustedAttribute switch
    {
        <= 5 => 0.80,
        <= 8 => 0.90,
        <= 11 => 1.00,
        <= 14 => 1.10,
        <= 17 => 1.20,
        _ => 1.25
    };

    /// <summary>Catalog-level learning modifier (spec section 10) -- later/rarer abilities advance faster since they see fewer opportunities to use.</summary>
    public static double CatalogLevelLearningModifier(int level) => level switch
    {
        <= 5 => 1.00,
        <= 10 => 1.15,
        <= 20 => 1.30,
        _ => 1.50
    };

    /// <summary>LuckyInsightChance = clamp(2% + (Luck-10)*0.5%, 1%, 8%) (spec section 11), off the same aptitude-only adjusted Luck value.</summary>
    public static double LuckyInsightChance(int adjustedLuck) =>
        Math.Clamp(ProficiencyConfig.LuckyInsightBase + (adjustedLuck - 10) * ProficiencyConfig.LuckyInsightPerLuckPoint,
            ProficiencyConfig.LuckyInsightMin, ProficiencyConfig.LuckyInsightMax);

    /// <summary>Challenge eligibility (spec section 17) -- how much of the base points a target's level actually earns, relative to the player's own level. A target at or above the player's level always earns full credit.</summary>
    public static double ChallengeMultiplier(int targetLevel, int playerLevel)
    {
        int gap = playerLevel - targetLevel;
        if (gap <= ProficiencyConfig.ChallengeFullCreditMaxGap)
        {
            return 1.0;
        }
        return gap <= ProficiencyConfig.ChallengeHalfCreditMaxGap ? ProficiencyConfig.ChallengeHalfCreditMultiplier : 0.0;
    }

    // --- Bespoke per-ability tables (spec section 19) -- numbers that don't fit any generic table above ---

    /// <summary>Pick Lock's lockpick-break chance after a failed attempt.</summary>
    public static double PickLockBreakChance(ProficiencyRank rank) => rank switch
    {
        ProficiencyRank.Novice => 0.45,
        ProficiencyRank.Practiced => 0.42,
        ProficiencyRank.Proficient => 0.40,
        ProficiencyRank.Expert => 0.30,
        ProficiencyRank.Master => 0.20,
        _ => 0.40
    };

    /// <summary>Sneak's detection radius in tiles -- Novice and Practiced deliberately share the same whole-tile radius (5), per spec's own note, while still advancing at different rates toward Proficient.</summary>
    public static int SneakDetectionRadius(ProficiencyRank rank) => rank switch
    {
        ProficiencyRank.Novice => 5,
        ProficiencyRank.Practiced => 5,
        ProficiencyRank.Proficient => 4,
        ProficiencyRank.Expert => 3,
        ProficiencyRank.Master => 2,
        _ => 4
    };

    /// <summary>Pick Pocket's bonus-item chance.</summary>
    public static double PickPocketBonusChance(ProficiencyRank rank) => rank switch
    {
        ProficiencyRank.Novice => 0.20,
        ProficiencyRank.Practiced => 0.22,
        ProficiencyRank.Proficient => 0.25,
        ProficiencyRank.Expert => 0.30,
        ProficiencyRank.Master => 0.35,
        _ => 0.25
    };

    /// <summary>Poison Weapon's full 4-column profile -- window (turns the weapon stays coated), proc chance, poison duration, tick damage.</summary>
    public static (int Window, double Chance, int Duration, int TickDamage) PoisonWeaponProfile(ProficiencyRank rank) => rank switch
    {
        ProficiencyRank.Novice => (9, 0.25, 4, 2),
        ProficiencyRank.Practiced => (10, 0.28, 4, 2),
        ProficiencyRank.Proficient => (10, 0.30, 4, 2),
        ProficiencyRank.Expert => (11, 0.35, 4, 2),
        ProficiencyRank.Master => (12, 0.40, 5, 2),
        _ => (10, 0.30, 4, 2)
    };

    /// <summary>Execution Call's target-HP threshold multiplier (threshold = Agility * this value, rounded to the nearest whole HP by the caller).</summary>
    public static double ExecutionCallThresholdMultiplier(ProficiencyRank rank) => rank switch
    {
        ProficiencyRank.Novice => 9.0,
        ProficiencyRank.Practiced => 9.5,
        ProficiencyRank.Proficient => 10.0,
        ProficiencyRank.Expert => 11.0,
        ProficiencyRank.Master => 12.0,
        _ => 10.0
    };
}
