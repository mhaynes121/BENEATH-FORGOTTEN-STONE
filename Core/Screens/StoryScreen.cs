namespace BENEATH_FORGOTTEN_STONE.Core.Screens;

/// <summary>
/// Reusable "read this, then press a key" narrative screen -- distinct from
/// Renderer.RenderMessage's raw pass-through in that the body re-wraps to the actual console
/// width every time (see TextWrapper), so a hand-authored paragraph never looks oddly clipped or
/// reflowed if the console isn't exactly the width it was written for. Paginated rather than
/// padded onto one overflowing screen (mirrors HelpScreen.ShowPaginated) -- a story longer than
/// the console's height shows only as many lines as actually fit, with the same footer line
/// repeated at the bottom of every page; the LAST page's keypress is what actually proceeds
/// (e.g. into character creation), not any earlier one, and nothing here ever lets the terminal
/// itself scroll the story out of view. Pagination packs whole paragraphs onto each page (see
/// BuildBlocks/Paginate) rather than chopping the flat wrapped-line list at a fixed count -- a
/// paragraph that doesn't fully fit in what's left of a page moves entirely to the next one
/// instead of being cut off mid-sentence. Currently only used for the opening "Old Kingdom"
/// story (see ShowOpeningStory), but the title/body/footer parameters are generic so a future
/// narrative interstitial can reuse Show directly instead of hand-rolling another
/// Console.Clear/WriteLine/ReadKey block.
/// </summary>
public static class StoryScreen
{
    /// <summary>
    /// Fixed rather than queried from Console.WindowWidth/WindowHeight -- ConPTY-hosted terminals
    /// (Windows Terminal chief among them, per InputHandler's own numpad doc comment and
    /// ConsoleSafety.TryFitConsole's three-exception-type guard) can misreport those, and an
    /// oversized page here isn't cosmetic the way a slightly-off HelpScreen page would be: it
    /// makes the title scroll off the top of the actual visible window before the player ever
    /// sees it. 60x24 matches the one dimension the game explicitly commits to and sets up itself
    /// (see Program.cs's ConsoleSafety.TryFitConsole(60, 24) call, and HelpScreen's own "the game
    /// defaults to a 60-column console" assumption) rather than trusting a live query that's
    /// already known to be unreliable on this game's actual target terminal.
    /// </summary>
    private const int ConsoleWidth = 60;

    private const int ConsoleHeight = 24;

    /// <summary>Rows reserved below each page's content for the blank line + footer -- see HelpScreen's identical constant.</summary>
    private const int FooterLines = 2;

    /// <summary>
    /// The game's opening narrative -- shown once, right after "Create a Character" and before
    /// name/race/class/stat selection begins. Kept here, as one named constant, rather than
    /// inline at its Program.cs call site, so it stays easy to find and edit later.
    /// </summary>
    internal const string OpeningStoryText =
        "For centuries, the old dwarven halls beneath the mountains have stood silent.\n\n" +
        "Few ventured there, and fewer had reason to. The dwarves abandoned the place long ago, sealing their gates and leaving their halls to darkness.\n\n" +
        "Until recently.\n\n" +
        "Travelers have vanished along the mountain roads. Goblins and other creatures have begun straying from the high country. There are whispers of graves found empty and of dead things walking beneath the trees.\n\n" +
        "The Crown sent soldiers to investigate.\n\n" +
        "Most never returned.\n\n" +
        "Now notices hang in every tavern and crossroads for a hundred miles. The Crown offers gold for information and proof of creatures slain within. Whatever treasure an adventurer recovers from the ruins is theirs to keep.\n\n" +
        "The wise have stayed away.\n\n" +
        "You have not.\n\n" +
        "You possess little more than the clothes you wear, a few supplies, and whatever skill fortune has given you.\n\n" +
        "Ahead stands the entrance to the Old Kingdom, its gates open once more after centuries of silence.\n\n" +
        "Whatever waits beneath that forgotten stone, you have chosen to find out.";

    public static void ShowOpeningStory() => Show("THE OLD KINGDOM", OpeningStoryText, "Press any key to continue");

    public static void Show(string title, string body, string footer)
    {
        var blocks = BuildBlocks(title, body, GetWidth());
        var pages = Paginate(blocks, GetPageSize());

        foreach (var page in pages)
        {
            var pageLines = new List<string>(page) { "", footer };

            Renderer.RenderMessage(string.Join('\n', pageLines));

            // A fresh keypress per page, consumed and discarded -- whatever key selected
            // "Create a Character" on the already-dismissed menu screen was already consumed by
            // that screen's own read, so there's nothing left over for this one to reuse, and
            // this one never passes its own keypress on to the next page or to whatever screen
            // follows the last one. Each screen/page's read blocks for and consumes exactly one
            // fresh key of its own.
            Console.ReadKey(true);
        }
    }

    /// <summary>
    /// The title and each paragraph of body, wrapped independently into "blocks" -- each block is
    /// a unit Paginate is never allowed to split across a page break, so a page break can only
    /// ever fall in the blank space BETWEEN paragraphs, never mid-sentence inside one. Body
    /// paragraphs are found by splitting on blank lines ("\n\n"), matching how OpeningStoryText
    /// (and TextWrapper.WrapText's own existing blank-line convention) already separates them.
    /// Internal so Diagnostics/SelfTest.cs can verify wrapping and block boundaries directly.
    /// </summary>
    internal static List<List<string>> BuildBlocks(string title, string body, int width)
    {
        var blocks = new List<List<string>> { new() { Center(title, width) } };
        foreach (var paragraph in body.Split("\n\n"))
        {
            blocks.Add(TextWrapper.WrapText(paragraph, width));
        }
        return blocks;
    }

    /// <summary>
    /// Packs whole blocks onto each page, oldest-first, never splitting one across a page break --
    /// a block that doesn't fit in the remaining space of a non-empty page starts a fresh page
    /// instead (see BuildBlocks). A single block longer than an entire page still gets placed on
    /// its own (otherwise-empty) page rather than looping forever trying to make it fit. Exactly
    /// one blank line separates two blocks that land on the same page. Always at least one page,
    /// even for an empty block list. Internal so Diagnostics/SelfTest.cs can verify pagination
    /// without driving Console.ReadKey.
    /// </summary>
    internal static List<List<string>> Paginate(List<List<string>> blocks, int pageSize)
    {
        int size = Math.Max(1, pageSize);
        var pages = new List<List<string>>();
        var current = new List<string>();

        foreach (var block in blocks)
        {
            int separator = current.Count > 0 ? 1 : 0;
            if (current.Count > 0 && current.Count + separator + block.Count > size)
            {
                pages.Add(current);
                current = new List<string>();
                separator = 0;
            }

            if (separator == 1)
            {
                current.Add("");
            }
            current.AddRange(block);
        }

        pages.Add(current);
        return pages;
    }

    internal static string Center(string text, int width) =>
        text.Length >= width ? text : new string(' ', (width - text.Length) / 2) + text;

    /// <summary>Reserves ScreenMargin's actual left+right margin (applied via Renderer.RenderMessage, which Show routes through) rather than this screen's own separate, now-redundant margin.</summary>
    private static int GetWidth() => ConsoleWidth - ScreenMargin.LeftMargin - ScreenMargin.RightMargin;

    private static int GetPageSize() => ConsoleHeight - FooterLines - 1;
}
