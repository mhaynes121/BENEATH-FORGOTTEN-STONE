namespace BENEATH_FORGOTTEN_STONE.Core.Screens;

public enum MainMenuOption
{
    CreateCharacter,
    LoadGame,
    Graveyard,
    Credits,
    Exit
}

/// <summary>First screen shown on launch (and again after returning from a game) -- title, subtitle, and the numbered menu together in one screen, with no separate "press any key" gate in front of it.</summary>
public static class MainMenuScreen
{
    private const string GameTitle = "BENEATH FORGOTTEN STONE";
    private const string Subtitle = "The deep remembers.";

    /// <summary>Matches HelpScreen's own documented assumption -- the game defaults to a 60-column console, so centering against a fixed width here looks right without needing to query Console.WindowWidth (which the main menu, unlike StoryScreen, has no other reason to touch). Reserves ScreenMargin's left+right margin (applied via MenuPrompt.Choose, which Show routes through) the same way StoryScreen.GetWidth does.</summary>
    private const int ConsoleWidth = 60 - ScreenMargin.LeftMargin - ScreenMargin.RightMargin;

    private static readonly string[] OptionLabels =
    {
        "Create a Character",
        "Load Saved Game",
        "View Top 10 Deepest Deaths",
        "Credits",
        "Exit Game"
    };

    public static MainMenuOption Show()
    {
        int index = MenuPrompt.Choose(BuildTitleBlock(), OptionLabels).Value;
        return (MainMenuOption)index;
    }

    /// <summary>
    /// Internal (not private) so Diagnostics/SelfTest.cs can verify the title/subtitle text and
    /// centering directly, without needing to drive MenuPrompt's Console.ReadKey loop. The
    /// trailing "\n" after the subtitle is deliberate, not a stray newline -- Console.WriteLine
    /// always appends one more line terminator after whatever string it's given, and
    /// MenuPrompt.Choose already writes a further blank line of its own before the numbered
    /// options begin. Together those three add up to exactly one blank line between the title
    /// and subtitle and two between the subtitle and the menu, matching the intended layout.
    /// </summary>
    internal static string BuildTitleBlock() => $"{Center(GameTitle, ConsoleWidth)}\n\n{Center(Subtitle, ConsoleWidth)}\n";

    internal static string Center(string text, int width) =>
        text.Length >= width ? text : new string(' ', (width - text.Length) / 2) + text;
}
