using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities;
using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Core;

/// <summary>
/// Pure chance math for the Lost Items system: whether an item can be permanently lost at all,
/// and its lost/misplaced chances for a given throw or drop. Rolling those chances into an actual
/// landing outcome (and placing the item) is ItemLandingResolver's job, not this class's -- kept
/// separate so the math here stays testable by value without a seeded Random. Existing Fire/Lava
/// destruction (ItemDestructionRules) always takes priority over both rolls -- see
/// ItemLandingResolver, which checks destruction before ever calling into this class.
/// </summary>
public static class ItemLossRules
{
    /// <summary>
    /// Whether this item is even eligible for permanent loss, independent of size/terrain/darkness
    /// -- Item.CanBeLost is the explicit per-template opt-out; IsSkeletonKey and
    /// ThrownWeaponBehavior.ReturnsToThrower are automatic exemptions no template needs to
    /// remember to set; the value threshold uses GoldValue directly, which is always the item's
    /// real value regardless of IsIdentified (see Item.GoldValue's own doc comment) -- so the
    /// player never has an otherwise-protected rare item vanish just because they hadn't
    /// identified it yet. For a container, the value of everything inside is added too (Persistent
    /// Containers proposal), so a cheap bag holding valuable gear is never treated as disposable.
    /// A protected item can still become Misplaced -- this only gates the permanent-loss branch
    /// (see ItemLandingResolver).
    /// </summary>
    public static bool CanBeLost(Item item) =>
        item.CanBeLost
        && !item.IsSkeletonKey
        && item.ThrownWeaponBehavior != ThrownWeaponBehavior.ReturnsToThrower
        && TotalValue(item) < ItemLossConfig.ProtectionValueThreshold;

    private static int TotalValue(Item item) =>
        item.Container == null ? item.GoldValue : item.GoldValue + item.Container.Contents.Items.Sum(i => i.GoldValue);

    /// <summary>Chance a thrown item is permanently lost, clamped to [MinChance, MaxChance] -- 0 for anything CanBeLost already rejects.</summary>
    public static double GetThrownLossChance(Item item, bool hitActor) =>
        CanBeLost(item) ? Math.Clamp(ItemLossConfig.ThrownLostChance(item.ItemSize, hitActor), ItemLossConfig.MinChance, ItemLossConfig.MaxChance) : 0.0;

    /// <summary>Chance a thrown item becomes Misplaced (concealed, recoverable), clamped to [MinChance, MaxChance] -- unlike GetThrownLossChance, this is NOT gated by CanBeLost: a protected item can still be misplaced, just never permanently deleted.</summary>
    public static double GetThrownMisplacedChance(Item item, bool hitActor) =>
        Math.Clamp(ItemLossConfig.ThrownMisplacedChance(item.ItemSize, hitActor), ItemLossConfig.MinChance, ItemLossConfig.MaxChance);

    /// <summary>
    /// Chance a dropped item is permanently lost, clamped to [MinChance, MaxChance]. Applies the
    /// optional Agility modifier (2 points of adjusted Agility above/below 10 shifts the chance by
    /// 1 percentage point the opposite way) and the bundle-quantity discount (a stack of 2-4
    /// halves the chance, 5+ cannot be lost at all) on top of the size/darkness base chance.
    /// </summary>
    public static double GetDroppedLossChance(Player player, Item item, Tile dropTile, int quantity)
    {
        if (!CanBeLost(item) || quantity >= 5)
        {
            return 0.0;
        }

        bool dark = dropTile.IsDarkRoom && !dropTile.IsIlluminated;
        double chance = ItemLossConfig.DroppedLostChance(item.ItemSize, dark);
        chance -= AgilitySteps(player) * 0.01;

        if (quantity >= 2)
        {
            chance *= 0.5;
        }

        return Math.Clamp(chance, ItemLossConfig.MinChance, ItemLossConfig.MaxChance);
    }

    /// <summary>Chance a dropped item becomes Misplaced -- same Agility/bundle shaping as GetDroppedLossChance, but never gated by CanBeLost (a protected item can still be misplaced).</summary>
    public static double GetDroppedMisplacedChance(Player player, Item item, Tile dropTile, int quantity)
    {
        if (quantity >= 5)
        {
            return 0.0;
        }

        bool dark = dropTile.IsDarkRoom && !dropTile.IsIlluminated;
        double chance = ItemLossConfig.DroppedMisplacedChance(item.ItemSize, dark);
        chance -= AgilitySteps(player) * 0.01;

        if (quantity >= 2)
        {
            chance *= 0.5;
        }

        return Math.Clamp(chance, ItemLossConfig.MinChance, ItemLossConfig.MaxChance);
    }

    private static int AgilitySteps(Player player) => (player.Stats.Adjusted(PrimaryAttribute.Agility) - 10) / 2;
}
