using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities;
using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Core;

/// <summary>
/// Context-sensitive flavor text for a permanently lost item -- organized by situation (dropped
/// vs. thrown-and-hit vs. thrown-and-missed, then narrowed further by terrain/darkness/jewelry
/// where a pool exists) rather than one repeated generic string. Every message uses
/// Item.DisplayName, so an unidentified item's loss message never leaks its true name. ForDropped/
/// ForThrown state plainly that the item is gone -- they're only ever used for the PermanentlyLost
/// outcome now (see ItemLandingResolver). ForMisplaced/ForRecovered are their Misplaced-outcome
/// counterparts: worded to imply the item might still be found, never that it's gone for good.
/// </summary>
public static class ItemLossMessages
{
    private static readonly string[] MisplacedGeneric =
    {
        "The {0} bounces away and disappears from sight.",
        "The {0} tumbles out of view somewhere nearby.",
        "The {0} skitters off and is lost from sight for now."
    };

    private static readonly string[] MisplacedSand =
    {
        "The {0} lands in the sand and disappears beneath the surface.",
        "The {0} vanishes into a dune, buried in loose sand."
    };

    private static readonly string[] MisplacedMud =
    {
        "The {0} sinks into the mud.",
        "The {0} disappears into the thick mud with a wet suck."
    };

    private static readonly string[] MisplacedSwamp =
    {
        "The {0} splashes into the swamp and out of sight.",
        "The {0} sinks beneath the murky water, somewhere close by."
    };

    private static readonly string[] MisplacedWater =
    {
        "The {0} splashes into the water and out of view.",
        "The {0} disappears beneath the surface nearby."
    };

    private static readonly string[] MisplacedGrass =
    {
        "The {0} disappears into the tall grass.",
        "The {0} vanishes somewhere in the undergrowth."
    };

    private static readonly string[] MisplacedDark =
    {
        "The {0} vanishes somewhere in the darkness.",
        "You lose track of the {0} the instant it leaves the light."
    };

    private static readonly string[] DroppedJewelry =
    {
        "The {0} slips from your fingers and bounces away into the cave.",
        "The {0} falls awkwardly and vanishes among the rubble."
    };

    private static readonly string[] DroppedGeneric =
    {
        "You fumble the {0} from your fingertips and lose it among the stones.",
        "The {0} slips from your grasp and disappears into a crack.",
        "The {0} rolls away and is gone before you can catch it."
    };

    private static readonly string[] ThrownHit =
    {
        "The {0} strikes its target, then disappears among the stones.",
        "The {0} hits, but you lose sight of it in the confusion.",
        "The {0} connects, then is lost in the scuffle."
    };

    private static readonly string[] ThrownMissGeneric =
    {
        "The {0} skips across the floor and is lost among the rubble.",
        "The {0} misses and vanishes into the darkness.",
        "The {0} sails wide and is nowhere to be found.",
        "The {0} goes astray and is lost for good."
    };

    private static readonly string[] Darkness =
    {
        "The {0} misses and vanishes into the darkness.",
        "The {0} disappears into the black before you hear it land.",
        "You lose track of the {0} the instant it leaves the light."
    };

    private static readonly string[] Sand =
    {
        "The {0} drops into the sand and disappears beneath the surface.",
        "The {0} vanishes into a dune, swallowed by loose sand."
    };

    private static readonly string[] Mud =
    {
        "The {0} sinks into the mud and is lost.",
        "The {0} disappears into the thick mud with a wet suck."
    };

    private static readonly string[] Swamp =
    {
        "The {0} splashes into the swamp and vanishes.",
        "The {0} sinks beneath the murky water and is gone."
    };

    private static readonly string[] Water =
    {
        "The {0} splashes into the water and sinks out of sight.",
        "The {0} disappears beneath the surface with barely a ripple."
    };

    private static readonly string[] Grass =
    {
        "The {0} vanishes into the tall grass.",
        "The {0} is swallowed by the undergrowth."
    };

    private static readonly string[] Ice =
    {
        "The {0} skitters across the ice and disappears from view.",
        "The {0} slides away across the ice and is lost."
    };

    private static string Pick(string[] pool, string displayName, Random rng) =>
        string.Format(pool[rng.Next(pool.Length)], displayName);

    private static bool IsJewelry(Item item) =>
        item.EquipmentType == EquipmentType.Ring || item.EquipmentType == EquipmentType.Neck;

    /// <summary>Prefers a terrain-specific pool for the tile the item was dropped/lost on, falling back to the jewelry or generic pool -- darkness only ever applies to a thrown item's final tile (see ForThrown), not a drop, since the dropped-item chance table already branches on lit vs. dark instead of stacking a separate modifier.</summary>
    public static string ForDropped(Item item, Tile tile, Random rng)
    {
        string[] pool = tile.FloorType switch
        {
            FloorType.Sand => Sand,
            FloorType.Mud => Mud,
            FloorType.Swamp => Swamp,
            FloorType.Water => Water,
            FloorType.Grass => Grass,
            FloorType.Ice => Ice,
            _ => IsJewelry(item) ? DroppedJewelry : DroppedGeneric
        };
        return Pick(pool, item.DisplayName, rng);
    }

    public static string ForThrown(Item item, bool hitActor, Tile tile, Random rng)
    {
        bool dark = tile.IsDarkRoom && !tile.IsIlluminated;
        string[] pool = tile.FloorType switch
        {
            FloorType.Sand => Sand,
            FloorType.Mud => Mud,
            FloorType.Swamp => Swamp,
            FloorType.Water => Water,
            FloorType.Grass => Grass,
            FloorType.Ice => Ice,
            _ when dark => Darkness,
            _ when hitActor => ThrownHit,
            _ => ThrownMissGeneric
        };
        return Pick(pool, item.DisplayName, rng);
    }

    /// <summary>Terrain-flavored where a pool exists, darkness next, generic otherwise -- unlike ForDropped/ForThrown, every pool here implies the item may still be found rather than stating it's gone for good (Misplaced, not PermanentlyLost).</summary>
    public static string ForMisplaced(Item item, Tile tile, Random rng)
    {
        bool dark = tile.IsDarkRoom && !tile.IsIlluminated;
        string[] pool = tile.FloorType switch
        {
            FloorType.Sand => MisplacedSand,
            FloorType.Mud => MisplacedMud,
            FloorType.Swamp => MisplacedSwamp,
            FloorType.Water => MisplacedWater,
            FloorType.Grass => MisplacedGrass,
            _ when dark => MisplacedDark,
            _ => MisplacedGeneric
        };
        return Pick(pool, item.DisplayName, rng);
    }

    public static string ForRecovered(Item item) => $"You recover the {item.DisplayName} you had misplaced.";
}
