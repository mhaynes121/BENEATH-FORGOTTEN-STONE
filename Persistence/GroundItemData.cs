using BENEATH_FORGOTTEN_STONE.Dungeon;

namespace BENEATH_FORGOTTEN_STONE.Persistence;

/// <summary>
/// One entry of a Level's GroundItems overlay -- position plus the same name-resolved ItemData
/// already used for inventory/equipment. IsConcealed/ConcealmentDifficulty/LandingOrigin/
/// CountsAsMisplaced/HasBeenRecovered all default to visible/0/Generated/false/false, so an older
/// save missing these fields entirely deserializes every ground item as a plain visible one --
/// exactly the pre-Misplaced-Items behavior.
/// </summary>
public class GroundItemData
{
    public int X { get; set; }
    public int Y { get; set; }
    public ItemData Item { get; set; }

    public bool IsConcealed { get; set; }
    public int ConcealmentDifficulty { get; set; }
    public ItemLandingOrigin LandingOrigin { get; set; }
    public bool CountsAsMisplaced { get; set; }
    public bool HasBeenRecovered { get; set; }
}
