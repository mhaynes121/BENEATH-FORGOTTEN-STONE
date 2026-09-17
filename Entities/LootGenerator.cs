using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Centralizes monster-death item drops so combat/death code never needs to
/// know a loot formula exists -- GameLoop's death-sweep is the only caller,
/// mirroring ExperienceRewardCalculator/GoldDropCalculator. Only generates
/// Item instances; placing them on the ground is the caller's job (see
/// GameLoop.AwardDeathRewards). Which item TYPE gets drawn is balanced via
/// ItemTypeWeights rather than a plain uniform draw over the eligible pool.
/// </summary>
public static class LootGenerator
{
    // Loot roll balance knobs -- centralized here for easy rebalancing.
    // Must sum to 1.0.
    private const double NoDropChance = 0.60;
    private const double OneItemDropChance = 0.30;
    private const double TwoItemDropChance = 0.10;

    private const int MaxItemsPerMonster = 2;

    /// <summary>How far below/above min(PlayerLevel, MonsterLevel) an item's own MinimumLevel may fall to still be droppable.</summary>
    private const int LootLevelMinOffset = -3;
    private const int LootLevelMaxOffset = 0;

    public static List<Item> GenerateLoot(Monster monster, Player player, Random rng) =>
        RollItems(monster, Math.Min(player.Level, monster.Level), rng);

    /// <summary>
    /// Items a monster spawns already carrying -- a separate roll from death loot
    /// (GenerateLoot), called once by Monster.CreateRandom so a monster with
    /// CanUseItems might actually have something to use (see ChaseAI's healing-item
    /// check) instead of that capability only ever mattering for the corpse it leaves
    /// behind. Same eligibility rules (CanCarryItems/CanEquipItems/Size/MinimumLevel),
    /// just windowed off the monster's own Level since no Player exists yet at spawn time.
    /// </summary>
    public static List<Item> GenerateSpawnItems(Monster monster, Random rng) =>
        RollItems(monster, monster.Level, rng);

    private static List<Item> RollItems(Monster monster, int targetLevel, Random rng)
    {
        var items = new List<Item>();

        // A creature that can't carry items at all (the CreatureType.Animal default)
        // never gets any, regardless of the roll below -- see CreatureCapabilities.
        // Checking the capability rather than CreatureType directly means a future type
        // (or an individual archetype override) changes behavior automatically here.
        if (!monster.CanCarryItems)
        {
            return items;
        }

        int count = RollDropCount(rng);
        if (count == 0)
        {
            return items;
        }

        int minLevel = Math.Max(1, targetLevel + LootLevelMinOffset);
        int maxLevel = targetLevel + LootLevelMaxOffset;

        // MinimumLevel == 0 means "no level requirement" (most consumables/basic
        // gear in this catalog) -- those stay eligible regardless of the loot
        // level window; only items that actually declare a MinimumLevel get
        // range-filtered against it.
        var candidates = Items.All
            .Where(i => i.CanDropAsLoot)
            // Bridges the five-tier ItemSize (Lost Items system) to the existing three-tier
            // monster Size rather than comparing Item.Size directly -- see ItemSizeMapping's own
            // doc comment for why a Large monster can still drop a Very Large weapon under this.
            .Where(i => ItemSizeMapping.MinimumMonsterSizeForLoot(i.ItemSize) <= monster.Size)
            .Where(i => monster.CanEquipItems || i.EquipmentType == EquipmentType.None)
            .Where(i => i.MinimumLevel == 0 || (i.MinimumLevel >= minLevel && i.MinimumLevel <= maxLevel))
            .ToList();

        var byType = candidates.GroupBy(i => i.Type).ToDictionary(g => g.Key, g => g.ToList());

        for (int i = 0; i < Math.Min(count, MaxItemsPerMonster); i++)
        {
            var type = ItemTypeWeights.PickType(byType.Keys, rng);
            if (type == null)
            {
                break; // no eligible candidates of any type left
            }

            var pool = byType[type.Value];
            var chosen = pool[rng.Next(pool.Count)];
            pool.Remove(chosen); // no duplicate item within the same roll
            if (pool.Count == 0)
            {
                byType.Remove(type.Value);
            }

            // Same "don't hand out the shared catalog reference" rule DungeonGenerator.SpawnItems
            // already follows -- see Item.RequiresUniqueInstance.
            items.Add(chosen.RequiresUniqueInstance ? chosen.Clone() : chosen);
        }

        return items;
    }

    private static int RollDropCount(Random rng)
    {
        double roll = rng.NextDouble();
        if (roll < NoDropChance)
        {
            return 0;
        }
        if (roll < NoDropChance + OneItemDropChance)
        {
            return 1;
        }
        return 2;
    }
}
