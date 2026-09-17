namespace BENEATH_FORGOTTEN_STONE.Persistence;

/// <summary>Serializable mirror of Dungeon.Trap.</summary>
public class TrapData
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Damage { get; set; }
    public bool IsRevealed { get; set; }
    public bool IsTriggered { get; set; }
}
