namespace BENEATH_FORGOTTEN_STONE.Dungeon;

/// <summary>
/// A door blocking a corridor tile -- placed by DungeonGenerator at genuine
/// room entrances (see FindRoomEntrance). Like Chest, not every
/// instance is actually locked -- see IsLocked. An unlocked one still
/// blocks the tile and still needs one bump to walk through (so it reads
/// as a real door, not indistinguishable open floor), but that bump always
/// succeeds for free, no roll/skill/key involved. Kept as a Level-level
/// overlay list, same reasoning as Chest/Trap, and enforced as
/// impassable by both GameLoop.HandleMove (the player) and
/// ChaseAI.TryMove (monsters) until IsOpen.
/// </summary>
public class Door
{
    public int X { get; }
    public int Y { get; }

    public bool IsLocked { get; set; }

    /// <summary>Whether SkillCatalog.PickLock can be attempted on this door at all -- false means picking is never offered, regardless of skill. Meaningless when IsLocked is false.</summary>
    public bool IsPickable { get; }

    /// <summary>Whether bumping into this door (anyone, no skill required) chips away at its Health -- false means it can never be broken down. Meaningless when IsLocked is false.</summary>
    public bool IsBashable { get; }

    /// <summary>Compared against a Knowledge-based roll -- see GameLoop's door-pick flow. Meaningless when IsLocked is false.</summary>
    public int Difficulty { get; }

    public int MaxHealth { get; }
    public int Health { get; set; }

    /// <summary>Once true, GetDoorAt no longer returns this door -- both HandleMove and ChaseAI.TryMove treat that as "just floor" from then on.</summary>
    public bool IsOpen { get; set; }

    public Door(int x, int y, bool isLocked, bool isPickable, bool isBashable, int difficulty, int maxHealth)
    {
        X = x;
        Y = y;
        IsLocked = isLocked;
        IsPickable = isPickable;
        IsBashable = isBashable;
        Difficulty = difficulty;
        MaxHealth = maxHealth;
        Health = maxHealth;
    }
}
