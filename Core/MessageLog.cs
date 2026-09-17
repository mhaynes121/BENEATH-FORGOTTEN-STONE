namespace BENEATH_FORGOTTEN_STONE.Core;

/// <summary>
/// One retained status/flavor/combat line plus the color it should render in -- White for
/// ordinary messages, Yellow for the Ability Proficiency System's lucky-insight/rank-up/Mastery
/// lines (see ProficiencyTracker). Carrying color alongside the text (rather than a parallel
/// list) is what lets RenderStatusBar/RenderColoredMessage bracket each wrapped line with the
/// right Console.ForegroundColor without losing track of which original message it came from.
/// </summary>
public sealed class MessageEntry
{
    public string Text { get; }
    public ConsoleColor Color { get; }

    public MessageEntry(string text, ConsoleColor color = ConsoleColor.White)
    {
        Text = text;
        Color = color;
    }
}

/// <summary>
/// Centralized status-message history -- every status/flavor/combat line the player sees passes
/// through Add exactly once, rather than each call site managing its own capped list (GameLoop
/// used to keep a bare List&lt;string&gt; capped inline). Renderer only ever shows the most recent
/// VisibleLines each turn; the full History caps at MaxHistory so the 'm' message-history screen
/// (see MessageHistoryScreen) can show more context than fits on the map without retaining every
/// message the game has ever printed.
/// </summary>
public class MessageLog
{
    /// <summary>How many of the most recent messages Renderer draws under the map each turn.</summary>
    public const int VisibleLines = 3;

    private const int MaxHistory = 20;

    private readonly List<MessageEntry> history = new();

    /// <summary>Full retained history, oldest first, capped at MaxHistory -- see MessageHistoryScreen.</summary>
    public IReadOnlyList<MessageEntry> History => history;

    public void Add(string message, ConsoleColor color = ConsoleColor.White)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        history.Add(new MessageEntry(message, color));
        if (history.Count > MaxHistory)
        {
            history.RemoveAt(0);
        }
    }

    /// <summary>The most recent `count` messages, oldest first -- what Renderer/RenderWithTransientMessage actually display each turn.</summary>
    public IReadOnlyList<MessageEntry> GetRecent(int count) =>
        history.Skip(Math.Max(0, history.Count - count)).ToList();
}
