using BENEATH_FORGOTTEN_STONE.Dungeon;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Prone/Knockdown System: every tunable number for the Water/Ice slip-and-fall mechanic, kept in
/// its own config class rather than embedded in GameLoop -- same convention as
/// ProficiencyConfig/FloorAttunementConfig. See GameLoop.HandleMove for the one call site (rolled
/// only on a successful move onto the tile -- see that method's own doc comment for the full list
/// of cases deliberately excluded: waiting, standing, teleporting, a blocked move, a bump-attack,
/// a door, or any other floor type).
/// </summary>
public static class SlipConfig
{
    public const double WaterBaseChance = 0.005;
    public const double IceBaseChance = 0.020;

    private const double BarefootMultiplier = 2.0;
    private const double ShodMultiplier = 1.0;

    private const double LuckModifierPerPoint = 0.05;
    private const double LuckModifierMin = 0.5;
    private const double LuckModifierMax = 1.5;

    public static double BaseChanceFor(FloorType floorType) => floorType switch
    {
        FloorType.Water => WaterBaseChance,
        FloorType.Ice => IceBaseChance,
        _ => 0.0
    };

    /// <summary>Reduces the chance of falling only -- never grants complete immunity, per the proposal's own "footwear should reduce only the chance of falling" rule, unless a future item explicitly says otherwise.</summary>
    public static double FootwearModifier(bool feetSlotOccupied) => feetSlotOccupied ? ShodMultiplier : BarefootMultiplier;

    /// <summary>clamp(1 + ((10 - adjustedLuck) * 0.05), 0.5, 1.5) -- higher Luck lowers the chance, lower Luck raises it.</summary>
    public static double LuckModifier(int adjustedLuck) =>
        Math.Clamp(1 + (10 - adjustedLuck) * LuckModifierPerPoint, LuckModifierMin, LuckModifierMax);

    public static double SlipChance(FloorType floorType, bool feetSlotOccupied, int adjustedLuck) =>
        BaseChanceFor(floorType) * FootwearModifier(feetSlotOccupied) * LuckModifier(adjustedLuck);
}
