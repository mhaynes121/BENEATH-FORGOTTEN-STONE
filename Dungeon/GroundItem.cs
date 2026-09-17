using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Dungeon;

/// <summary>
/// One item sitting on the floor -- replaces the old (X, Y, Item) tuple GroundItems used to be a
/// list of, so a ground item can carry discovery state. IsConcealed is the only field visibility
/// (Level.GetItemAt/GetItemsAt) checks; the rest exists for the concealment/discovery/recovery
/// system built on top of it -- see ItemLandingResolver, ItemDiscoveryRules, GameLoop.HandleSearch.
/// </summary>
public sealed class GroundItem
{
    public int X { get; set; }
    public int Y { get; set; }
    public Item Item { get; set; }

    /// <summary>Hidden from Level.GetItemAt/GetItemsAt (and therefore rendering/pickup/Look) until a successful Active Search or passive discovery flips this to false -- see Level.GetGroundItemsAt for the unfiltered view discovery code needs.</summary>
    public bool IsConcealed { get; set; }

    /// <summary>Fixed at the moment this item became concealed (see ItemLossConfig.ConcealmentDifficulty) -- never recomputed live, so a later change in lighting/terrain doesn't retroactively make an already-misplaced item easier or harder to find.</summary>
    public int ConcealmentDifficulty { get; set; }

    public ItemLandingOrigin LandingOrigin { get; set; }

    /// <summary>Set once, at creation, whenever ItemLandingResolver's outcome was Misplaced -- never changes afterward (even once IsConcealed flips to false on discovery). Distinguishes "this ground item is eligible to award a recovery credit on pickup" from "this item happens to be sitting on the floor."</summary>
    public bool CountsAsMisplaced { get; set; }

    /// <summary>Guards the recovery credit above so it can only ever be awarded once for this specific GroundItem instance -- see GameLoop's pickup handling. A later drop of the same physical Item creates a brand new GroundItem with its own independent CountsAsMisplaced/HasBeenRecovered, so a genuinely new misplacement is free to award its own new recovery credit.</summary>
    public bool HasBeenRecovered { get; set; }

    /// <summary>
    /// One-shot gate for the "player first illuminates a tile containing a concealed item" passive
    /// check (see GameLoop.RecomputeFov) -- deliberately transient, not part of persistence, so a
    /// save/reload doesn't need an extra field just to remember an attempt was already made. The
    /// accepted tradeoff: reloading near an already-lit concealed item can grant one extra
    /// illumination-triggered roll it wouldn't otherwise have gotten.
    /// </summary>
    internal bool ConsideredForIlluminationDiscovery { get; set; }

    public GroundItem(int x, int y, Item item)
    {
        X = x;
        Y = y;
        Item = item;
    }
}
