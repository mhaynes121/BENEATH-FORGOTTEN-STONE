namespace BENEATH_FORGOTTEN_STONE.Core.Screens;

public static class CreditsScreen
{
    /// <summary>
    /// Major.Minor.Sub-minor, bumped on every change: Major for a new
    /// feature, Minor for modifying an existing feature, Sub-minor for a
    /// pure value/balance tweak. Each bump resets the tiers below it
    /// (e.g. a new feature also resets Minor and Sub-minor to 0). Lives here
    /// (not a standalone TitleScreen) since the title/menu are now one
    /// combined screen with no separate splash to hang a version line on --
    /// see MainMenuScreen.
    /// </summary>
    private const string Version = "Version: 38.3.0";

    public static void Show()
    {
        Renderer.RenderMessage(
            "=== Credits ===\n\n" +
            "Programming & Story: Michael Haynes\n\n" +
            "Original Score Composer: Malachy I. Sheen\n" +
            "Audio Designer: Michaela Hynes\n" +
            "Video Editor: Yasmine H. Leach\n" +
            "Script Writer: Ishmael Chaney\n" +
            "Lead Playtester: Amani H. Chesley\n\n" +
            $"{Version}\n\n" +
            "Press any key to return...");
        Console.ReadKey(true);
    }
}
