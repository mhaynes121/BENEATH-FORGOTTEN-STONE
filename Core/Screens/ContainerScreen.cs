using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities;
using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Core.Screens;

/// <summary>
/// Interactive transfer/examine/compare screen for any ContainerComponent -- a treasure Chest
/// (Core/GameLoop.cs HandleOpenChest) or a carried bag (InventoryScreen's 'O' command). Modeled
/// on InventoryScreen/TraderScreen's looping Console.Clear-and-redraw shape rather than
/// ItemComparisonScreen's one-shot display, since this supports several sequential actions.
/// Transfers only ever move items between this container and the player's own top-level
/// inventory (see ContainerTransferService) -- never directly between two containers.
/// </summary>
public static class ContainerScreen
{
    /// <param name="allowPut">Corpse System: false for a corpse container -- "the player should not be allowed to place items into it." Hides the Inventory/Put section entirely rather than just disabling the key, since a corpse has nothing to offer that section's display anyway.</param>
    public static void Show(Player player, ContainerComponent container, string containerName, Level level, bool allowPut = true)
    {
        string message = "";
        using var margin = new ScreenMargin();
        while (true)
        {
            ConsoleSafety.TryClear();
            ConsoleSafety.TrySetCursorPosition(0, 0);

            Console.WriteLine($"=== {containerName} ===");
            Console.WriteLine();

            double rawContentWeight = ContainerWeightCalculator.RawContentWeight(container);
            double carriedContentWeight = container.Kind == ContainerKind.Chest
                ? rawContentWeight
                : rawContentWeight * (1 - container.WeightReduction);

            Console.WriteLine($"Slots: {container.Contents.Items.Count}/{container.SlotCapacity}");
            if (container.Kind == ContainerKind.PortableBag && container.WeightReduction > 0)
            {
                Console.WriteLine($"Weight reduction: {container.WeightReduction:0%}");
            }
            Console.WriteLine($"Contents: {rawContentWeight:0.#} lbs raw / {carriedContentWeight:0.#} lbs carried");
            Console.WriteLine($"Carrying: {EncumbranceCalculator.CurrentWeight(player):0.#} / {EncumbranceCalculator.MaxCapacity(player):0.#} lbs");
            Console.WriteLine();

            Console.WriteLine("CONTENTS:");
            if (container.Contents.Items.Count == 0)
            {
                Console.WriteLine("  (empty)");
            }
            else
            {
                for (int i = 0; i < container.Contents.Items.Count; i++)
                {
                    Console.WriteLine($"  {MenuPrompt.OptionKey(i)}. {ItemLine(container.Contents.Items[i])}");
                }
            }
            Console.WriteLine();

            if (allowPut)
            {
                Console.WriteLine("Inventory:");
                var eligibleItems = EligibleInventoryItems(player, container);
                if (eligibleItems.Count == 0)
                {
                    Console.WriteLine("  (nothing you're carrying fits in here)");
                }
                else
                {
                    for (int i = 0; i < eligibleItems.Count; i++)
                    {
                        Console.WriteLine($"  {MenuPrompt.OptionKey(i)}. {ItemLine(eligibleItems[i])}");
                    }
                }
                Console.WriteLine();
            }

            if (!string.IsNullOrEmpty(message))
            {
                Console.WriteLine(message);
                Console.WriteLine();
            }
            Console.WriteLine(allowPut
                ? "1-9/a-z: take   R: remove qty   G: get all   P: put   L: examine   C: compare   Esc: close"
                : "1-9/a-z: take   R: remove qty   G: get all   L: examine   C: compare   Esc: close");

            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Escape)
            {
                if (container.Kind == ContainerKind.PortableBag)
                {
                    container.IsOpen = false;
                }
                ConsoleSafety.TryClear();
                return;
            }

            // Case-insensitive, matching MenuPrompt.Choose's own normalization -- a raw,
            // case-SENSITIVE comparison here meant that on any terminal/OS combination that
            // reports an unshifted letter keypress as uppercase (or a shifted one as lowercase --
            // e.g. Caps Lock being on), the displayed letter and the actual key that registered
            // could silently disagree.
            char keyChar = char.ToLowerInvariant(key.KeyChar);
            if (keyChar == 'r')
            {
                message = RemoveFlow(player, container, containerName);
                continue;
            }
            if (keyChar == 'g')
            {
                message = GetAllFlow(player, container, containerName);
                continue;
            }
            if (keyChar == 'p' && allowPut)
            {
                message = PutFlow(player, container);
                continue;
            }
            if (keyChar == 'l')
            {
                message = ExamineFlow(player, container);
                continue;
            }
            if (keyChar == 'c')
            {
                ItemComparisonScreen.ShowContainerCompare(player, containerName, container);
                message = "";
                continue;
            }

            // Direct number/letter selection: takes that one CONTENTS item immediately (the
            // whole stack), the fast path for "I already know exactly which one I want" without
            // detouring through the R flow's own quantity prompt -- R still exists for when a
            // partial quantity off a stack is actually wanted. Always means "take FROM the
            // container," never "put" -- even though the Inventory/Put list below reuses the same
            // key labels for its own, separate items; Put still requires pressing P first.
            int takeIndex = MenuPrompt.IndexFromKey(keyChar);
            if (takeIndex >= 0 && takeIndex < container.Contents.Items.Count)
            {
                var result = ContainerTransferService.TryRemove(player, container, container.Contents.Items[takeIndex], containerName);
                message = result.Message;
            }
        }
    }

    private static string ItemLine(Item item)
    {
        bool showCharges = item.Charges != null && (!ItemStacking.IsStackable(item) || item.Charges > 1);
        return item.DisplayName + (showCharges ? $" [{item.Charges}]" : "");
    }

    private static string RemoveFlow(Player player, ContainerComponent container, string containerName)
    {
        if (container.Contents.Items.Count == 0)
        {
            return "There's nothing in here to remove.";
        }

        var labels = container.Contents.Items.Select(ItemLine).ToList();
        int? index = MenuPrompt.Choose("Remove which item?", labels, allowCancel: true);
        if (index == null)
        {
            return "Cancelled.";
        }
        var item = container.Contents.Items[index.Value];

        int? quantity = PromptQuantity(item, "Remove how many {0}?");
        if (quantity == null)
        {
            return "Cancelled.";
        }

        var result = ContainerTransferService.TryRemove(player, container, item, containerName, quantity);
        return result.Message;
    }

    /// <summary>
    /// "Get All" for any container -- a chest, a corpse (see GameLoop's open-container flow,
    /// which converts an emptied corpse into a portable item exactly the same way whether it was
    /// emptied one item at a time via Remove or all at once here), or a carried bag. Reuses
    /// ContainerTransferService.TryRemove per item (the same weight/stacking rules a manual
    /// Remove already enforces) rather than reimplementing them, and reports one combined summary
    /// instead of one message per item, mirroring GameLoop.PickUpAll's own ground-item "Get All"
    /// exactly. Needs no selection menu at all (unlike Remove/Put), so this is directly
    /// unit-testable -- internal (not private) for that reason.
    /// </summary>
    internal static string GetAllFlow(Player player, ContainerComponent container, string containerName)
    {
        if (container.Contents.Items.Count == 0)
        {
            return "There's nothing in here to take.";
        }

        var taken = new List<string>();
        string failureReason = null;

        foreach (var item in container.Contents.Items.ToList())
        {
            var result = ContainerTransferService.TryRemove(player, container, item, containerName);
            if (result.Success)
            {
                taken.Add(item.DisplayName);
            }
            else
            {
                failureReason ??= result.Message;
            }
        }

        if (taken.Count == 0)
        {
            return failureReason ?? "You couldn't take anything from here.";
        }

        string message = $"You take {string.Join(", ", taken)}.";
        if (failureReason != null)
        {
            message += $" ({failureReason})";
        }
        return message;
    }

    private static string PutFlow(Player player, ContainerComponent container)
    {
        var eligibleItems = EligibleInventoryItems(player, container);
        if (eligibleItems.Count == 0)
        {
            return "You aren't carrying anything that fits in here.";
        }

        var labels = eligibleItems.Select(ItemLine).ToList();
        int? index = MenuPrompt.Choose("Put which item?", labels, allowCancel: true);
        if (index == null)
        {
            return "Cancelled.";
        }
        var item = eligibleItems[index.Value];

        int? quantity = PromptQuantity(item, "Put how many {0}?");
        if (quantity == null)
        {
            return "Cancelled.";
        }

        var result = ContainerTransferService.TryPut(player, container, item, quantity);
        return result.Message;
    }

    /// <summary>Carried items this specific container could actually accept -- excludes another container entirely (no bag ever holds a container, though a Chest does -- see ContainerRules.CanInsert) and, for a bag, anything too large for its size class. Shared by the main screen's "Inventory:" display and the Put flow's own selection list, so what's shown is always exactly what's selectable. Internal (not private) so Diagnostics/SelfTest.cs can verify the filter directly.</summary>
    internal static List<Item> EligibleInventoryItems(Player player, ContainerComponent container) =>
        player.Inventory.Items.Where(i => ContainerRules.CanInsert(container, i, out _)).ToList();

    /// <summary>Whole stack (or the single unit) by default -- only prompts when there's an actual choice to make, matching TraderScreen.SellWithQuantityPrompt's precedent.</summary>
    private static int? PromptQuantity(Item item, string promptFormat)
    {
        int stackCount = ItemStacking.IsStackable(item) ? item.Charges ?? 1 : 1;
        if (stackCount <= 1)
        {
            return stackCount;
        }
        return NumericPrompt.ReadQuantity(string.Format(promptFormat, item.DisplayName), stackCount);
    }

    private static string ExamineFlow(Player player, ContainerComponent container)
    {
        var candidates = new List<(string Label, Item Item)>();
        foreach (var item in container.Contents.Items)
        {
            candidates.Add((ItemLine(item), item));
        }
        foreach (var item in player.Inventory.Items)
        {
            candidates.Add((ItemLine(item), item));
        }

        if (candidates.Count == 0)
        {
            return "There's nothing to examine.";
        }

        var labels = candidates.Select(c => c.Label).ToList();
        int? index = MenuPrompt.Choose("Examine which item?", labels, allowCancel: true);
        return index == null ? "Cancelled." : InventoryScreen.FormatItemDetails(player, candidates[index.Value].Item);
    }
}
