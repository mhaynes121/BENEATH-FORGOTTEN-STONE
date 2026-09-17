namespace BENEATH_FORGOTTEN_STONE.Core.Screens;

/// <summary>
/// A typed numeric quantity prompt -- unlike MenuPrompt.Choose's one-key-per-option list (capped
/// at 35 selectable rows), this reads actual digit keys one at a time, so a stack of any size (a
/// large bundle of arrows, say) can still be split by an exact count without needing one
/// selectable row per unit. Backspace edits the typed digits, Enter confirms, Esc cancels.
/// Pressing Enter with nothing typed returns `max` directly -- the common "just take/sell all of
/// it" fast path -- rather than forcing the player to type the exact count every time.
/// </summary>
public static class NumericPrompt
{
    /// <returns>A value from 1 to `max`, or null if the player cancelled with Esc.</returns>
    public static int? ReadQuantity(string title, int max)
    {
        using var margin = new ScreenMargin();
        var buffer = new System.Text.StringBuilder();
        int maxDigits = max.ToString().Length;

        while (true)
        {
            ConsoleSafety.TryClear();
            ConsoleSafety.TrySetCursorPosition(0, 0);
            Console.WriteLine(title);
            Console.WriteLine();
            Console.WriteLine($"How many? (1-{max}, Enter for all, Esc to cancel)");
            Console.Write($"> {buffer}");

            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Escape)
            {
                ConsoleSafety.TryClear();
                return null;
            }
            if (key.Key == ConsoleKey.Enter)
            {
                ConsoleSafety.TryClear();
                if (buffer.Length == 0)
                {
                    return max;
                }
                return Math.Clamp(int.Parse(buffer.ToString()), 1, max);
            }
            if (key.Key == ConsoleKey.Backspace)
            {
                if (buffer.Length > 0)
                {
                    buffer.Length--;
                }
                continue;
            }
            if (char.IsDigit(key.KeyChar) && buffer.Length < maxDigits)
            {
                buffer.Append(key.KeyChar);
            }
        }
    }
}
