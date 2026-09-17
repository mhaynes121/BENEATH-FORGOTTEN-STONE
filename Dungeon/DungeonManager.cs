using BENEATH_FORGOTTEN_STONE.Entities;

namespace BENEATH_FORGOTTEN_STONE.Dungeon;

/// <summary>
/// Owns every floor the player has visited this session and handles
/// transitions between them.
///
/// This implements "Approach A" from the design notes: all visited
/// floors are kept in memory (Dictionary&lt;int, Level&gt;) rather than
/// serialized to disk on transition. Memory cost is negligible at
/// roguelike dungeon scale, and it defers the harder serialization
/// design until the tile/entity schema has stabilized. Off-screen
/// floors are implicitly "frozen" -- their Scheduler is simply never
/// ticked while they're not CurrentLevel, matching classic NetHack-ish
/// conventions where monsters on floors you're not on don't act.
/// </summary>
public class DungeonManager
{
    private const int LevelWidth = 60;
    private const int LevelHeight = 22;

    private readonly Dictionary<int, Level> floors = new();
    private readonly Random rng;

    public int CurrentFloorIndex { get; private set; }
    public Level CurrentLevel => floors[CurrentFloorIndex];

    /// <summary>Consecutive floors generated (not just visited) without a Trader -- see GetOrGenerateFloor and TraderConfig.MaximumLevelsWithoutTrader. Reset to 0 whenever a generated floor turns out to have one, incremented otherwise; never touched when revisiting an already-generated floor.</summary>
    public int LevelsSinceLastTrader { get; private set; }

    /// <summary>Every floor visited so far, keyed by index -- SaveManager.Save enumerates this to persist all of them, not just the current one.</summary>
    public IReadOnlyDictionary<int, Level> Floors => floors;

    public DungeonManager(Random rng)
    {
        this.rng = rng;
    }

    /// <summary>Seeds a previously-visited floor (reconstructed from a save) so GetOrGenerateFloor picks it up instead of generating fresh. Must be called before EnterFloor for that index.</summary>
    public void RestoreFloor(int index, Level level) => floors[index] = level;

    /// <summary>Restores the cross-floor trader-frequency counter from a save -- see LevelsSinceLastTrader.</summary>
    public void RestoreTraderCounter(int value) => LevelsSinceLastTrader = value;

    public void EnterFirstFloor(Player player)
    {
        CurrentFloorIndex = 1;
        var level = GetOrGenerateFloor(1);
        PlaceActorAt(level, player, level.StairsUpPosition);
    }

    /// <summary>
    /// Enters a floor restored from a save. If the floor was seeded via
    /// RestoreFloor, it's used as-is (exactly as left); otherwise it's
    /// generated fresh, same as always. resumePosition places the player
    /// at their exact last position instead of the stairs, falling back
    /// to StairsUpPosition when absent or no longer walkable (a safety
    /// net for legacy/corrupted saves).
    /// </summary>
    public void EnterFloor(Player player, int floorIndex, (int X, int Y)? resumePosition = null)
    {
        CurrentFloorIndex = floorIndex;
        var level = GetOrGenerateFloor(floorIndex);
        var position = resumePosition is { } pos && level.IsWalkable(pos.X, pos.Y) ? pos : level.StairsUpPosition;
        PlaceActorAt(level, player, position);
    }

    public void Descend(Player player)
    {
        RemoveFromCurrentLevel(player);
        CurrentFloorIndex++;
        var level = GetOrGenerateFloor(CurrentFloorIndex);
        PlaceActorAt(level, player, level.StairsUpPosition);
    }

    public void Ascend(Player player)
    {
        if (CurrentFloorIndex == 1)
        {
            return;
        }

        RemoveFromCurrentLevel(player);
        CurrentFloorIndex--;
        var level = GetOrGenerateFloor(CurrentFloorIndex);
        PlaceActorAt(level, player, level.StairsDownPosition);
    }

    private Level GetOrGenerateFloor(int index)
    {
        if (!floors.TryGetValue(index, out var level))
        {
            bool forceTrader = LevelsSinceLastTrader >= TraderConfig.MaximumLevelsWithoutTrader;
            level = DungeonGenerator.Generate(index, LevelWidth, LevelHeight, rng, difficultyLevel: index, forceTraderSpawn: forceTrader);
            floors[index] = level;

            if (level.Actors.OfType<Trader>().Any())
            {
                LevelsSinceLastTrader = 0;
            }
            else
            {
                LevelsSinceLastTrader++;
            }
        }
        return level;
    }

    private void RemoveFromCurrentLevel(Player player)
    {
        var level = CurrentLevel;
        level.Actors.Remove(player);
        level.Scheduler.Unregister(player);
    }

    private void PlaceActorAt(Level level, Player player, (int X, int Y) position)
    {
        player.MoveTo(position.X, position.Y);

        // Adventure Record: the one place every floor entry funnels through -- a new game
        // (EnterFirstFloor), a resumed save (EnterFloor), and both Descend/Ascend all call this,
        // so a single Math.Max here covers every "update the deepest floor reached" location the
        // design calls for at once. Never assigned directly, so ascending back toward floor 1
        // can never lower it.
        player.AdventureRecord.DeepestFloorReached = Math.Max(player.AdventureRecord.DeepestFloorReached, level.FloorIndex);

        if (!level.Actors.Contains(player))
        {
            level.Actors.Add(player);
            level.Scheduler.Register(player);
        }
    }
}
