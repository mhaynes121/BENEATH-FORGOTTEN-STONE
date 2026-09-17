using BENEATH_FORGOTTEN_STONE.Entities;
using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Dungeon;

/// <summary>
/// Corpse System: a loot-bearing corpse lying on a dungeon level -- a stationary, nonblocking
/// container, mirroring Chest's own "Level-level overlay list" shape (Level.Corpses, alongside
/// Level.Chests/Traps/Doors). Exists only while it still holds items; once the last one is
/// removed it's replaced by a portable Item (see CorpseItemFactory) and dropped from this list
/// entirely -- see GameLoop's corpse-open flow, the only place that transition happens. Reuses
/// ContainerKind.Chest (never carried, no weight reduction) rather than adding a third
/// ContainerKind value purely for this.
/// </summary>
public class Corpse
{
    public int X { get; set; }
    public int Y { get; set; }
    public CorpseMetadata Metadata { get; }
    public ContainerComponent Container { get; } = new(ContainerKind.Chest, default, CorpseConfig.SlotCapacity, 0);

    public Corpse(int x, int y, CorpseMetadata metadata, IEnumerable<Item> loot)
    {
        X = x;
        Y = y;
        Metadata = metadata;
        foreach (var item in loot)
        {
            Container.Contents.AddItem(item);
        }
    }
}
