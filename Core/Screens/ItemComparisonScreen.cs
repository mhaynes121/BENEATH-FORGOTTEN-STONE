using System.Text;
using BENEATH_FORGOTTEN_STONE.Entities;
using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Core.Screens;

/// <summary>
/// Interactive item-selection and side-by-side comparison display -- reached via 'C' from the
/// Inventory screen (owned-vs-owned) and the Trader Buy tab (trader-vs-owned). Modeled on
/// MessageHistoryScreen's single-shot shape (render, wait for a key, clear) rather than a looping
/// tab screen, since a comparison is a one-time display, not an interactive multi-turn view. Both
/// entry points are always free actions -- neither consumes a turn nor mutates any state.
/// </summary>
public static class ItemComparisonScreen
{
    public static void ShowInventoryCompare(Player player)
    {
        var owned = GatherOwnedItems(player);
        if (owned.Count == 0)
        {
            ShowMessage("You have nothing to compare.");
            return;
        }

        var firstLabels = owned.Select(i => ItemComparisonFormatter.OwnedItemLabel(player, i)).ToList();
        int? firstIndex = MenuPrompt.Choose("Compare which item?", firstLabels, allowCancel: true);
        if (firstIndex == null)
        {
            return;
        }
        var first = owned[firstIndex.Value];

        var candidates = owned.Where(i => !ReferenceEquals(i, first) && ItemComparisonCompatibility.CanCompare(first, i, player)).ToList();
        if (candidates.Count == 0)
        {
            ShowMessage($"You do not own another item comparable to the {first.DisplayName}.");
            return;
        }

        var secondLabels = candidates.Select(i => ItemComparisonFormatter.OwnedItemLabel(player, i)).ToList();
        int? secondIndex = MenuPrompt.Choose($"Compare the {first.DisplayName} to:", secondLabels, allowCancel: true);
        if (secondIndex == null)
        {
            return;
        }
        var second = candidates[secondIndex.Value];

        ShowComparison(first, ItemComparisonFormatter.OwnedItemLabel(player, first),
            second, ItemComparisonFormatter.OwnedItemLabel(player, second));
    }

    public static void ShowTraderCompare(Player player, Trader trader)
    {
        var traderItems = trader.Inventory.Items;
        if (traderItems.Count == 0)
        {
            ShowMessage("Nothing to compare.");
            return;
        }

        var traderLabels = traderItems.Select(i => i.DisplayName).ToList();
        int? traderIndex = MenuPrompt.Choose("Compare which item?", traderLabels, allowCancel: true);
        if (traderIndex == null)
        {
            return;
        }
        var traderItem = traderItems[traderIndex.Value];

        var owned = GatherOwnedItems(player);
        var candidates = owned.Where(i => ItemComparisonCompatibility.CanCompare(traderItem, i, player)).ToList();
        if (candidates.Count == 0)
        {
            ShowMessage($"You do not own an item comparable to the {traderItem.DisplayName}.");
            return;
        }

        var labels = candidates.Select(i => ItemComparisonFormatter.OwnedItemLabel(player, i)).ToList();
        int? index = MenuPrompt.Choose($"Compare Trader's {traderItem.DisplayName} to:", labels, allowCancel: true);
        if (index == null)
        {
            return;
        }
        var ownedItem = candidates[index.Value];

        int price = ItemPricingCalculator.CalculateBuyPrice(traderItem, player);
        ShowComparison(ownedItem, ItemComparisonFormatter.OwnedItemLabel(player, ownedItem),
            traderItem, traderItem.DisplayName, priceLineB: $"{price}g");
    }

    /// <summary>Every item the player owns for comparison purposes -- top-level inventory, equipped, and one level into any carried bag's own contents (nesting is disallowed, so this never needs to recurse further).</summary>
    private static List<Item> GatherOwnedItems(Player player)
    {
        var items = player.Inventory.Items.Concat(player.Equipment.AllEquipped.Select(kvp => kvp.Value)).ToList();
        foreach (var item in player.Inventory.Items.Where(i => i.Container != null).ToList())
        {
            items.AddRange(item.Container.Contents.Items);
        }
        return items;
    }

    /// <summary>Compares an item from an arbitrary open container (a Chest's contents) against a compatible owned item -- mirrors ShowTraderCompare's shape (pick from external stock, then pick a compatible owned item) but without a price line, since a chest item isn't for sale.</summary>
    public static void ShowContainerCompare(Player player, string containerName, ContainerComponent container)
    {
        var containerItems = container.Contents.Items;
        if (containerItems.Count == 0)
        {
            ShowMessage("Nothing to compare.");
            return;
        }

        var containerLabels = containerItems.Select(i => i.DisplayName).ToList();
        int? containerIndex = MenuPrompt.Choose("Compare which item?", containerLabels, allowCancel: true);
        if (containerIndex == null)
        {
            return;
        }
        var containerItem = containerItems[containerIndex.Value];

        var owned = GatherOwnedItems(player);
        var candidates = owned.Where(i => ItemComparisonCompatibility.CanCompare(containerItem, i, player)).ToList();
        if (candidates.Count == 0)
        {
            ShowMessage($"You do not own an item comparable to the {containerItem.DisplayName}.");
            return;
        }

        var labels = candidates.Select(i => ItemComparisonFormatter.OwnedItemLabel(player, i)).ToList();
        int? index = MenuPrompt.Choose($"Compare {containerName}'s {containerItem.DisplayName} to:", labels, allowCancel: true);
        if (index == null)
        {
            return;
        }
        var ownedItem = candidates[index.Value];

        ShowComparison(ownedItem, ItemComparisonFormatter.OwnedItemLabel(player, ownedItem),
            containerItem, containerItem.DisplayName);
    }

    private static void ShowComparison(Item itemA, string labelA, Item itemB, string labelB, string priceLineB = null)
    {
        var ratingA = ItemQualityCalculator.Evaluate(itemA);
        var ratingB = ItemQualityCalculator.Evaluate(itemB);
        var result = ItemComparer.Compare(itemA, ratingA, itemB, ratingB);

        var sb = new StringBuilder();
        sb.AppendLine("=== Compare Items ===");
        sb.AppendLine();
        foreach (var line in ItemComparisonFormatter.FormatTable(labelA, ratingA, labelB, ratingB, priceLineB))
        {
            sb.AppendLine(line);
        }
        sb.AppendLine();
        sb.AppendLine(ItemComparisonFormatter.FormatDeltaSentence((labelA, itemA), (labelB, itemB), result));
        var qualifier = ItemComparisonFormatter.FormatProcQualifier(result);
        if (qualifier != null)
        {
            sb.AppendLine(qualifier);
        }

        ShowMessage(sb.ToString().TrimEnd());
    }

    private static void ShowMessage(string body)
    {
        Renderer.RenderMessage(body + "\n\nPress any key to return...");
        Console.ReadKey(true);

        // Same reasoning as MessageHistoryScreen's own exit -- this screen's text would
        // otherwise leave stray characters behind once the game view resumes.
        ConsoleSafety.TryClear();
    }
}
