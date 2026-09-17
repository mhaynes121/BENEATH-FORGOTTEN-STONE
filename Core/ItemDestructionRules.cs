using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities;
using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Core;

/// <summary>
/// Single "an item lands on the floor, possibly destroyed" hook -- every site that ever places
/// an Item onto a tile because it's LEAVING an actor's hands right now (a player drop, a thrown
/// weapon's landing spot, a chest's contents spilling out, a monster's death loot) routes through
/// LandOnFloor instead of calling Level.AddItem directly, so Fire/Lava destruction is enforced in
/// exactly one place. Restoring ground items from a save (SaveManager) is NOT one of these sites
/// on purpose -- that's replaying state that already survived (or already didn't) the first time,
/// not a new landing event.
/// </summary>
public static class ItemDestructionRules
{
    /// <returns>True if landing on (x, y) right now would destroy this item -- Flammable and the matching Fire/Lava proofing is absent.</returns>
    public static bool WouldBeDestroyed(Level level, int x, int y, Item item)
    {
        if (!item.Flammable)
        {
            return false;
        }

        return level.Tiles[x, y].FloorType switch
        {
            FloorType.Fire => !item.Fireproof,
            FloorType.Lava => !item.LavaProof,
            _ => false
        };
    }

    /// <summary>The destruction message for landing on (x, y) right now, or null if it would survive -- a pure query, no placement side effect. Extracted so ItemLossRules can check for destruction BEFORE a random-loss roll without also placing the item (see LandOnFloor, which composes this with the actual placement for every ordinary caller that isn't rolling for loss).</summary>
    public static string GetDestructionMessage(Level level, int x, int y, Item item)
    {
        if (!WouldBeDestroyed(level, x, y, item))
        {
            return null;
        }
        string element = level.Tiles[x, y].FloorType == FloorType.Lava ? "lava" : "flames";
        return $"{CombatMessages.WithArticle(item.DisplayName)} is consumed by the {element}!";
    }

    /// <summary>
    /// Adds the item to the floor at (x, y), or destroys it instead if the tile's Fire/Lava would
    /// consume it. Returns a message describing the destruction if that happened, otherwise null
    /// (the ordinary, unremarkable case). When a destroyed item is a container (a bag), its
    /// contents spill onto the same tile first -- recursing into this same function so every
    /// spilled item independently gets its own normal destruction check, rather than a bespoke
    /// bag-specific spill routine (Persistent Containers proposal).
    /// </summary>
    public static string LandOnFloor(Level level, int x, int y, Item item)
    {
        string destructionMessage = GetDestructionMessage(level, x, y, item);
        if (destructionMessage != null)
        {
            if (item.Container != null)
            {
                foreach (var contained in item.Container.Contents.Items.ToList())
                {
                    LandOnFloor(level, x, y, contained);
                }
            }
            return destructionMessage;
        }

        level.AddItem(x, y, item);
        return null;
    }
}
