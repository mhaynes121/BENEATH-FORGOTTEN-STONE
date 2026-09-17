using System.Text;
using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities;

namespace BENEATH_FORGOTTEN_STONE.Core;

public static class Renderer
{
    /// <summary>
    /// Reserved rows for the (possibly multi-line, word-wrapped) status
    /// message history. GameLoop's console sizing reserves space around
    /// this constant, so the two stay in sync. Sized to comfortably fit
    /// GameLoop.MaxStatusMessages (2) logical messages even if one of them
    /// wraps to a couple of lines (e.g. a Look description).
    /// </summary>
    public const int MaxStatusMessageLines = 6;

    /// <summary>Reserved rows for the "On the ground: ..." line -- separate from message history since it's a persistent reflection of current state, not an event (see GameLoop's floor-item display, doc section 9: don't spam history).</summary>
    public const int MaxFloorItemLines = 2;

    private static readonly StringBuilder RowBuilder = new();

    /// <summary>
    /// Writes each row as runs of same-colored text instead of one
    /// Console.ForegroundColor set + one Console.Write per cell. A naive
    /// per-cell approach means up to Width*Height (1320 at 60x22) console
    /// API calls every single frame -- and this renders once per turn
    /// processed, including every monster's turn, not just the player's --
    /// so a handful of monsters all getting turns before control returns to
    /// the player multiplies that cost several times over. Batching into
    /// color runs (a typical row is a handful of runs: wall, floor, maybe
    /// one actor) cuts that by roughly two orders of magnitude with
    /// identical visual output.
    /// </summary>
    /// <param name="overlay">A transient glyph drawn on top of everything else at one tile -- used by ProjectileEngine to show an in-flight projectile without it ever becoming a real Actor. Only drawn when that tile is currently visible to the player, same as every other visibility-gated glyph here.</param>
    /// <param name="blackedOut">The Sleep command's blacked-out map (design doc: "the player, terrain, monsters, items, doors, and other map features are hidden" while the stats/message areas stay visible) -- every map row is written as blank space instead of running the normal per-tile logic below. FieldOfView/lighting still compute normally every tick regardless (see GameLoop.CheckSleepInterrupts); only the drawing is skipped, so the very next non-blacked-out render already reflects current state.</param>
    public static void Render(Level level, Player player, IReadOnlyList<MessageEntry> statusMessages,
        (int X, int Y, char Glyph, ConsoleColor Color)? overlay = null, bool blackedOut = false)
    {
        ConsoleSafety.TrySetCursorPosition(0, 0);

        if (blackedOut)
        {
            string blankRow = new string(' ', level.Width);
            for (int y = 0; y < level.Height; y++)
            {
                Console.Write(blankRow);
                Console.Write('\n');
            }
            Console.ResetColor();
            RenderStatusBar(level, player, statusMessages);
            return;
        }

        for (int y = 0; y < level.Height; y++)
        {
            ConsoleColor? runColor = null;
            RowBuilder.Clear();

            for (int x = 0; x < level.Width; x++)
            {
                var tile = level.Tiles[x, y];
                var actor = level.GetActorAt(x, y);

                char glyph;
                ConsoleColor color;

                // Corpse System: at most one glyph regardless of how many corpses share this
                // tile (proposal section 2) -- FirstOrDefault is enough, the exact one chosen
                // among several is irrelevant since they're all drawn identically.
                var corpse = tile.IsVisible ? level.Corpses.FirstOrDefault(c => c.X == x && c.Y == y) : null;
                var groundItem = tile.IsVisible ? level.GetItemAt(x, y) : null;
                var chest = tile.IsVisible ? level.GetChestAt(x, y) : null;
                // Room objects are semi-dynamic (pushable), unlike the wall/door layout, so they
                // get the same purely-visible gating as chests/traps rather than the door's
                // "remembered once seen" treatment -- a pushed boulder shouldn't leave a ghost
                // behind at a position it no longer occupies.
                var roomObject = tile.IsVisible ? level.GetRoomObjectAt(x, y) : null;
                // Doors are remembered once seen, unlike items/chests/traps -- a closed door
                // glimpsed from its lit approach stays known even from the unlit far side of a
                // dark room later (see FieldOfView.Compute's "blind observer" doc comment), the
                // same way the map's own shape is remembered rather than re-hidden the moment
                // it's no longer directly visible.
                var door = (tile.IsVisible || tile.IsExplored) ? level.GetDoorAt(x, y) : null;
                var trap = tile.IsVisible ? level.GetTrapAt(x, y) : null;

                // The player's own glyph always draws, even standing on an unilluminated dark
                // tile (design spec section 2: "the player should always know their own
                // location") -- everything else on that tile (other actors, items, terrain)
                // still needs tile.IsVisible, which darkness correctly withholds; see
                // FieldOfView.Compute's own gating.
                if (actor != null && (tile.IsVisible || actor is Player))
                {
                    glyph = actor.Symbol;
                    color = actor.Color;
                }
                else if (corpse != null)
                {
                    glyph = CorpseItemFactory.Glyph;
                    color = ConsoleColor.Gray;
                }
                else if (groundItem != null)
                {
                    glyph = groundItem.Symbol;
                    color = ConsoleColor.Cyan;
                }
                else if (chest != null)
                {
                    glyph = chest.IsOpen ? '-' : '=';
                    color = ConsoleColor.Yellow;
                }
                else if (roomObject != null)
                {
                    glyph = roomObject.Symbol;
                    color = roomObject.Color;
                }
                else if (door != null)
                {
                    glyph = '+';
                    color = tile.IsVisible ? ConsoleColor.DarkYellow : ConsoleColor.DarkGray;
                }
                else if (trap != null && trap.IsRevealed)
                {
                    glyph = '^';
                    color = ConsoleColor.Red;
                }
                else if (tile.IsVisible)
                {
                    if (tile.Type == TileType.Floor)
                    {
                        // FloorType drives a plain floor tile's look, not the reverse -- see
                        // Tile.FloorType's own doc comment on why nothing may branch on Symbol.
                        var floorDefinition = FloorTypeCatalog.Get(tile.FloorType);
                        glyph = floorDefinition.DisplayCharacter;
                        color = floorDefinition.DisplayColor;
                    }
                    else
                    {
                        glyph = tile.Symbol;
                        color = tile.Type == TileType.Wall ? ConsoleColor.Gray : ConsoleColor.White;
                    }
                }
                else if (tile.IsExplored)
                {
                    glyph = tile.Symbol;
                    color = ConsoleColor.DarkGray;
                }
                else
                {
                    glyph = ' ';
                    color = ConsoleColor.Black;
                }

                if (tile.IsVisible && overlay.HasValue && overlay.Value.X == x && overlay.Value.Y == y)
                {
                    glyph = overlay.Value.Glyph;
                    color = overlay.Value.Color;
                }

                if (runColor != color)
                {
                    if (runColor.HasValue)
                    {
                        Console.ForegroundColor = runColor.Value;
                        Console.Write(RowBuilder.ToString());
                        RowBuilder.Clear();
                    }
                    runColor = color;
                }
                RowBuilder.Append(glyph);
            }

            if (runColor.HasValue)
            {
                Console.ForegroundColor = runColor.Value;
                Console.Write(RowBuilder.ToString());
            }
            Console.Write('\n');
        }

        Console.ResetColor();
        RenderStatusBar(level, player, statusMessages);
    }

    private static void RenderStatusBar(Level level, Player player, IReadOnlyList<MessageEntry> statusMessages)
    {
        // Reserves ScreenMargin's left+right margin on top of the raw window width -- the status
        // bar is drawn via explicit per-row cursor positioning (not natural Console.WriteLine
        // flow), so it needs both this narrower width AND the left column shift below, unlike
        // every other screen, which gets its left margin for free from ScreenMargin.
        int width;
        try
        {
            width = Math.Max(20, Console.WindowWidth - ScreenMargin.LeftMargin - ScreenMargin.RightMargin);
        }
        catch (IOException)
        {
            width = 72;
        }

        int ClampRow(int row)
        {
            try
            {
                return Math.Min(row, Console.WindowHeight - 1);
            }
            catch (IOException)
            {
                return row;
            }
        }

        string statsLine =
            $"HP: {player.Health.Current}/{player.Health.Max}   " +
            $"MP: {player.Mana.Current}/{player.Mana.Max}   " +
            $"{player.Experience.Current}/{player.Experience.ToNextLevel} Experience   " +
            $"Gold: {player.Gold}   " +
            $"Floor: {level.FloorIndex}";

        int statsRow = level.Height + 1;
        int floorItemFirstRow = level.Height + 2;
        int messageFirstRow = floorItemFirstRow + MaxFloorItemLines;

        ConsoleSafety.TrySetCursorPosition(ScreenMargin.LeftMargin, ClampRow(statsRow));
        Console.Write(Fit(statsLine, width));

        // Persistent reflection of the player's current tile, not a status event -- recomputed
        // fresh every frame so it never gets pushed into (or evicts) the message history.
        // A chest on the player's own tile is otherwise invisible on the map (the player's
        // own glyph always wins that cell), so it's reported here the same way floor items are.
        // Both are withheld entirely while standing on an unilluminated dark tile (design spec
        // section 12/13) -- the player can still feel around for them (HandlePickUp/HandleOpenChest
        // still work), this HUD line just can't tell them what's there before they do.
        var tileHere = level.Tiles[player.X, player.Y];
        bool hiddenByDarkness = tileHere.IsDarkRoom && !tileHere.IsIlluminated;

        var floorItems = level.GetItemsAt(player.X, player.Y);
        var floorItemParts = new List<string>();
        if (!hiddenByDarkness && floorItems.Count > 0)
        {
            floorItemParts.Add("On the ground: " + string.Join(", ", floorItems.Select(i => i.DisplayName)));
        }
        var chestHere = level.GetChestAt(player.X, player.Y);
        if (!hiddenByDarkness && chestHere != null)
        {
            floorItemParts.Add(chestHere.IsOpen ? "There's an open chest here."
                : chestHere.IsLocked ? "There's a locked chest here."
                : "There's a closed chest here.");
        }
        string floorItemText = string.Join("  ", floorItemParts);
        var floorItemLines = TextWrapper.WrapText(floorItemText, width);
        for (int i = 0; i < MaxFloorItemLines; i++)
        {
            string line = i < floorItemLines.Count ? floorItemLines[i] : "";
            ConsoleSafety.TrySetCursorPosition(ScreenMargin.LeftMargin, ClampRow(floorItemFirstRow + i));
            Console.Write(Fit(line, width));
        }

        var wrappedLines = BuildWrappedColoredLines(statusMessages, width, MaxStatusMessageLines);

        for (int i = 0; i < MaxStatusMessageLines; i++)
        {
            ConsoleSafety.TrySetCursorPosition(ScreenMargin.LeftMargin, ClampRow(messageFirstRow + i));
            if (i < wrappedLines.Count)
            {
                Console.ForegroundColor = wrappedLines[i].Color;
                Console.Write(Fit(wrappedLines[i].Text, width));
                Console.ResetColor();
            }
            else
            {
                Console.Write(Fit("", width));
            }
        }
    }

    /// <summary>
    /// Word-wraps each message independently (oldest first), keeping every resulting line
    /// tagged with its source message's color, then drops the oldest wrapped lines first if the
    /// total overflows maxLines -- so the most recent message is always fully visible. Extracted
    /// from RenderStatusBar (internal, not private) so Diagnostics/SelfTest.cs can verify the
    /// Ability Proficiency System's yellow lines survive wrapping without needing to capture
    /// actual Console output.
    /// </summary>
    internal static List<(string Text, ConsoleColor Color)> BuildWrappedColoredLines(
        IReadOnlyList<MessageEntry> statusMessages, int width, int maxLines)
    {
        var wrappedLines = new List<(string Text, ConsoleColor Color)>();
        foreach (var message in statusMessages)
        {
            foreach (var wrapped in TextWrapper.WrapText(message.Text, width))
            {
                wrappedLines.Add((wrapped, message.Color));
            }
        }
        if (wrappedLines.Count > maxLines)
        {
            wrappedLines = wrappedLines.Skip(wrappedLines.Count - maxLines).ToList();
        }
        return wrappedLines;
    }

    private static string Fit(string text, int width) =>
        text.Length > width ? text.Substring(0, width) : text.PadRight(width);

    public static void RenderMessage(string message)
    {
        using var margin = new ScreenMargin();
        ConsoleSafety.TryClear();
        ConsoleSafety.TrySetCursorPosition(0, 0);
        Console.WriteLine(message);
    }

    /// <summary>Same full-screen shape as RenderMessage, but each line keeps its own color -- see MessageHistoryScreen, which needs the Ability Proficiency System's yellow lines to stay yellow in the scrollback view too.</summary>
    public static void RenderColoredMessage(IEnumerable<(string Text, ConsoleColor Color)> lines)
    {
        using var margin = new ScreenMargin();
        ConsoleSafety.TryClear();
        ConsoleSafety.TrySetCursorPosition(0, 0);
        foreach (var (text, color) in lines)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
        }
        Console.ResetColor();
    }
}
