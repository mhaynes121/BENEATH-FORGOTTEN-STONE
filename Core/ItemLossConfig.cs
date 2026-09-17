using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities;

namespace BENEATH_FORGOTTEN_STONE.Core;

/// <summary>
/// Every probability/threshold the Lost Items system uses, centralized here rather than
/// scattered through GameLoop/ItemLossRules -- a rebalance is a numbers-only change to this one
/// file. See ItemLossRules for how the *LostChance/*MisplacedChance pairs combine into a final
/// roll, and ItemLandingResolver for how a landing turns into PermanentlyLost/Misplaced/LandsNormally.
///
/// Stage 2 (Phase 2 design doc) note: terrain and darkness used to add directly to loss chance
/// here. They no longer do -- the revised tables below are pure size + hit/miss (thrown) or
/// lit/dark (dropped) lookups. Terrain/darkness instead raise ConcealmentDifficulty, i.e. they
/// make a misplaced item harder to find rather than more likely to vanish outright.
/// </summary>
public static class ItemLossConfig
{
    public const double MinChance = 0.0;
    public const double MaxChance = 0.95;

    /// <summary>An item whose GoldValue is at or above this is protected from permanent loss regardless of size -- see ItemLossRules.CanBeLost. Uses the item's real (identified) value even while unidentified, so the player not yet knowing an item is valuable never lets it slip away. Protected items can still be misplaced/concealed -- only the permanent-loss branch is gated by this.</summary>
    public const int ProtectionValueThreshold = 500;

    public static double ThrownLostChance(ItemSize size, bool hitActor) => size switch
    {
        ItemSize.VerySmall => hitActor ? 0.05 : 0.10,
        ItemSize.Small => hitActor ? 0.01 : 0.02,
        _ => 0.0
    };

    public static double ThrownMisplacedChance(ItemSize size, bool hitActor) => size switch
    {
        ItemSize.VerySmall => hitActor ? 0.15 : 0.30,
        ItemSize.Small => hitActor ? 0.05 : 0.12,
        _ => 0.0
    };

    public static double DroppedLostChance(ItemSize size, bool dark) => size switch
    {
        ItemSize.VerySmall => dark ? 0.03 : 0.01,
        ItemSize.Small => dark ? 0.01 : 0.0,
        _ => 0.0
    };

    public static double DroppedMisplacedChance(ItemSize size, bool dark) => size switch
    {
        ItemSize.VerySmall => dark ? 0.20 : 0.05,
        ItemSize.Small => dark ? 0.06 : 0.01,
        _ => 0.0
    };

    /// <summary>Base concealment difficulty by size, before any modifier below -- a Very Large item is never actually concealed in practice (its misplaced chance is always 0), but the table stays complete for Stage 2, where displacement can carry any size onto rough terrain.</summary>
    public static int ConcealmentBaseDifficulty(ItemSize size) => size switch
    {
        ItemSize.VerySmall => 14,
        ItemSize.Small => 10,
        ItemSize.Medium => 6,
        ItemSize.Large => 2,
        ItemSize.VeryLarge => 0,
        _ => 0
    };

    private static int ConcealmentTerrainModifier(FloorType floorType) => floorType switch
    {
        FloorType.Sand => 5,
        FloorType.Mud => 4,
        FloorType.Swamp => 6,
        FloorType.Grass => 3,
        FloorType.Water => 6,
        _ => 0
    };

    /// <summary>
    /// Final concealment difficulty for an item becoming Misplaced right now -- computed once at
    /// landing time and stored on the GroundItem (see GroundItem.ConcealmentDifficulty), never
    /// recomputed live. `isDisplacedFromOriginal` is always false until Stage 2 adds displacement.
    /// Clamped to a minimum of 1 so even a Very Large item's difficulty (base 0) is never
    /// impossible to beat outright, on the off chance Stage 2 ever concealed one.
    /// </summary>
    public static int ConcealmentDifficulty(
        ItemSize size,
        FloorType floorType,
        bool darkAndUnilluminated,
        bool wasThrownMiss,
        bool isDisplacedFromOriginal,
        bool isIlluminated,
        bool playerStandingHere)
    {
        int difficulty = ConcealmentBaseDifficulty(size);
        difficulty += ConcealmentTerrainModifier(floorType);
        if (darkAndUnilluminated) difficulty += 5;
        if (wasThrownMiss) difficulty += 3;
        if (isDisplacedFromOriginal) difficulty += 2;
        if (isIlluminated) difficulty -= 3;
        if (playerStandingHere) difficulty -= 4;

        return Math.Max(1, difficulty);
    }
}
