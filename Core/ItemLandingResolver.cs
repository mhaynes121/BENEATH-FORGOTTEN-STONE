using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities;
using BENEATH_FORGOTTEN_STONE.Entities.Components;
using BENEATH_FORGOTTEN_STONE.Entities.Projectiles;

namespace BENEATH_FORGOTTEN_STONE.Core;

/// <summary>Outcome of resolving a single item landing -- Message is non-null for every outcome except LandsNormally, whose caller supplies its own "You drop/throw the X." line instead.</summary>
public readonly struct LandingResult
{
    public ItemLandingOutcome Outcome { get; init; }
    public string Message { get; init; }
}

/// <summary>
/// Coordinates the full sequence for one dropped or thrown item reaching its landing tile:
/// destruction check, then a lost/misplaced/lands-normally roll, then placement, Adventure Record
/// bookkeeping, and a single matching message -- see the Misplaced Items design doc's "Resolution
/// Order". Supersedes the old ItemLossRules.ResolveThrownLoss/ResolveDroppedLoss, which only ever
/// resolved a two-way lost-or-survives roll with no concealment/recovery outcome. Placing the item
/// (rather than leaving that to the caller) mirrors ItemDestructionRules.LandOnFloor's existing
/// precedent of composing message + placement together in one call.
/// </summary>
public static class ItemLandingResolver
{
    public static LandingResult ResolveDropped(Player player, Item item, Level level, int x, int y, int quantity, Random rng)
    {
        string destructionMessage = ItemDestructionRules.GetDestructionMessage(level, x, y, item);
        if (destructionMessage != null)
        {
            return new LandingResult { Outcome = ItemLandingOutcome.Destroyed, Message = destructionMessage };
        }

        Tile tile = level.Tiles[x, y];
        double lostChance = ItemLossRules.GetDroppedLossChance(player, item, tile, quantity);
        double misplacedChance = ItemLossRules.GetDroppedMisplacedChance(player, item, tile, quantity);
        bool dark = tile.IsDarkRoom && !tile.IsIlluminated;

        return Resolve(player, item, level, x, y, quantity, lostChance, misplacedChance, tile, dark,
            wasThrownMiss: false, ItemLandingOrigin.Dropped, rng);
    }

    /// <summary>`reason` is the projectile's actual termination reason (HitWall/HitCreature/Miss) -- used both for the existing hit-vs-miss loss/misplaced chance split (HitCreature = hit, anything else = miss) and for the new breakage check, which additionally distinguishes a wall impact from an ordinary out-of-range landing (see ItemBreakageRules).</summary>
    public static LandingResult ResolveThrown(Player player, Item item, ProjectileTerminationReason reason, Level level, int x, int y, Random rng)
    {
        string destructionMessage = ItemDestructionRules.GetDestructionMessage(level, x, y, item);
        if (destructionMessage != null)
        {
            return new LandingResult { Outcome = ItemLandingOutcome.Destroyed, Message = destructionMessage };
        }

        var (broke, breakMessage) = ItemBreakageRules.Resolve(item, reason, rng);
        if (broke)
        {
            player.AdventureRecord.ItemsBroken += 1;
            return new LandingResult { Outcome = ItemLandingOutcome.Broken, Message = breakMessage };
        }

        bool hitActor = reason == ProjectileTerminationReason.HitCreature;
        Tile tile = level.Tiles[x, y];
        double lostChance = ItemLossRules.GetThrownLossChance(item, hitActor);
        double misplacedChance = ItemLossRules.GetThrownMisplacedChance(item, hitActor);
        bool dark = tile.IsDarkRoom && !tile.IsIlluminated;

        return Resolve(player, item, level, x, y, quantity: 1, lostChance, misplacedChance, tile, dark,
            wasThrownMiss: !hitActor, ItemLandingOrigin.Thrown, rng);
    }

    private static LandingResult Resolve(
        Player player, Item item, Level level, int x, int y, int quantity,
        double lostChance, double misplacedChance, Tile tile, bool dark, bool wasThrownMiss,
        ItemLandingOrigin origin, Random rng)
    {
        double roll = rng.NextDouble();

        if (roll < lostChance)
        {
            player.AdventureRecord.ItemsPermanentlyLost += quantity;
            if (item.GoldValue > player.AdventureRecord.MostValuableLostItemValue)
            {
                player.AdventureRecord.MostValuableLostItemName = item.DisplayName;
                player.AdventureRecord.MostValuableLostItemValue = item.GoldValue;
            }
            string message = origin == ItemLandingOrigin.Thrown
                ? ItemLossMessages.ForThrown(item, hitActor: !wasThrownMiss, tile, rng)
                : ItemLossMessages.ForDropped(item, tile, rng);
            return new LandingResult { Outcome = ItemLandingOutcome.PermanentlyLost, Message = message };
        }

        if (roll < lostChance + misplacedChance)
        {
            bool playerStandingHere = x == player.X && y == player.Y;
            int difficulty = ItemLossConfig.ConcealmentDifficulty(
                item.ItemSize, tile.FloorType, dark, wasThrownMiss,
                isDisplacedFromOriginal: false, tile.IsIlluminated, playerStandingHere);

            var groundItem = level.AddConcealedItem(x, y, item, difficulty, origin);
            groundItem.CountsAsMisplaced = true;
            player.AdventureRecord.ItemsMisplaced += quantity;

            // The item landed at the player's own feet -- that's itself a discovery trigger
            // (design doc: "a concealed item lands at the player's feet"), independent of ever
            // stepping onto the tile later, since the player is already standing there right now.
            if (playerStandingHere && ItemDiscoveryRules.RollPassiveDiscovery(player, groundItem, playerStandingOnTile: true, rng))
            {
                groundItem.IsConcealed = false;
            }

            return new LandingResult { Outcome = ItemLandingOutcome.Misplaced, Message = ItemLossMessages.ForMisplaced(item, tile, rng) };
        }

        level.AddVisibleItem(x, y, item, origin);
        return new LandingResult { Outcome = ItemLandingOutcome.LandsNormally, Message = null };
    }
}
