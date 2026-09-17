using BENEATH_FORGOTTEN_STONE.Dungeon;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Centralized Look/Examine decision logic -- what's inspectable in a
/// direction and which description level applies. UI code (GameLoop for
/// the map, InventoryScreen for items) handles the actual key-reading and
/// console output around these decisions; nothing here does I/O, and no
/// description text is generated here -- it always comes from the
/// inspected object's own IInspectable data.
/// </summary>
public static class LookService
{
    /// <summary>Look never needs to see further than the player's own FOV radius.</summary>
    public const int MaxLookRange = 8;

    /// <summary>Closest actor along the ray, respecting the existing FOV (Tile.IsVisible) -- not just walls.</summary>
    public static Actor FindClosestVisibleActor(Level level, int x, int y, int dx, int dy) =>
        RayTracer.FindActorAlongRay(level, x, y, dx, dy, MaxLookRange, requireVisible: true);

    /// <summary>The tile the look ray settles on -- the closest actor's tile if one was found, otherwise the last visible tile before a wall/range limit. Used to check for an item at the same spot.</summary>
    public static (int X, int Y) FindLookTargetTile(Level level, int x, int y, int dx, int dy) =>
        RayTracer.FindTileAlongRay(level, x, y, dx, dy, MaxLookRange, requireVisible: true);

    /// <summary>"Adjacent" includes diagonals, matching how melee range/movement already work throughout the game.</summary>
    public static bool IsAdjacent(Actor from, Actor to) =>
        Math.Max(Math.Abs(from.X - to.X), Math.Abs(from.Y - to.Y)) == 1;

    /// <summary>
    /// Prefixed with the target's name -- ShortDescription/LongDescription are flavor text
    /// that never mentions who/what it's describing (e.g. "A mangy rat, twitching and
    /// wary." never says "rat"), so without this a monster's own name never actually
    /// appeared anywhere in a Look result. A boss additionally shows its underlying
    /// creature type in parentheses, e.g. "Lormax Golden Wing (Giant Cockroach)" -- its
    /// DisplayName is a proper name (see the Boss Monster Naming doc) that no longer
    /// reveals what kind of creature it is, and the flavor clause CreateBoss appends
    /// ("...than others of its kind") would otherwise have no antecedent.
    /// </summary>
    /// <param name="currentTurn">Level.TurnNumber -- needed only to check the Frightened tag below (a turn-deadline condition, unlike the plain IsProne flag). Null skips that check entirely, e.g. for a caller with no Level handy.</param>
    public static string DescribeCharacter(Actor viewer, Actor target, int? currentTurn = null)
    {
        string description = IsAdjacent(viewer, target) ? target.LongDescription : target.ShortDescription;
        string label = target is Monster { IsBoss: true } boss
            ? $"{Capitalize(boss.DisplayName)} ({Capitalize(boss.Name)})"
            : Capitalize(target.DisplayName);
        if (target.IsProne)
        {
            label += " (Prone)";
        }
        if (currentTurn.HasValue && currentTurn.Value < target.FrightenedUntilTurn)
        {
            label += " (Frightened)";
        }
        return $"{label}: {description}";
    }

    private static string Capitalize(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToUpper(name[0]) + name[1..];
}
