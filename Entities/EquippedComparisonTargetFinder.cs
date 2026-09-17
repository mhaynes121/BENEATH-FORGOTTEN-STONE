using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Finds equipped items comparable to a candidate item, searching every equipped slot by
/// semantic role rather than a fixed preferred-slot order -- so a weapon in the Off-hand is found
/// even when a Shield occupies the usually-preferred Primary Hand slot, and the Shield itself is
/// never mistaken for a candidate replacement target.
/// </summary>
public static class EquippedComparisonTargetFinder
{
    /// <summary>Every currently-equipped item that shares the candidate's comparison category at its own slot -- includes cursed and unidentified matches; see FindWeakestLegalReplacementTarget for the filtered, pickup-safe version.</summary>
    public static List<(EquipmentSlot Slot, Item Item)> FindComparableEquipped(Player player, Item candidate)
    {
        var matches = new List<(EquipmentSlot, Item)>();
        foreach (var kvp in player.Equipment.AllEquipped)
        {
            var equippedCategory = ItemComparisonCompatibility.CategoryFor(kvp.Value, kvp.Key);
            if (equippedCategory != ItemComparisonCategory.None &&
                equippedCategory == ItemComparisonCompatibility.CategoryFor(candidate, kvp.Key))
            {
                matches.Add((kvp.Key, kvp.Value));
            }
        }
        return matches;
    }

    /// <summary>
    /// The lowest-quality legally-replaceable, IDENTIFIED equipped candidate -- used by pickup
    /// upgrade detection. An unidentified equipped item is never auto-selected as "the weakest"
    /// (the proposal's own words: "use explicit selection or avoid declaring it the weakest"),
    /// and ItemComparisonCompatibility.CanReplaceInContext already excludes cursed, illegal-for-
    /// class, and conflicting candidates. Returns null when nothing qualifies.
    /// </summary>
    public static (EquipmentSlot Slot, Item Item)? FindWeakestLegalReplacementTarget(Player player, Item candidate)
    {
        return FindComparableEquipped(player, candidate)
            .Where(m => m.Item.IsIdentified && ItemComparisonCompatibility.CanReplaceInContext(player, candidate, m.Item, m.Slot))
            .OrderBy(m => ItemQualityCalculator.Evaluate(m.Item).OverallQuality)
            .Select(m => ((EquipmentSlot, Item)?)m)
            .FirstOrDefault();
    }
}
