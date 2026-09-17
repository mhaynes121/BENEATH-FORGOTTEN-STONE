using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Shared player-facing text for every comparison context (Inventory Compare, Trader Compare,
/// Inventory/Trader examine, ground-item Look, pickup upgrade prompts) -- pure string building,
/// no Console I/O, mirroring CombatMessages' own role. Centralizing wording here is what keeps
/// "comparable" (a genuine tie) and "cannot be fully evaluated" (an unidentified side) from ever
/// getting confused with each other across four different screens.
/// </summary>
public static class ItemComparisonFormatter
{
    public static string OwnedItemLabel(Player player, Item item)
    {
        foreach (var kvp in player.Equipment.AllEquipped)
        {
            if (ReferenceEquals(kvp.Value, item))
            {
                return $"{item.DisplayName} -- equipped: {EquipmentCompatibility.SlotLabel(kvp.Key)}";
            }
        }
        foreach (var bag in player.Inventory.Items.Where(i => i.Container != null))
        {
            if (bag.Container.Contents.Items.Contains(item))
            {
                return $"{item.DisplayName} -- in {bag.DisplayName}";
            }
        }
        return $"{item.DisplayName} -- inventory";
    }

    /// <summary>
    /// The side-by-side breakdown -- one shape when both ratings are complete (Base/Expected
    /// proc/Maximum proc/Overall quality), a different, coarser shape when either is incomplete
    /// (Known base value/Hidden properties/Visible quality/Evaluation), per the proposal's own
    /// two distinct worked examples. priceLineB, when given, appends a trailing Price row under
    /// item B's column only (the Trader Compare screen's own addition).
    /// </summary>
    public static List<string> FormatTable(string labelA, ItemComparisonRating a, string labelB, ItemComparisonRating b, string priceLineB = null)
    {
        int columnWidth = Math.Max(labelA.Length, labelB.Length) + 3;
        var lines = new List<string> { "".PadRight(22) + labelA.PadRight(columnWidth) + labelB };

        string Row(string label, string valueA, string valueB) => label.PadRight(22) + valueA.PadRight(columnWidth) + valueB;

        if (a.IsComplete && b.IsComplete)
        {
            lines.Add(Row("Base value", Format(a.BaseValue), Format(b.BaseValue)));
            lines.Add(Row("Expected proc value", Format(a.ExpectedProcValue), Format(b.ExpectedProcValue)));
            lines.Add(Row("Maximum proc value", Format(a.MaximumProcValue), Format(b.MaximumProcValue)));
            lines.Add(Row("Overall quality", Format(a.OverallQuality), Format(b.OverallQuality)));
        }
        else
        {
            lines.Add(Row("Known base value", Format(a.BaseValue), Format(b.BaseValue)));
            lines.Add(Row("Hidden properties", a.IsComplete ? "-" : "?", b.IsComplete ? "-" : "?"));
            lines.Add(Row("Visible quality", Format(a.OverallQuality), Format(b.OverallQuality)));
            lines.Add(Row("Evaluation", a.IsComplete ? "Complete" : "Incomplete", b.IsComplete ? "Complete" : "Incomplete"));
        }

        if (priceLineB != null)
        {
            lines.Add(Row("Price", "", priceLineB));
        }
        return lines;
    }

    public static string FormatDeltaSentence((string Label, Item Item) a, (string Label, Item Item) b, ComparisonResult result)
    {
        if (result.BothComplete)
        {
            if (result.DefinitiveWinner == null)
            {
                return $"{a.Label} and {b.Label} are comparable.";
            }
            string winnerLabel = ReferenceEquals(result.DefinitiveWinner, a.Item) ? a.Label : b.Label;
            return $"{winnerLabel} has +{Format(result.Delta)} quality.";
        }

        if (result.ApparentlyStronger == null)
        {
            return "Cannot be fully evaluated until identified.";
        }
        string strongerLabel = ReferenceEquals(result.ApparentlyStronger, a.Item) ? a.Label : b.Label;
        return $"{strongerLabel} appears stronger based on known properties. Its full quality cannot be determined until it is identified.";
    }

    /// <summary>Null when the comparison isn't proc-dependent -- callers should skip this line entirely rather than print an empty one.</summary>
    public static string FormatProcQualifier(ComparisonResult result) =>
        result.ProcDependent
            ? "This difference depends partly on a chance-based effect triggering -- the realistic outcome may vary."
            : null;

    /// <summary>
    /// The one-or-two-line summary used by every examine context (Inventory examine, Trader
    /// inspect, ground-item Look). Returns null when there's nothing worth adding: an identified
    /// item with no comparable equipped counterpart. When multiple equipped counterparts exist
    /// and none are identified, also returns null rather than guessing which one is "the weakest"
    /// (the proposal's own words) -- a single candidate is always shown regardless of its own
    /// identification status, since there's no ambiguity to avoid in that case.
    /// </summary>
    public static string FormatLightweightLine(Player player, Item item)
    {
        var itemRating = ItemQualityCalculator.Evaluate(item);
        var counterpart = PickExamineCounterpart(player, item);

        if (counterpart == null)
        {
            return itemRating.IsComplete ? null : $"Visible quality: {Format(itemRating.OverallQuality)} (incomplete).";
        }

        var counterpartRating = ItemQualityCalculator.Evaluate(counterpart.Value.Item);
        var result = ItemComparer.Compare(item, itemRating, counterpart.Value.Item, counterpartRating);
        string equippedLabel = $"Equipped {counterpart.Value.Item.DisplayName}";

        if (result.BothComplete)
        {
            decimal difference = itemRating.OverallQuality - counterpartRating.OverallQuality;
            return $"Quality: {Format(itemRating.OverallQuality)} / {equippedLabel}: {Format(counterpartRating.OverallQuality)} / Difference: {FormatSigned(difference)}";
        }

        string itemQualityLabel = itemRating.IsComplete ? Format(itemRating.OverallQuality) : $"{Format(itemRating.OverallQuality)} (incomplete)";
        string counterpartQualityLabel = counterpartRating.IsComplete ? Format(counterpartRating.OverallQuality) : $"{Format(counterpartRating.OverallQuality)} (incomplete)";

        if (result.ApparentlyStronger == null)
        {
            return $"Visible quality: {itemQualityLabel} / {equippedLabel}: {counterpartQualityLabel} / Cannot be fully evaluated until identified.";
        }

        // Always name whichever side is actually ApparentlyStronger and say "stronger" -- never a
        // "weaker" variant naming the other side, which would otherwise get the direction backwards.
        string strongerName = ReferenceEquals(result.ApparentlyStronger, item) ? "This item" : equippedLabel;
        return $"Visible quality: {itemQualityLabel} / {equippedLabel}: {counterpartQualityLabel} / " +
            $"{strongerName} appears stronger based on known properties.";
    }

    private static (EquipmentSlot Slot, Item Item)? PickExamineCounterpart(Player player, Item item)
    {
        var candidates = EquippedComparisonTargetFinder.FindComparableEquipped(player, item)
            .Where(m => !ReferenceEquals(m.Item, item))
            .ToList();
        if (candidates.Count == 0)
        {
            return null;
        }
        if (candidates.Count == 1)
        {
            return candidates[0];
        }

        var identified = candidates.Where(m => m.Item.IsIdentified).ToList();
        return identified.Count == 0
            ? null
            : identified.OrderBy(m => ItemQualityCalculator.Evaluate(m.Item).OverallQuality).First();
    }

    private static string Format(decimal value) => value == Math.Truncate(value) ? value.ToString("0") : value.ToString("0.#");

    private static string FormatSigned(decimal value) => value >= 0 ? $"+{Format(value)}" : Format(value);
}
