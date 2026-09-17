using BENEATH_FORGOTTEN_STONE.Entities.Components;
using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// The player-visible quality breakdown for one item, computed live rather than stored -- see
/// ItemQualityCalculator.Evaluate. IsComplete mirrors Item.IsIdentified: false means BaseValue is
/// the only populated component (the item's "mundane" bonus, knowable without identifying it) and
/// every other field is zero, never a peek at concealed procs/stats/resistances/curse status.
/// </summary>
public class ItemComparisonRating
{
    public bool IsComplete { get; init; }
    public decimal BaseValue { get; init; }
    public decimal ExpectedProcValue { get; init; }
    public decimal MaximumProcValue { get; init; }

    /// <summary>Signed -- weighted attribute/resistance/carry-capacity contributions, positive or negative, plus weighted non-damage proc/passive effects (Stun, Corrode, Protection, Thorns, Regeneration). A negative StatModifier or resistance naturally subtracts here rather than needing separate penalty handling.</summary>
    public decimal UtilityValue { get; init; }

    /// <summary>The curse flat penalty only (see ItemComparisonConfig.CurseFlatPenalty) -- kept separate from UtilityValue so the breakdown can show "why" a cursed item scores lower beyond just its own negative stats.</summary>
    public decimal PenaltyValue { get; init; }

    public decimal OverallQuality => BaseValue + ExpectedProcValue + UtilityValue + PenaltyValue;
}

/// <summary>
/// Computes an ItemComparisonRating for any equippable item -- weapons, armor, shields, wands,
/// rings, ammunition. Never stores the result on Item (see the Item Comparison proposal's own
/// reasoning: rebalancing, charges, identification, and new effect types would all leave a cached
/// score stale). Deliberately does not accept a separate "knowledge" parameter -- Item.IsIdentified
/// already is that concept in this codebase, so reading it directly avoids an unused abstraction.
/// </summary>
public static class ItemQualityCalculator
{
    public static ItemComparisonRating Evaluate(Item item)
    {
        // Base weapon/armor value is "mundane" -- knowable without identifying the item, matching
        // the proposal's Known-Information Rule -- so it's computed and shown unconditionally.
        decimal baseValue = item.PhysicalAttackBonus
            + (decimal)(item.MagicalAttackBonus * ItemComparisonConfig.MagicalAttackWeight)
            + (decimal)(item.DefenseBonus * ItemComparisonConfig.DefenseWeight);

        if (!item.IsIdentified)
        {
            // Everything else (procs, stat modifiers, resistances, Blessed/Cursed) is concealed
            // by identification (see InventoryScreen.FormatItemDetails' own all-or-nothing gate)
            // and must never influence a shown score -- so it simply isn't computed at all here.
            return new ItemComparisonRating { IsComplete = false, BaseValue = baseValue };
        }

        decimal maxProc = 0, expectedProc = 0, utility = 0;
        foreach (var effect in item.StatusEffects)
        {
            switch (effect.EffectType)
            {
                case ItemEffectType.Fire or ItemEffectType.Frost or ItemEffectType.Poison
                    or ItemEffectType.Bleed or ItemEffectType.Shock:
                    decimal thisMax = effect.Magnitude * Math.Max(1, effect.Duration);
                    maxProc += thisMax;
                    expectedProc += thisMax * (decimal)effect.Chance;
                    break;

                case ItemEffectType.Stun:
                    utility += (decimal)(ItemComparisonConfig.StunWeightPerTurn * effect.Duration * effect.Chance);
                    break;

                case ItemEffectType.Corrode:
                    utility += (decimal)(ItemComparisonConfig.CorrodeWeightPerPoint * effect.Magnitude
                        * Math.Max(1, effect.Duration) * effect.Chance);
                    break;

                case ItemEffectType.Regeneration:
                    int turns = effect.Duration > 0
                        ? Math.Min(effect.Duration, ItemComparisonConfig.RegenerationHorizonTurns)
                        : ItemComparisonConfig.RegenerationHorizonTurns;
                    utility += effect.Magnitude * turns;
                    break;

                case ItemEffectType.Protection:
                    utility += (decimal)(ItemComparisonConfig.ProtectionWeightPerPoint * effect.Magnitude);
                    break;

                case ItemEffectType.Thorns:
                    utility += (decimal)(ItemComparisonConfig.ThornsWeightPerPoint * effect.Magnitude);
                    break;
            }
        }

        foreach (var amount in item.StatModifiers.Values)
        {
            utility += (decimal)(amount * ItemComparisonConfig.AttributeWeightPerPoint);
        }
        foreach (var type in Enum.GetValues<ResistanceType>())
        {
            utility += (decimal)(item.ResistanceModifiers.Get(type) * ItemComparisonConfig.ResistanceWeightPerPoint);
        }
        utility += (decimal)(item.EncumbranceModifier * ItemComparisonConfig.CarryCapacityWeightPerPoint);

        decimal penalty = item.IsCursed ? (decimal)ItemComparisonConfig.CurseFlatPenalty : 0;

        return new ItemComparisonRating
        {
            IsComplete = true,
            BaseValue = baseValue,
            ExpectedProcValue = expectedProc,
            MaximumProcValue = maxProc,
            UtilityValue = utility,
            PenaltyValue = penalty
        };
    }
}
