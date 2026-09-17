namespace BENEATH_FORGOTTEN_STONE.Persistence;

/// <summary>Serializable snapshot of a Trader's fully-resolved state -- mirrors MonsterData's philosophy exactly: persist the exact remaining stock/gold, never regenerate on load. See Trader.RestoreData/Restore.</summary>
public class TraderData
{
    public string Name { get; set; }
    public char Symbol { get; set; }
    public ConsoleColor Color { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public string ShortDescription { get; set; }
    public string LongDescription { get; set; }
    public long Gold { get; set; }

    /// <summary>The trader's exact remaining stock -- resolved by name against Items.All, same as every other item list in the save file.</summary>
    public List<ItemData> Inventory { get; set; } = new();
}
