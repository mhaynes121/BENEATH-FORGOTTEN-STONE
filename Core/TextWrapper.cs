using System.Text;

namespace BENEATH_FORGOTTEN_STONE.Core;

/// <summary>
/// Word-wraps arbitrary text to a fixed width -- one reusable place
/// instead of ad hoc truncation in whatever UI component happens to
/// display a message. Explicit '\n' boundaries are preserved; each
/// resulting line is then word-wrapped independently. A single word
/// longer than the width is only ever hard-broken as a last resort.
/// </summary>
public static class TextWrapper
{
    public static List<string> WrapText(string text, int width)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(text) || width <= 0)
        {
            return result;
        }

        foreach (var rawLine in text.Split('\n'))
        {
            WrapLine(rawLine.TrimEnd('\r'), width, result);
        }

        return result;
    }

    private static void WrapLine(string line, int width, List<string> result)
    {
        var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            result.Add("");
            return;
        }

        var current = new StringBuilder();
        foreach (var rawWord in words)
        {
            string word = rawWord;

            while (word.Length > width)
            {
                // Fill the current line with whatever's already pending, then hard-break
                // the oversized word -- otherwise it would never fit on any line at all.
                if (current.Length > 0)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                result.Add(word.Substring(0, width));
                word = word.Substring(width);
            }

            int spaceIfAny = current.Length == 0 ? 0 : 1;
            if (current.Length + spaceIfAny + word.Length > width)
            {
                result.Add(current.ToString());
                current.Clear();
                spaceIfAny = 0;
            }

            if (spaceIfAny == 1)
            {
                current.Append(' ');
            }
            current.Append(word);
        }

        if (current.Length > 0)
        {
            result.Add(current.ToString());
        }
    }
}
