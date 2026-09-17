using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities;
using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Core.Screens;

/// <summary>
/// Buy/Sell/Identify interface opened by bumping into a Trader (see GameLoop.HandleMove's
/// bump interception) -- mirrors InventoryScreen's own Console.Clear()/ReadKey loop shape
/// rather than being built out of repeated MenuPrompt.Choose calls, since a persistent
/// gold/tab header needs its own redraw every iteration. Buying/selling/identifying inside
/// this screen never consumes a player turn, same convention InventoryScreen/HelpScreen
/// already use. Selling only ever offers player.Inventory.Items (never equipped items) --
/// the player unequips via the existing Inventory screen first, where the existing
/// cursed-item block (InventoryScreen.TryUnequip) already applies, so there's no second
/// unequip-and-sell path that could bypass it.
/// </summary>
public static class TraderScreen
{
    private const int BuyTab = 0;
    private const int SellTab = 1;

    /// <summary>At most this many items are drawn per column before the list wraps into a new one alongside it -- see RenderColumns.</summary>
    private const int MaxRowsPerColumn = 16;

    private static readonly Random rng = new();

    /// <summary>Flavor variety for TrySell's inventory-full rejection -- picked at random rather than always the same line.</summary>
    private static readonly string[] InventoryFullMessages =
    {
        "My inventory is already full -- I can't take on anything else right now.",
        "I've got no room left for that. Come back after I've sold off some stock.",
        "My shelves are packed solid. I'll have to pass on that one.",
        "I can't carry another thing right now -- try me again later.",
        "Sorry, I'm stocked to the rafters. No room for that."
    };

    public static void Show(Player player, Trader trader, Level level)
    {
        // One-time cleanup: merges any carried stack that ended up split into several entries
        // for a historical reason (e.g. a type only just added to ItemStacking.IsStackable) back
        // into a single running count -- a no-op once everything's already properly merged, so
        // it's cheap to run every time this screen opens.
        ItemStacking.ConsolidateStacks(player.Inventory);

        string message = "";
        int tab = BuyTab;

        using var margin = new ScreenMargin();
        while (true)
        {
            ConsoleSafety.TryClear();
            ConsoleSafety.TrySetCursorPosition(0, 0);

            Console.WriteLine("==============================");
            Console.WriteLine("           TRADER");
            Console.WriteLine("==============================");
            Console.WriteLine();
            Console.WriteLine(tab == BuyTab ? "BUY" : "SELL");
            Console.WriteLine();

            if (tab == BuyTab)
            {
                ShowBuyTab(trader, player);
            }
            else
            {
                ShowSellTab(player);
            }

            Console.WriteLine();
            Console.WriteLine($"Gold: {player.Gold}g");
            Console.WriteLine();
            if (!string.IsNullOrEmpty(message))
            {
                Console.WriteLine(message);
                Console.WriteLine();
            }

            Console.WriteLine(tab == BuyTab
                ? "1-9/a-z: buy   L: inspect   C: compare   Tab: sell   Esc: leave"
                : "1-9/a-z: sell   L: inspect   I: identify   Tab: buy   Esc: leave");

            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Escape)
            {
                // Renderer.Render only redraws the map's own footprint -- this screen's much
                // wider/taller text would otherwise leave stray characters behind once the
                // game view resumes. Same reasoning as InventoryScreen's own exit.
                ConsoleSafety.TryClear();
                return;
            }
            if (key.Key == ConsoleKey.Tab)
            {
                tab = tab == BuyTab ? SellTab : BuyTab;
                message = "";
                continue;
            }

            // Case-insensitive, matching MenuPrompt.Choose's own normalization -- a raw,
            // case-SENSITIVE comparison here meant that on any terminal/OS combination that
            // reports an unshifted letter keypress as uppercase (or a shifted one as lowercase --
            // e.g. Caps Lock being on), the displayed lowercase letter (MenuPrompt.OptionKey
            // always shows 'a'-'z') and the actual key that registered could silently disagree.
            char keyChar = char.ToLowerInvariant(key.KeyChar);
            if (keyChar == 'l')
            {
                message = InspectFlow(player, trader, tab);
                continue;
            }
            if (keyChar == 'i' && tab == SellTab)
            {
                message = IdentifyFlow(player);
                continue;
            }
            if (keyChar == 'c' && tab == BuyTab)
            {
                ItemComparisonScreen.ShowTraderCompare(player, trader);
                continue;
            }

            int index = MenuPrompt.IndexFromKey(keyChar);
            if (index < 0)
            {
                message = "";
                continue;
            }

            message = tab == BuyTab ? TryBuy(player, trader, index) : SellWithQuantityPrompt(player, trader, index);
        }
    }

    private static void ShowBuyTab(Trader trader, Player player)
    {
        var items = trader.Inventory.Items;
        if (items.Count == 0)
        {
            Console.WriteLine("(nothing left to buy)");
            return;
        }

        var lines = new List<(string Text, ConsoleColor Color)>();
        for (int i = 0; i < items.Count; i++)
        {
            int price = ItemPricingCalculator.CalculateBuyPrice(items[i], player);
            bool canUse = ItemRequirementValidator.CanUse(player, items[i], out _);
            var color = ItemLineColor(canUse, canAfford: player.Gold >= price);
            string name = items[i].DisplayName + StackSuffix(items[i]);
            lines.Add(($"{MenuPrompt.OptionKey(i)}. {name,-30}{price,6}g", color));
        }
        RenderColumns(lines);
    }

    private static void ShowSellTab(Player player)
    {
        var items = player.Inventory.Items;
        if (items.Count == 0)
        {
            Console.WriteLine("(you have nothing to sell)");
            return;
        }

        var lines = new List<(string Text, ConsoleColor Color)>();
        for (int i = 0; i < items.Count; i++)
        {
            int price = ItemPricingCalculator.CalculateSellPrice(items[i], player);
            bool canUse = ItemRequirementValidator.CanUse(player, items[i], out _);
            // No affordability concept when selling -- only usability dims a line here.
            var color = ItemLineColor(canUse, canAfford: true);
            string name = items[i].DisplayName + StackSuffix(items[i]);
            lines.Add(($"{MenuPrompt.OptionKey(i)}. {name,-30}{price,6}g", color));
        }
        RenderColumns(lines);
    }

    /// <summary>
    /// Lays out up to MaxRowsPerColumn lines per column, column-major (the first column fills
    /// top-to-bottom before the next one starts) -- used once a trader's stock or the player's
    /// own inventory grows past what fits in a single column. Purely a display concern: each
    /// line's key binding (MenuPrompt.OptionKey, already baked into Text by the caller) doesn't
    /// change based on where it's drawn on screen.
    /// </summary>
    /// <summary>Exposed for Diagnostics/SelfTest.cs -- a pure function of the item count, so the "16 per column" rule is directly testable without capturing Console output.</summary>
    internal static int ColumnCountFor(int itemCount) => (int)Math.Ceiling(itemCount / (double)MaxRowsPerColumn);

    private static void RenderColumns(IReadOnlyList<(string Text, ConsoleColor Color)> lines)
    {
        if (lines.Count == 0)
        {
            return;
        }

        int columnCount = ColumnCountFor(lines.Count);
        int columnWidth = lines.Max(l => l.Text.Length) + 3;

        for (int row = 0; row < MaxRowsPerColumn; row++)
        {
            bool wroteAnything = false;
            for (int col = 0; col < columnCount; col++)
            {
                int index = col * MaxRowsPerColumn + row;
                if (index >= lines.Count)
                {
                    continue;
                }

                wroteAnything = true;
                Console.ForegroundColor = lines[index].Color;
                Console.Write(col == columnCount - 1 ? lines[index].Text : lines[index].Text.PadRight(columnWidth));
            }

            if (wroteAnything)
            {
                Console.WriteLine();
            }
        }
        Console.ResetColor();
    }

    /// <summary>
    /// Brighter for an item the player can both use and (when buying) afford right now;
    /// dimmer otherwise. Usability (level/class/race requirements -- ItemRequirementValidator,
    /// the same check InventoryScreen.ApplyItem enforces before letting an item be used)
    /// takes visual priority over affordability with its own color, since it's a hard block
    /// no amount of gold changes, distinct from "just can't afford it yet."
    /// </summary>
    internal static ConsoleColor ItemLineColor(bool canUse, bool canAfford) =>
        !canUse ? ConsoleColor.DarkRed : canAfford ? ConsoleColor.White : ConsoleColor.DarkGray;

    /// <summary>" x5" for a stacked item holding more than one -- buying/selling always moves the whole entry at once (see TryBuy/TrySell), so this tells the player up front how many units that single priced line actually represents.</summary>
    internal static string StackSuffix(Item item) =>
        ItemStacking.IsStackable(item) && item.Charges > 1 ? $" x{item.Charges}" : "";

    /// <summary>The actual buy decision/action, split out from Show's key handling so it's exercisable without Console.ReadKey -- see Diagnostics/SelfTest.cs.</summary>
    internal static string TryBuy(Player player, Trader trader, int index)
    {
        var items = trader.Inventory.Items;
        if (index < 0 || index >= items.Count)
        {
            return "";
        }

        var item = items[index];
        int price = ItemPricingCalculator.CalculateBuyPrice(item, player);

        if (!EncumbranceCalculator.CanCarry(player, item, out string capacityReason))
        {
            return capacityReason;
        }
        if (!player.SpendGold(price))
        {
            return "You don't have enough gold for that.";
        }

        trader.Inventory.RemoveItem(item);
        player.Inventory.AddItem(item);
        return $"You buy the {item.DisplayName} for {price}g.";
    }

    /// <summary>
    /// Prompts for a quantity first when the selected item is a stack of more than one (Enter
    /// alone sells the whole stack, matching the pre-existing behavior for everything else) --
    /// split out from TrySell itself so that method stays directly exercisable from
    /// Diagnostics/SelfTest.cs without needing to drive NumericPrompt's own Console.ReadKey loop.
    /// </summary>
    private static string SellWithQuantityPrompt(Player player, Trader trader, int index)
    {
        var items = player.Inventory.Items;
        if (index < 0 || index >= items.Count)
        {
            return "";
        }

        var item = items[index];
        int stackCount = ItemStacking.IsStackable(item) ? item.Charges ?? 1 : 1;
        int quantity = stackCount;
        if (stackCount > 1)
        {
            int? chosen = NumericPrompt.ReadQuantity($"Sell how many {item.DisplayName}?", stackCount);
            if (chosen == null)
            {
                return "Cancelled.";
            }
            quantity = chosen.Value;
        }

        return TrySell(player, trader, index, quantity);
    }

    /// <summary>
    /// The actual sell decision/action -- see TryBuy's own note. `quantity` defaults to the
    /// entire stack (or 1 for a non-stackable item), matching the original whole-entry-at-a-time
    /// behavior when omitted; a smaller quantity splits exactly that many units off into their
    /// own item (mirroring GameLoop.HandleDropItem's own stack-splitting shape) and prices/sells
    /// only that portion, leaving the rest of the stack untouched in the player's inventory.
    /// </summary>
    internal static string TrySell(Player player, Trader trader, int index, int? quantity = null)
    {
        var items = player.Inventory.Items;
        if (index < 0 || index >= items.Count)
        {
            return "";
        }

        var item = items[index];
        if (item.Container?.Contents.Items.Count > 0)
        {
            return $"Empty the {item.DisplayName} before selling it.";
        }

        if (trader.Inventory.Items.Count >= TraderConfig.TraderInventoryCap)
        {
            return InventoryFullMessages[rng.Next(InventoryFullMessages.Length)];
        }

        int stackCount = ItemStacking.IsStackable(item) ? item.Charges ?? 1 : 1;
        int sellQuantity = Math.Clamp(quantity ?? stackCount, 1, stackCount);

        Item sold;
        if (sellQuantity >= stackCount)
        {
            player.Inventory.RemoveItem(item);
            sold = item;
        }
        else
        {
            sold = item.Clone();
            sold.Charges = sellQuantity;
            ItemStacking.ConsumeMany(player.Inventory, item, sellQuantity);
        }

        int price = ItemPricingCalculator.CalculateSellPrice(sold, player);
        player.CollectGold(price);
        trader.Inventory.AddItem(sold);

        string label = sellQuantity > 1 ? $"{sellQuantity} {sold.DisplayName}s" : $"the {sold.DisplayName}";
        return $"You sell {label} for {price}g.";
    }

    private static string InspectFlow(Player player, Trader trader, int tab)
    {
        var items = tab == BuyTab ? trader.Inventory.Items : player.Inventory.Items;
        if (items.Count == 0)
        {
            return "Nothing to inspect.";
        }

        var labels = items.Select(i => i.DisplayName).ToList();
        int? index = MenuPrompt.Choose("Inspect which item?", labels, allowCancel: true);
        return index == null ? "Cancelled." : InventoryScreen.FormatItemDetails(player, items[index.Value]);
    }

    /// <summary>
    /// Same unidentified-candidate filter InventoryScreen.ApplyItem's IsIdentifyScroll branch
    /// already uses -- reuses the exact same identification mechanism (just item.IsIdentified
    /// = true, no event/registry) rather than inventing a trader-specific one.
    /// </summary>
    private static string IdentifyFlow(Player player)
    {
        var candidates = UnidentifiedCandidates(player);
        if (candidates.Count == 0)
        {
            return "You have nothing unidentified to identify.";
        }

        var labels = candidates.Select(i => i.DisplayName).ToList();
        int? index = MenuPrompt.Choose("Identify which item?", labels, allowCancel: true);
        return index == null ? "Cancelled." : TryIdentify(player, candidates[index.Value]);
    }

    private static List<Item> UnidentifiedCandidates(Player player) =>
        player.Inventory.Items
            .Concat(player.Equipment.AllEquipped.Select(kvp => kvp.Value))
            .Where(i => !i.IsIdentified)
            .ToList();

    /// <summary>The actual identify decision/action, split out from IdentifyFlow's item-selection prompt so it's exercisable without Console.ReadKey -- see Diagnostics/SelfTest.cs.</summary>
    internal static string TryIdentify(Player player, Item target)
    {
        if (!player.SpendGold(TraderConfig.TraderIdentificationCost))
        {
            return $"Identification costs {TraderConfig.TraderIdentificationCost}g -- you don't have enough.";
        }

        target.IsIdentified = true;
        return $"You pay {TraderConfig.TraderIdentificationCost}g and learn it is a {target.Name}.";
    }
}
