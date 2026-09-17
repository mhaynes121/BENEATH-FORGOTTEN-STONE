namespace BENEATH_FORGOTTEN_STONE.Dungeon;

/// <summary>
/// Every tunable number for the darkness/illumination system in one place, matching
/// FloorTypeConfig's own precedent -- Item/Spell definitions read these instead of embedding
/// magic numbers, so balancing never means hunting through Items.cs/SpellCatalog.cs.
/// </summary>
public static class LightingConfig
{
    public const int CandleLightRadius = 2;
    public const int CandleMaxDuration = 50;
    public const double CandleExtinguishChancePerTurn = 0.005;

    public const int TorchLightRadius = 6;
    public const int TorchMaxDuration = 150;
    public const double TorchExtinguishChancePerTurn = 0.0025;

    public const int LanternLightRadius = 8;
    public const int LanternMaxDuration = 250;
    public const double LanternExtinguishChancePerTurn = 0.001;

    public const int ArcaneOrbLightRadius = 6;
    public const int ArcaneOrbDuration = 100;

    public const int DivineRadianceLightRadius = 5;
    public const int DivineRadianceDuration = 125;

    /// <summary>Chance an ordinary (non-boss, non-stairs) room is generated as a dark room -- see DungeonGenerator.AssignDarkRooms. Recommended range was 15-25%; picked the midpoint.</summary>
    public const double DarkRoomChance = 0.20;

    /// <summary>Chance of noticing (without identifying) an item on an unilluminated dark tile the player just stepped onto -- see GameLoop's darkness-item-notice check.</summary>
    public const double DarkItemNoticeChance = 0.50;
}
