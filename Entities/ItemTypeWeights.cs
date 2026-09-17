using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Centralized item-type balance weights, shared by every random item draw
/// over Items.All -- LootGenerator's monster-death drops and
/// DungeonGenerator's ambient floor spawns -- so the drawn item type is
/// balanced by design rather than by how many catalog entries happen to
/// exist per ItemType. Without this, Scroll+Spellbook (one item per spell,
/// ~48 entries) would swamp a plain uniform draw next to e.g. Wand (4
/// entries). Must sum to 1.0.
/// </summary>
public static class ItemTypeWeights
{
    private static readonly Dictionary<ItemType, double> Weights = new()
    {
        [ItemType.Consumable] = 0.22,
        [ItemType.Weapon] = 0.18,
        [ItemType.Armor] = 0.27,
        [ItemType.Wand] = 0.05,
        [ItemType.Scroll] = 0.09,
        [ItemType.Spellbook] = 0.09,
        [ItemType.Lockpick] = 0.10
    };

    /// <summary>Fallback weight for any ItemType added later without an entry above, so it stays reachable instead of silently never being drawn.</summary>
    private const double DefaultWeight = 0.05;

    public static double Get(ItemType type) => Weights.TryGetValue(type, out double weight) ? weight : DefaultWeight;

    /// <summary>
    /// Weighted pick among the given ItemTypes (expected to be exactly the
    /// types with at least one eligible candidate this roll), renormalized
    /// over just those so a type absent from the pool never wastes
    /// probability mass. Returns null only if availableTypes is empty.
    /// </summary>
    public static ItemType? PickType(IReadOnlyCollection<ItemType> availableTypes, Random rng)
    {
        if (availableTypes.Count == 0)
        {
            return null;
        }

        double totalWeight = availableTypes.Sum(Get);
        double roll = rng.NextDouble() * totalWeight;
        double cumulative = 0;
        foreach (var type in availableTypes)
        {
            cumulative += Get(type);
            if (roll < cumulative)
            {
                return type;
            }
        }
        return availableTypes.Last(); // floating-point rounding fallback
    }
}
