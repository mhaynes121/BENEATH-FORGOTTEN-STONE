using System.Text;
using BENEATH_FORGOTTEN_STONE.Entities;
using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Core.Screens;

/// <summary>
/// Adventure Record: lifetime run statistics for the current character. Opened with 'A' during
/// exploration (a free action -- see GameLoop's ShowAdventureRecord handling, which never
/// consumes a turn), or offered once in finalView mode right after permadeath is confirmed (see
/// GameLoop.HandleDeath). Every cumulative counter reads straight off Player.AdventureRecord;
/// the most valuable owned item is the one thing calculated fresh every time this opens (see
/// FindMostValuableItem) rather than stored, since a cached value would drift the moment
/// something was bought, sold, dropped, or picked up.
/// </summary>
public static class AdventureRecordScreen
{
    private static readonly Random TieBreakRng = new();

    /// <summary>Width of the bordered box -- wide enough for the longest line this screen ever produces (a boss name + level, or an item name + gold value) without wrapping in the game's default 60-column console.</summary>
    private const int Width = 44;

    public static void Show(Player player, bool finalView = false)
    {
        Renderer.RenderMessage(Build(player, finalView));
        Console.ReadKey(true);

        if (!finalView)
        {
            // Renderer.Render only redraws the map's own footprint -- this screen's wider/taller
            // text would otherwise leave stray characters behind once the game view resumes.
            // Skipped for finalView -- the death flow never redraws the dungeon again afterward,
            // it just returns to the main menu.
            ConsoleSafety.TryClear();
        }
    }

    /// <summary>Internal (not private) so Diagnostics/SelfTest.cs can verify the rendered text directly, the same reasoning MainMenuScreen.BuildTitleBlock is internal for.</summary>
    internal static string Build(Player player, bool finalView)
    {
        var record = player.AdventureRecord;
        var (mostValuableItem, mostValuableGold) = FindMostValuableItem(player);

        var sb = new StringBuilder();
        string bar = new string('=', Width);

        sb.AppendLine(bar);
        sb.AppendLine(Center("ADVENTURE RECORD", Width));
        sb.AppendLine(bar);
        sb.AppendLine();

        sb.AppendLine(StatLine("Monsters slain:", record.MonstersKilled.ToString("N0")));
        sb.AppendLine(StatLine("Bosses slain:", record.BossesKilled.ToString("N0")));
        sb.AppendLine("Strongest boss defeated:");
        sb.AppendLine(record.HighestLevelBossName == null
            ? "  None"
            : $"  {record.HighestLevelBossName} - Level {record.HighestLevelBossLevel}");
        sb.AppendLine();

        sb.AppendLine(StatLine("Damage dealt:", record.DamageDealt.ToString("N0")));
        sb.AppendLine(StatLine("Damage taken:", record.DamageTaken.ToString("N0")));
        sb.AppendLine();

        sb.AppendLine(StatLine("Gold collected:", $"{record.GoldCollected:N0}g"));
        sb.AppendLine(StatLine("Gold spent:", $"{record.GoldSpent:N0}g"));
        sb.AppendLine();

        sb.AppendLine("Most valuable item owned:");
        sb.AppendLine(mostValuableItem == null
            ? "  None"
            : $"  {mostValuableItem.DisplayName} - {mostValuableGold:N0}g");
        sb.AppendLine();

        sb.AppendLine(StatLine("Deepest floor reached:", record.DeepestFloorReached.ToString("N0")));
        sb.AppendLine(StatLine("Time in dungeon:", FormatTimeInDungeon(record.TimeInDungeonSeconds)));
        sb.AppendLine();

        sb.AppendLine(StatLine("Items permanently lost:", record.ItemsPermanentlyLost.ToString("N0")));
        sb.AppendLine(StatLine("Items misplaced:", record.ItemsMisplaced.ToString("N0")));
        sb.AppendLine(StatLine("Misplaced items recovered:", record.MisplacedItemsRecovered.ToString("N0")));
        sb.AppendLine(StatLine("Items broken:", record.ItemsBroken.ToString("N0")));
        sb.AppendLine("Most valuable item lost:");
        sb.AppendLine(record.MostValuableLostItemName == null
            ? "  None"
            : $"  {record.MostValuableLostItemName} - {record.MostValuableLostItemValue:N0}g");
        sb.AppendLine();
        sb.AppendLine(bar);

        sb.Append(finalView ? "Press any key to exit" : "Press any key to return");
        return sb.ToString();
    }

    /// <summary>Right-aligns `value` against a fixed total width, matching a ledger's look -- the label always starts flush left, the value's last character always lands on the same column no matter how long either side is.</summary>
    private static string StatLine(string label, string value)
    {
        int padding = Math.Max(1, Width - label.Length - value.Length);
        return label + new string(' ', padding) + value;
    }

    /// <summary>Hours:minutes:seconds, per the design's explicit format. Built from TotalHours rather than a "hh" custom TimeSpan format string -- TimeSpan's "hh" specifier is the Hours component (0-23) and silently wraps every 24 hours, which would misreport any run past a single day.</summary>
    internal static string FormatTimeInDungeon(long totalSeconds)
    {
        var span = TimeSpan.FromSeconds(Math.Max(0, totalSeconds));
        return $"{(int)span.TotalHours:D2}:{span.Minutes:D2}:{span.Seconds:D2}";
    }

    private static string Center(string text, int width) =>
        text.Length >= width ? text : new string(' ', (width - text.Length) / 2) + text;

    /// <summary>
    /// Every currently-owned item (carried or equipped -- sold/consumed/dropped/destroyed/fired
    /// items never qualify, since they simply aren't in either collection anymore), by highest
    /// Item.GoldValue. Ties are broken randomly on each call, per design -- opening the screen
    /// twice in a row with an unbroken tie can show a different one of the tied items each time.
    /// </summary>
    internal static (Item Item, int GoldValue) FindMostValuableItem(Player player)
    {
        var owned = player.Inventory.Items.Concat(player.GetEquippedItems()).ToList();
        if (owned.Count == 0)
        {
            return (null, 0);
        }

        int highest = owned.Max(i => i.GoldValue);
        var tied = owned.Where(i => i.GoldValue == highest).ToList();
        return (tied[TieBreakRng.Next(tied.Count)], highest);
    }
}
