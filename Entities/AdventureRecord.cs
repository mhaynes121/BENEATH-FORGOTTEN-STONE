namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Lifetime run statistics for one character -- kept as its own small class rather than more
/// loose counters directly on Player, mirroring how ExperienceComponent/EquipmentComponent are
/// already split out. `long` for every cumulative counter so an unusually long run can never
/// overflow an `int`. The most valuable currently-owned item is deliberately NOT stored here --
/// it's derived fresh from live Inventory/Equipment every time the Adventure Record screen opens
/// (see AdventureRecordScreen), since a stored value would drift the moment an item was sold,
/// dropped, or picked up.
/// </summary>
public class AdventureRecord
{
    public long MonstersKilled { get; set; }
    public long DamageDealt { get; set; }
    public long DamageTaken { get; set; }
    public long GoldCollected { get; set; }
    public long GoldSpent { get; set; }
    public long BossesKilled { get; set; }

    /// <summary>Null until the first boss kill -- AdventureRecordScreen shows "None" in that case.</summary>
    public string HighestLevelBossName { get; set; }
    public int HighestLevelBossLevel { get; set; }

    /// <summary>Highest floor index the player has ever entered this run -- see DungeonManager.PlaceActorAt, the single place every floor entry (new game, load, ascend, descend) funnels through. Never decreases, even when ascending back toward floor 1.</summary>
    public int DeepestFloorReached { get; set; }

    /// <summary>
    /// Real-world seconds actually spent playing this character, accumulated across every
    /// completed session -- NOT wall-clock time since character creation, so a save left closed
    /// for days doesn't inflate it. Only reflects sessions that have already ended (quit or
    /// death); the CURRENT session's own elapsed time lives only in GameLoop's own
    /// sessionStartUtc until GameLoop.RecordElapsedDungeonTime folds it in here (on quit, on
    /// death, and whenever the Adventure Record screen is opened mid-run, so a live check always
    /// shows an up-to-date total). Starts at 0 for a freshly created character -- the clock
    /// starts the moment CharacterCreationScreen.Run returns and GameLoop is constructed.
    /// </summary>
    public long TimeInDungeonSeconds { get; set; }

    /// <summary>Individual item units removed permanently by the Lost Items system -- a dropped bundle of 3 that's lost counts as 3, not 1. Never incremented for fire/lava destruction (see ItemDestructionRules), which is a separate outcome from permanent loss.</summary>
    public long ItemsPermanentlyLost { get; set; }

    /// <summary>Individual item units placed as concealed (Misplaced outcome) -- see ItemLandingResolver. Counted at the moment of concealment, independent of whether they're ever found.</summary>
    public long ItemsMisplaced { get; set; }

    /// <summary>Individual concealed item units picked up after being discovered -- see GroundItem.CountsAsMisplaced/HasBeenRecovered and GameLoop's pickup handling. A repeated drop/pickup cycle of the same physical item only adds to this again if it's genuinely misplaced again, never twice for the same concealment.</summary>
    public long MisplacedItemsRecovered { get; set; }

    /// <summary>Null until the first permanent loss -- AdventureRecordScreen shows "None" in that case. Always DisplayName, never the true Name of an unidentified item.</summary>
    public string MostValuableLostItemName { get; set; }
    public int MostValuableLostItemValue { get; set; }

    /// <summary>Individual projectile units removed by impact breakage (see ItemBreakageRules/ItemLandingOutcome.Broken) -- deliberately separate from ItemsPermanentlyLost, since breaking and permanent loss are distinct outcomes even though both remove the item the same way.</summary>
    public long ItemsBroken { get; set; }
}
