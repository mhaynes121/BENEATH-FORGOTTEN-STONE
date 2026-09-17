namespace BENEATH_FORGOTTEN_STONE.Dungeon;

/// <summary>
/// How a GroundItem ended up on its tile. Purely informational today (nothing branches on it
/// except ItemLandingResolver picking flavor text) -- kept on every GroundItem anyway since it's
/// cheap to record and Stage 2's displacement work will want to know "did this item arrive here
/// by sliding away from a throw" without re-deriving it. Generated is the default (0) value on
/// purpose: an older save's GroundItemData has no LandingOrigin field, so it deserializes to this
/// automatically, which is the correct label for pre-existing dungeon loot anyway.
/// </summary>
public enum ItemLandingOrigin
{
    Generated,
    Dropped,
    Thrown,
    MonsterLoot,
    ChestSpill,

    /// <summary>Unused until Stage 2 -- reserved so Displaced doesn't need to be inserted (and every later enum value renumbered) once displacement lands.</summary>
    Displaced
}
