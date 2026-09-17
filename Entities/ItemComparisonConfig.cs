namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Every tunable number the Item Comparison system uses -- weights for effect types that have
/// no natural damage-equivalent formula, plus the beneficial-effect evaluation horizon -- lives
/// here so rebalancing never touches ItemQualityCalculator's own logic. First-pass values; easy
/// to retune once the feature has been played with.
/// </summary>
public static class ItemComparisonConfig
{
    /// <summary>Turns used to value an ongoing beneficial effect (e.g. Regeneration) that has no shorter explicit Duration -- see the Item Comparison proposal's own worked example (10 HP/turn x 10 turns = 100 quality).</summary>
    public const int RegenerationHorizonTurns = 10;

    /// <summary>Stun carries no Magnitude (see ItemStatusEffect's own doc comment -- "Unused for Stun") -- Duration is the only lever, so this weight alone converts "turns disabled" into quality.</summary>
    public const double StunWeightPerTurn = 3.0;

    public const double CorrodeWeightPerPoint = 1.0;

    /// <summary>Weighted above a one-shot proc point -- Protection applies passively on every hit taken while equipped, not just on a chance-based trigger.</summary>
    public const double ProtectionWeightPerPoint = 1.5;

    public const double ThornsWeightPerPoint = 1.0;
    public const double ResistanceWeightPerPoint = 0.5;

    /// <summary>A permanent primary-attribute point is broad-impact (affects far more than combat), weighted accordingly.</summary>
    public const double AttributeWeightPerPoint = 2.0;

    /// <summary>1 point of DefenseBonus is treated as equally valuable as 1 point of PhysicalAttackBonus (the implicit 1.0/point baseline every weapon's "base value" already uses).</summary>
    public const double DefenseWeight = 1.0;

    public const double MagicalAttackWeight = 1.0;

    /// <summary>Carry capacity is minor utility compared to combat stats -- lbs are plentiful and rarely decisive.</summary>
    public const double CarryCapacityWeightPerPoint = 0.1;

    /// <summary>Applied on top of whatever negative stats a cursed item already carries -- represents the standalone risk of being unable to remove it. See EquippedComparisonTargetFinder/ItemComparisonCompatibility.CanReplaceInContext for the separate, harder rule that a cursed equipped item can never be silently auto-replaced at all.</summary>
    public const double CurseFlatPenalty = -5.0;
}
