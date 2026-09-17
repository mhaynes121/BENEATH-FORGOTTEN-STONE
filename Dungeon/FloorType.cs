namespace BENEATH_FORGOTTEN_STONE.Dungeon;

/// <summary>
/// What a specific TileType.Floor tile actually is, layered on top of TileType rather than
/// replacing it -- Wall/StairsUp/StairsDown tiles are never affected by this at all. Always
/// check this field directly (tile.FloorType == FloorType.Water), never the tile's glyph --
/// two different floor types can share a DisplayCharacter (see FloorTypeCatalog) and are only
/// told apart by this. See FloorTypeCatalog for the visual/behavioral definition of each value.
/// </summary>
public enum FloorType
{
    /// <summary>The default for every tile unless generation explicitly assigns something else -- existing dungeons/saves are entirely this, unaffected by adding this system.</summary>
    Normal,
    Water,
    Ice,
    Fire,
    Lava,
    Mud,
    Sand,
    Grass,
    Swamp,
    Ash
}
