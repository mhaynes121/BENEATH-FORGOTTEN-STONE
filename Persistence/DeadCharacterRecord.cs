namespace BENEATH_FORGOTTEN_STONE.Persistence;

public class DeadCharacterRecord
{
    public string Name { get; set; }
    public string ClassName { get; set; }
    public string RaceName { get; set; }
    public int Level { get; set; }
    public long TurnCount { get; set; }
    public int FloorReached { get; set; }
    public DateTime DiedAtUtc { get; set; }

    /// <summary>What killed the character, e.g. "an orc's physical attack" or "a goblin shaman casting Fireball" -- see Actor.LastDamageSource. No "was killed by" prefix; that's added at display time.</summary>
    public string CauseOfDeath { get; set; }
}
