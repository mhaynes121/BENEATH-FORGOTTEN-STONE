namespace BENEATH_FORGOTTEN_STONE.Persistence;

/// <summary>Serializable mirror of Dungeon.Corpse -- a loot-bearing corpse only; an emptied/portable one is just an ItemData (with a non-null Corpse field) inside the level's ordinary GroundItems.</summary>
public class CorpseData
{
    public int X { get; set; }
    public int Y { get; set; }
    public CorpseMetadataData Metadata { get; set; }
    public List<ItemData> Contents { get; set; } = new();
}
