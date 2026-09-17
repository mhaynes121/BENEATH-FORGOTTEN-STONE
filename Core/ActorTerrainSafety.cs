using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities;

namespace BENEATH_FORGOTTEN_STONE.Core;

/// <summary>
/// Safe Monster Spawning and Hazard-Aware Movement proposal: the one centralized "would occupying
/// this tile hurt this actor" policy, kept deliberately separate from Level.IsBlockedForActorMovement
/// -- that answers whether a tile is physically PASSABLE; this answers whether completing a turn
/// there would cause unavoidable terrain damage to a SPECIFIC actor. The player always uses
/// physical passability alone (players may deliberately walk into danger); every automatic
/// spawn/placement system and every AI-controlled actor's own voluntary movement should use
/// CanActorOccupy instead. Reuses EnvironmentalFloorEffects' own non-mutating queries rather than
/// maintaining a second hard-coded Fire/Lava/attunement rule that could drift out of sync.
/// </summary>
public static class ActorTerrainSafety
{
    private static readonly (int Dx, int Dy)[] AdjacentOffsets =
    {
        (0, -1), (0, 1), (-1, 0), (1, 0), (-1, -1), (1, -1), (-1, 1), (1, 1)
    };

    /// <summary>
    /// True when completing a turn at (x, y) would NOT cause unavoidable terrain damage to
    /// `actor` -- Fire/Lava environmental damage (respecting a floor-attuned monster's immunity to
    /// its own matching terrain's tick -- see EnvironmentalFloorEffects.CalculateEnvironmentalDamage)
    /// and, for a Monster, floor-attunement attrition from standing off its preferred terrain.
    /// Water/Ice/traps and other non-guaranteed or separately-triggered effects are deliberately
    /// NOT included -- they don't affect AI-controlled actors under current game rules at all (the
    /// Water/Ice slip mechanic only ever rolls for the player's own HandleMove), so treating them
    /// as hazardous here would make AI uniquely (and pointlessly) cautious about tiles nothing
    /// actually threatens them on.
    /// </summary>
    public static bool IsSafeForActor(Level level, Actor actor, int x, int y)
    {
        if (!level.IsInBounds(x, y))
        {
            return false;
        }

        if (EnvironmentalFloorEffects.CalculateEnvironmentalDamage(level, actor, x, y, out _) > 0)
        {
            return false;
        }

        if (actor is Monster monster && EnvironmentalFloorEffects.WouldTakeFloorAttunementAttrition(monster, level.Tiles[x, y].FloorType))
        {
            return false;
        }

        return true;
    }

    /// <summary>Physical passability AND actor-specific safety -- the combined check every automatic placement and every voluntary AI movement decision should use.</summary>
    public static bool CanActorOccupy(Level level, Actor actor, int x, int y) =>
        !level.IsBlockedForActorMovement(x, y) && IsSafeForActor(level, actor, x, y);

    /// <summary>
    /// The first of the 8 tiles adjacent to (x, y) that `actor` could safely occupy right now, or
    /// null if none qualifies -- used by automatic/friendly placement (pet spawn/respawn/stair-
    /// follow/bump-displacement) that would otherwise fall back to Level.FindFreeAdjacentTile's
    /// physical-only result. Callers that must place the actor somewhere regardless (a pet can't
    /// simply fail to exist the way a spawn can be skipped) should fall back to
    /// Level.FindFreeAdjacentTile themselves when this returns null, rather than leaving the actor
    /// unplaced.
    /// </summary>
    public static (int X, int Y)? FindSafeAdjacentTile(Level level, Actor actor, int x, int y)
    {
        foreach (var (dx, dy) in AdjacentOffsets)
        {
            int nx = x + dx;
            int ny = y + dy;
            if (CanActorOccupy(level, actor, nx, ny))
            {
                return (nx, ny);
            }
        }
        return null;
    }
}
