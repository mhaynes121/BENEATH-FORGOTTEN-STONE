namespace BENEATH_FORGOTTEN_STONE.Dungeon;

/// <summary>
/// A hidden hazard placed by DungeonGenerator.SpawnTraps -- damages whoever
/// steps on it, revealed or not (Detect Traps only makes it visible, it
/// doesn't disarm it). Kept as a Level-level overlay list rather than a new
/// Tile subtype, same reasoning as Chest.
/// </summary>
public class Trap
{
    public int X { get; }
    public int Y { get; }
    public int Damage { get; }

    /// <summary>Whether this trap is currently drawn on the map -- see GameLoop's Detect Traps auto-reveal and Renderer's glyph priority. Purely cosmetic; doesn't affect whether stepping on it triggers.</summary>
    public bool IsRevealed { get; set; }

    public bool IsTriggered { get; set; }

    public Trap(int x, int y, int damage)
    {
        X = x;
        Y = y;
        Damage = damage;
    }
}
