namespace BENEATH_FORGOTTEN_STONE.Persistence;

/// <summary>
/// Serializable mirror of Entities.AdventureRecord. Deliberately excludes the most valuable
/// currently-owned item -- that's derived fresh from live Inventory/Equipment every time the
/// Adventure Record screen opens (see AdventureRecordScreen), never stored.
/// </summary>
public class AdventureRecordData
{
    public long MonstersKilled { get; set; }
    public long DamageDealt { get; set; }
    public long DamageTaken { get; set; }
    public long GoldCollected { get; set; }
    public long GoldSpent { get; set; }
    public long BossesKilled { get; set; }
    public string HighestLevelBossName { get; set; }
    public int HighestLevelBossLevel { get; set; }
    public int DeepestFloorReached { get; set; }
    public long TimeInDungeonSeconds { get; set; }
    public long ItemsPermanentlyLost { get; set; }
    public long ItemsMisplaced { get; set; }
    public long MisplacedItemsRecovered { get; set; }
    public string MostValuableLostItemName { get; set; }
    public int MostValuableLostItemValue { get; set; }
    public long ItemsBroken { get; set; }
}
