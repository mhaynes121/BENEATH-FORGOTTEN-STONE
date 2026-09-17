namespace BENEATH_FORGOTTEN_STONE.Persistence;

/// <summary>Serializable mirror of Dungeon.Door.</summary>
public class DoorData
{
    public int X { get; set; }
    public int Y { get; set; }
    public bool IsLocked { get; set; }
    public bool IsPickable { get; set; }
    public bool IsBashable { get; set; }
    public int Difficulty { get; set; }
    public int MaxHealth { get; set; }
    public int Health { get; set; }
    public bool IsOpen { get; set; }
}
