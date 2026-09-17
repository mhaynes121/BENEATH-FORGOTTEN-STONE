using System.Text.Json;

namespace BENEATH_FORGOTTEN_STONE.Persistence;

/// <summary>Permanent record of the deepest deaths across saves -- a top-10 list by dungeon floor reached, not a full history of every death.</summary>
public static class GraveyardManager
{
    private const string GraveyardFileName = "graveyard.json";

    /// <summary>Only this many of the deepest deaths are ever kept -- see TrimToDeepest.</summary>
    internal const int MaxEntries = 10;

    public static void Add(DeadCharacterRecord record)
    {
        var records = GetAll();
        records.Add(record);
        var trimmed = TrimToDeepest(records);

        var json = JsonSerializer.Serialize(trimmed, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(GraveyardFileName, json);
    }

    /// <summary>
    /// Keeps only the MaxEntries deaths that reached the deepest floor, ties broken by most
    /// recent -- split out from Add so this ranking/trimming logic is exercisable directly
    /// against in-memory records, without touching the real graveyard.json save file (see
    /// Diagnostics/SelfTest.cs).
    /// </summary>
    internal static List<DeadCharacterRecord> TrimToDeepest(List<DeadCharacterRecord> records) =>
        records
            .OrderByDescending(r => r.FloorReached)
            .ThenByDescending(r => r.DiedAtUtc)
            .Take(MaxEntries)
            .ToList();

    /// <returns>The current top-10 list, deepest floor first -- already in that order on disk (Add always writes it pre-sorted/trimmed), so this never needs to re-sort.</returns>
    public static List<DeadCharacterRecord> GetAll()
    {
        if (!File.Exists(GraveyardFileName))
        {
            return new List<DeadCharacterRecord>();
        }

        var json = File.ReadAllText(GraveyardFileName);
        return JsonSerializer.Deserialize<List<DeadCharacterRecord>>(json) ?? new List<DeadCharacterRecord>();
    }
}
