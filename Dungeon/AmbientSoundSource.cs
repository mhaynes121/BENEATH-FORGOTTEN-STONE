namespace BENEATH_FORGOTTEN_STONE.Dungeon;

/// <summary>
/// One real, fixed location on a Level that can produce an environmental ambient sound --
/// created once at generation time for a lucky subset of special-floor rooms (see
/// DungeonGenerator.AssignSpecialFloors and Entities.Sounds.SoundConfig.EnvironmentSoundSourceChance),
/// never moved or destroyed afterward (design spec section 40: "environmental sounds persist
/// while their source exists," and nothing in this game can currently remove a floor type once
/// placed). Position is the room's own center, not every tile of that FloorType, so distance is
/// measured from one consistent point per room.
/// </summary>
public class AmbientSoundSource
{
    public int X { get; }
    public int Y { get; }
    public FloorType FloorType { get; }

    public AmbientSoundSource(int x, int y, FloorType floorType)
    {
        X = x;
        Y = y;
        FloorType = floorType;
    }
}
