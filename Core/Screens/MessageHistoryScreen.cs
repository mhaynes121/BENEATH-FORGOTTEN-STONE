namespace BENEATH_FORGOTTEN_STONE.Core.Screens;

/// <summary>Full-history view opened with 'm' -- shows every retained message (MessageLog.History, capped at 20), not just the few lines Renderer keeps visible under the map. Never consumes a turn, same as HelpScreen/InventoryScreen.</summary>
public static class MessageHistoryScreen
{
    public static void Show(IReadOnlyList<MessageEntry> history)
    {
        var lines = new List<(string Text, ConsoleColor Color)>
        {
            ("=== Message History ===", ConsoleColor.White),
            ("", ConsoleColor.White)
        };

        if (history.Count == 0)
        {
            lines.Add(("Nothing has happened yet.", ConsoleColor.White));
        }
        else
        {
            foreach (var message in history)
            {
                lines.Add((message.Text, message.Color));
            }
        }

        lines.Add(("", ConsoleColor.White));
        lines.Add(("Press any key to return...", ConsoleColor.White));

        Renderer.RenderColoredMessage(lines);
        Console.ReadKey(true);

        // Renderer.Render only redraws the map's own footprint -- this screen's wider/taller
        // text (up to 20 retained messages, plus header/footer) would otherwise leave stray
        // characters behind once the game view resumes, same as HelpScreen's own exit clear.
        ConsoleSafety.TryClear();
    }
}
