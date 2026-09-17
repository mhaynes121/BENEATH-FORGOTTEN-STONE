using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// The outcome of comparing two ItemComparisonRatings. DefinitiveWinner is only ever non-null
/// when both ratings are complete (identified) -- an incomplete comparison can still hint a
/// direction via ApparentlyStronger (computed from the same, honestly-partial totals), but must
/// never be presented as certain. See ItemComparisonFormatter for the exact wording each
/// combination produces.
/// </summary>
public class ComparisonResult
{
    public bool BothComplete { get; init; }
    public Item DefinitiveWinner { get; init; }
    public Item ApparentlyStronger { get; init; }
    public decimal Delta { get; init; }

    /// <summary>True only when both ratings are complete AND swapping each side's Expected proc value for its Maximum would change who wins (including flipping a win into a tie or vice versa) -- i.e. the realistic outcome genuinely depends on proc luck.</summary>
    public bool ProcDependent { get; init; }
}

public static class ItemComparer
{
    public static ComparisonResult Compare(Item itemA, ItemComparisonRating a, Item itemB, ItemComparisonRating b)
    {
        decimal totalA = a.OverallQuality;
        decimal totalB = b.OverallQuality;
        bool bothComplete = a.IsComplete && b.IsComplete;

        Item apparentlyStronger = totalA == totalB ? null : totalA > totalB ? itemA : itemB;
        Item definitiveWinner = bothComplete ? apparentlyStronger : null;

        bool procDependent = false;
        if (bothComplete)
        {
            decimal altA = totalA - a.ExpectedProcValue + a.MaximumProcValue;
            decimal altB = totalB - b.ExpectedProcValue + b.MaximumProcValue;
            Item altWinner = altA == altB ? null : altA > altB ? itemA : itemB;
            procDependent = definitiveWinner != altWinner;
        }

        return new ComparisonResult
        {
            BothComplete = bothComplete,
            DefinitiveWinner = definitiveWinner,
            ApparentlyStronger = apparentlyStronger,
            Delta = Math.Abs(totalA - totalB),
            ProcDependent = procDependent
        };
    }
}
