using BENEATH_FORGOTTEN_STONE.Core;
using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Corpse System section 18 "Death-Drop Service": centralizes actor-death output so combat/reward
/// code never needs to know a corpse-creation formula exists. Reuses ItemDestructionRules' pure
/// destruction QUERY (not LandOnFloor, which places the item on the ground itself) since the loot
/// is landing INSIDE a corpse now, not loose on the tile -- Fire/Lava can still destroy it exactly
/// as before, just without a separate ground placement step.
/// </summary>
public static class DeathDropService
{
    /// <returns>The created Corpse (loot-bearing) or null (nothing survived -- an immediately portable corpse Item was placed on the ground instead), plus a destruction message for each item that didn't survive.</returns>
    public static (Corpse Corpse, List<string> DestructionMessages) CreateCorpse(
        Level level, int x, int y, CorpseMetadata metadata, IReadOnlyList<Item> loot)
    {
        var destructionMessages = new List<string>();
        var survivingLoot = new List<Item>();
        foreach (var item in loot)
        {
            string destroyedMessage = ItemDestructionRules.GetDestructionMessage(level, x, y, item);
            if (destroyedMessage != null)
            {
                destructionMessages.Add(destroyedMessage);
            }
            else
            {
                survivingLoot.Add(item);
            }
        }

        // Section 6: an actor with no surviving loot creates an immediately portable corpse item
        // rather than a container the player would have to open just to find it empty.
        if (survivingLoot.Count == 0)
        {
            var portableItem = CorpseItemFactory.CreatePortableItem(metadata);
            level.AddVisibleItem(x, y, portableItem, ItemLandingOrigin.Generated);
            return (null, destructionMessages);
        }

        var corpse = new Corpse(x, y, metadata, survivingLoot);
        level.Corpses.Add(corpse);
        return (corpse, destructionMessages);
    }
}
