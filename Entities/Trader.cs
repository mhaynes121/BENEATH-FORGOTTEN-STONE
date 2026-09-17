using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// A stationary, combat-immune merchant NPC -- see NPC's own doc comment for how immunity is
/// centralized, and TraderScreen for the buy/sell/identify interface bumping into one opens.
/// Health is deliberately never set (stays null): Actor.IsAlive already treats a null Health
/// as "always alive," so a Trader can never die, matching CanBeTargeted's protection with one
/// less thing to guard elsewhere. No AI is ever assigned either -- same null-AI pattern
/// Player already uses -- and a Trader is added to Level.Actors but deliberately never
/// registered with Level.Scheduler (see NPC's doc comment), which is what actually makes it
/// stationary: an unregistered actor is never offered a turn at all.
/// </summary>
public class Trader : NPC
{
    /// <summary>
    /// Seeded very high rather than tracked against real spending -- the design doc allows an
    /// effectively unlimited trader wallet for now, but wants the field to already exist so a
    /// finite cap can be introduced later (a buy-side check against this) without redesigning
    /// the transaction flow.
    /// </summary>
    public long Gold { get; set; }

    private Trader()
    {
    }

    /// <summary>
    /// Builds a level-appropriate trader with 10-20 items in stock -- mirrors Monster.CreateRandom's
    /// role as "spawn-time construction lives on the actor type," not in DungeonGenerator.
    /// Item eligibility mirrors LootGenerator's own level-window philosophy (minus the
    /// monster-only Size/CanEquipItems filters, which don't apply to a merchant's stock) and
    /// draws are independent per slot -- unlike a single monster's death drop, a shop can and
    /// should stock duplicates. A duplicate of a stackable kind (see ItemStacking) merges into
    /// the existing entry's Charges count rather than appearing as its own separate line, same
    /// as a player's inventory; a duplicate of anything else still gets its own entry.
    /// </summary>
    public static Trader CreateRandom(int x, int y, int difficultyLevel, Random rng)
    {
        var trader = new Trader
        {
            X = x,
            Y = y,
            Symbol = 'T',
            Color = ConsoleColor.Yellow,
            Name = "Trader",
            ShortDescription = "A traveling merchant watches you carefully.",
            LongDescription = "A weathered merchant stands behind a collection of carefully " +
                "arranged goods, patiently waiting to see whether you intend to buy or sell.",
            Gold = long.MaxValue / 2
        };

        var eligibleItems = Items.All
            .Where(i => i.CanDropAsLoot && i.MinimumLevel <= difficultyLevel + TraderConfig.ItemLevelLookahead)
            .ToList();
        var byType = eligibleItems.GroupBy(i => i.Type).ToDictionary(g => g.Key, g => g.ToList());

        int itemCount = rng.Next(TraderConfig.TraderMinInventory, TraderConfig.TraderMaxInventory + 1);
        for (int i = 0; i < itemCount; i++)
        {
            var type = ItemTypeWeights.PickType(byType.Keys, rng);
            if (type == null)
            {
                break; // no eligible items at all -- shouldn't happen given Items.All's breadth
            }

            var pool = byType[type.Value];
            var template = pool[rng.Next(pool.Count)];
            // Same "don't hand out the shared catalog reference" rule as every other spawn path.
            var item = template.RequiresUniqueInstance ? template.Clone() : template;

            var existingStack = ItemStacking.FindStackWithRoom(trader.Inventory.Items, item);
            if (existingStack != null)
            {
                existingStack.Charges = (existingStack.Charges ?? 1) + (item.Charges ?? 1);
            }
            else
            {
                trader.Inventory.AddItem(item);
            }
        }

        return trader;
    }

    /// <summary>Fully-resolved snapshot for reconstructing a saved trader -- mirrors Monster.RestoreData's philosophy exactly: persist the exact remaining stock/gold, never regenerate.</summary>
    public class RestoreData
    {
        public string Name;
        public char Symbol;
        public ConsoleColor Color;
        public int X;
        public int Y;
        public string ShortDescription = "";
        public string LongDescription = "";
        public long Gold;
        public List<Item> Inventory = new();
    }

    public static Trader Restore(RestoreData data)
    {
        var trader = new Trader
        {
            X = data.X,
            Y = data.Y,
            Name = data.Name,
            Symbol = data.Symbol,
            Color = data.Color,
            ShortDescription = data.ShortDescription,
            LongDescription = data.LongDescription,
            Gold = data.Gold
        };

        foreach (var item in data.Inventory)
        {
            trader.Inventory.AddItem(item);
        }

        return trader;
    }
}
