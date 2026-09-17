namespace BENEATH_FORGOTTEN_STONE.Dungeon;

/// <summary>
/// Default properties for each RoomObjectType -- DungeonGenerator.SpawnRoomObjects starts from
/// these and decides IsMovable per spawn (design spec: mobility is configured per instance, not
/// solely by type -- most statues are stationary, but a specific one can be a puzzle piece). A
/// movable instance's LongDescription subtly hints at that via scuff marks/drag tracks, per the
/// design spec's own Look-command guidance ("especially where scratches or tracks provide an
/// in-world clue"), rather than reusing the stationary description.
/// </summary>
public static class RoomObjectCatalog
{
    public static RoomObject Create(RoomObjectType type, int x, int y, bool isMovable) => type switch
    {
        RoomObjectType.WaterFountain => new RoomObject(x, y, type, "water fountain",
            "A carved stone fountain, water trickling softly.",
            isMovable
                ? "A carved stone fountain. Faint drag marks scar the grout around its base, as if it hasn't always stood exactly here."
                : "A carved stone fountain, worn smooth by age. Clear water trickles from a worn spout into a shallow basin.",
            '0', ConsoleColor.Cyan, blocksMovement: true, blocksVision: false, blocksProjectiles: false, isMovable),

        RoomObjectType.Shrine => new RoomObject(x, y, type, "shrine",
            "A small stone shrine.",
            isMovable
                ? "A small stone shrine, its base scored with faint scrape marks -- whatever it once honored, it hasn't always rested here."
                : "A small stone shrine, its carvings worn nearly featureless by centuries of hands and weather.",
            'n', ConsoleColor.White, blocksMovement: true, blocksVision: false, blocksProjectiles: false, isMovable),

        RoomObjectType.Boulder => new RoomObject(x, y, type, "boulder",
            "A large boulder.",
            isMovable
                ? "A large, roughly-hewn boulder. A shallow furrow trails behind it through the dust, like something once dragged it into place."
                : "A large boulder, wedged solidly into the floor -- it hasn't budged in a very long time.",
            'O', ConsoleColor.DarkGray, blocksMovement: true, blocksVision: true, blocksProjectiles: true, isMovable),

        RoomObjectType.Statue => new RoomObject(x, y, type, "statue",
            "A weathered stone statue.",
            isMovable
                ? "A weathered stone statue standing on a plain plinth. Fine scratches arc across the floor beneath its base, the kind something heavy leaves when it's shifted."
                : "A weathered stone statue, its plinth settled deep into the floor -- this one has stood undisturbed for a very long time.",
            'Q', ConsoleColor.Gray, blocksMovement: true, blocksVision: true, blocksProjectiles: true, isMovable),

        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}
