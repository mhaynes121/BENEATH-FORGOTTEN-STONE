namespace BENEATH_FORGOTTEN_STONE.Core.Screens;

/// <summary>
/// Shared "numbered list, press a key to choose" interaction used by the
/// main menu, class/race pickers, spell casting, and equipment slot
/// selection. Supports up to 35 options (1-9 then a-z) -- a fully-geared
/// character has up to 12 equipment slots, already past the old 1-9 cap.
/// </summary>
public static class MenuPrompt
{
    /// <summary>Returned by Choose when fixedKey was pressed instead of a numbered option -- see fixedKey's own doc comment.</summary>
    public const int FixedKeyIndex = -1;

    /// <summary>
    /// Options beyond this many switch from a single column to a multi-column, column-major
    /// layout (see RenderOptionLines) -- the same threshold and technique TraderScreen.RenderColumns
    /// already established for its own Buy/Sell lists, applied here too so a long list from any
    /// Choose caller (many spells, a full inventory to drop/unequip/inspect, ...) never scrolls
    /// past the console's own height instead of just fitting on screen.
    /// </summary>
    private const int MaxRowsPerColumn = 16;

    /// <param name="allowCancel">When true, Esc returns null instead of looping forever waiting for a valid selection -- for menus reachable by an accidental keypress (cast, pick up, drop, learn spell, unequip, examine, ...) where forcing a choice would be a trap. Defaults to false so required selections (race, class, main menu) keep their exact existing behavior -- Esc simply doesn't match anything and re-prompts, same as any other stray key.</param>
    /// <param name="fixedKey">
    /// An extra option (e.g. HandlePickUp's "Get All") bound to one unchanging letter regardless
    /// of how many numbered options precede it -- unlike appending it as one more label, whose
    /// own key (1-9 then a-z) would otherwise shift depending on the list's length. Returns
    /// FixedKeyIndex when pressed. Checked before the numbered range, so on the rare tile with
    /// 16+ items, the specific item that would have landed on this same letter (e.g. the 16th,
    /// on 'g') becomes unreachable by letter -- an accepted trade-off for a binding simple enough
    /// to never need remembering "which key means Get All this time."
    /// </param>
    public static int? Choose(string title, IReadOnlyList<string> optionLabels, bool allowCancel = false,
        char? fixedKey = null, string fixedKeyLabel = null)
    {
        using var margin = new ScreenMargin();
        while (true)
        {
            ConsoleSafety.TryClear();
            ConsoleSafety.TrySetCursorPosition(0, 0);
            Console.WriteLine(title);
            Console.WriteLine();

            var optionLines = new List<string>();
            for (int i = 0; i < optionLabels.Count; i++)
            {
                optionLines.Add($"{OptionKey(i)}. {optionLabels[i]}");
            }
            if (fixedKey.HasValue)
            {
                optionLines.Add($"{char.ToUpperInvariant(fixedKey.Value)}. {fixedKeyLabel}");
            }
            RenderOptionLines(optionLines);

            Console.WriteLine();
            if (allowCancel)
            {
                Console.WriteLine("Esc. Cancel");
                Console.WriteLine();
            }
            Console.Write("> ");

            var key = Console.ReadKey(intercept: true);
            if (allowCancel && key.Key == ConsoleKey.Escape)
            {
                // Same reasoning as the successful-selection Clear() below -- whatever
                // redraws next only overwrites its own footprint.
                ConsoleSafety.TryClear();
                return null;
            }

            char keyChar = char.ToLowerInvariant(key.KeyChar);
            if (fixedKey.HasValue && keyChar == char.ToLowerInvariant(fixedKey.Value))
            {
                ConsoleSafety.TryClear();
                return FixedKeyIndex;
            }

            int index = IndexFromKey(keyChar);
            if (index >= 0 && index < optionLabels.Count)
            {
                // Whatever redraws next (Renderer.Render, another screen) only overwrites its own
                // footprint, not the whole console -- leaving this menu's text uncleared behind it
                // would leave stray characters on screen, same class of bug fixed for InventoryScreen/HelpScreen.
                ConsoleSafety.TryClear();
                return index;
            }
        }
    }

    /// <summary>Exposed for Diagnostics/SelfTest.cs -- a pure function of the line count, same reasoning TraderScreen.ColumnCountFor is internal for (and the same formula -- MaxRowsPerColumn just happens to match).</summary>
    internal static int ColumnCountFor(int lineCount) => (int)Math.Ceiling(lineCount / (double)MaxRowsPerColumn);

    /// <summary>
    /// A single column, one line per entry, for anything at or under MaxRowsPerColumn -- byte-
    /// identical to this method's own previous behavior, so every existing short-list caller
    /// (class/race pickers, a typical spell/skill list, ...) looks exactly as it always has.
    /// Past that, wraps column-major (the first column fills top-to-bottom before the next one
    /// starts) into as many columns as needed, same technique as TraderScreen.RenderColumns.
    /// </summary>
    private static void RenderOptionLines(IReadOnlyList<string> lines)
    {
        if (lines.Count == 0)
        {
            return;
        }
        if (lines.Count <= MaxRowsPerColumn)
        {
            foreach (var line in lines)
            {
                Console.WriteLine(line);
            }
            return;
        }

        int columnCount = ColumnCountFor(lines.Count);
        int columnWidth = lines.Max(l => l.Length) + 3;

        for (int row = 0; row < MaxRowsPerColumn; row++)
        {
            bool wroteAnything = false;
            for (int col = 0; col < columnCount; col++)
            {
                int index = (col * MaxRowsPerColumn) + row;
                if (index >= lines.Count)
                {
                    continue;
                }

                wroteAnything = true;
                Console.Write(col == columnCount - 1 ? lines[index] : lines[index].PadRight(columnWidth));
            }

            if (wroteAnything)
            {
                Console.WriteLine();
            }
        }
    }

    /// <summary>The label shown for the option at this index: "1".."9" then "a".."z".</summary>
    public static string OptionKey(int index) => index < 9 ? (index + 1).ToString() : ((char)('a' + (index - 9))).ToString();

    /// <summary>Internal (not private) so TraderScreen/ContainerScreen -- which read keys outside Choose's own loop for their own multi-section screens -- share this exact digit/letter-to-index mapping instead of each maintaining their own (in-practice-diverging) copy. Callers must lowercase `keyChar` themselves first, same as Choose does internally, so a single normalization point covers everything.</summary>
    /// <returns>-1 if the key doesn't map to any option.</returns>
    internal static int IndexFromKey(char keyChar)
    {
        if (keyChar >= '1' && keyChar <= '9')
        {
            return keyChar - '1';
        }
        if (keyChar >= 'a' && keyChar <= 'z')
        {
            return 9 + (keyChar - 'a');
        }
        return -1;
    }
}
