using System.Text;
using BENEATH_FORGOTTEN_STONE.Persistence;

namespace BENEATH_FORGOTTEN_STONE.Core.Screens;

public static class GraveyardScreen
{
    public static void Show()
    {
        var records = GraveyardManager.GetAll();

        string text;
        if (records.Count == 0)
        {
            text = "=== Top 10 Deepest Deaths ===\n\nNo one has died yet.\n\nPress any key to return...";
        }
        else
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Top 10 Deepest Deaths ===");
            sb.AppendLine();
            // Already deepest-floor-first on disk -- see GraveyardManager.TrimToDeepest.
            for (int i = 0; i < records.Count; i++)
            {
                var record = records[i];
                string causeOfDeath = string.IsNullOrEmpty(record.CauseOfDeath) ? "unknown causes" : record.CauseOfDeath;
                sb.AppendLine(
                    $"{i + 1,2}. {record.Name} the {record.RaceName} {record.ClassName} " +
                    $"- Level {record.Level} ({record.TurnCount} turns), died on floor {record.FloorReached} ({record.DiedAtUtc:yyyy-MM-dd}), " +
                    $"was killed by {causeOfDeath}.");
            }
            sb.AppendLine();
            sb.Append("Press any key to return...");
            text = sb.ToString();
        }

        Renderer.RenderMessage(text);
        Console.ReadKey(true);
    }
}
