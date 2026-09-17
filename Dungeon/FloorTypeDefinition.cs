using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Dungeon;

/// <summary>
/// Centralized visual + behavioral definition for one FloorType -- mirrors CharacterClass/
/// Race/Monster.Archetype's own "one static instance per catalog entry" shape. Every property
/// listed in the design spec lives here and nowhere else, so a new floor type is authored once
/// (this file) rather than scattered across Renderer/combat/generation.
/// </summary>
public class FloorTypeDefinition
{
    public FloorType FloorType { get; }
    public char DisplayCharacter { get; }
    public ConsoleColor DisplayColor { get; }
    public bool Walkable { get; }

    /// <summary>True for Fire/Lava -- see EnvironmentalFloorEffects, which is the only thing that reads this.</summary>
    public bool EnvironmentalDamageEnabled { get; }
    public DamageType? EnvironmentalDamageType { get; }

    public double FireDamageMultiplier { get; }
    public double WaterDamageMultiplier { get; }
    public double IceDamageMultiplier { get; }
    public double ShockDamageMultiplier { get; }

    public FloorTypeDefinition(
        FloorType floorType, char displayCharacter, ConsoleColor displayColor, bool walkable,
        bool environmentalDamageEnabled = false, DamageType? environmentalDamageType = null,
        double fireDamageMultiplier = 1.0, double waterDamageMultiplier = 1.0,
        double iceDamageMultiplier = 1.0, double shockDamageMultiplier = 1.0)
    {
        FloorType = floorType;
        DisplayCharacter = displayCharacter;
        DisplayColor = displayColor;
        Walkable = walkable;
        EnvironmentalDamageEnabled = environmentalDamageEnabled;
        EnvironmentalDamageType = environmentalDamageType;
        FireDamageMultiplier = fireDamageMultiplier;
        WaterDamageMultiplier = waterDamageMultiplier;
        IceDamageMultiplier = iceDamageMultiplier;
        ShockDamageMultiplier = shockDamageMultiplier;
    }

    /// <summary>Maps an incoming attack's DamageType to the matching multiplier above -- Lightning is this game's existing "Shock" damage type (see DamageType's own doc history), Arcane is its "Magic". Everything else (Physical, Poison, Arcane, Shadow, Holy) is always 1.0 here, per the design spec's own "do not modify non-elemental damage" rule -- unless a future floor definition explicitly adds one.</summary>
    public double MultiplierFor(DamageType damageType) => damageType switch
    {
        DamageType.Fire => FireDamageMultiplier,
        DamageType.Water => WaterDamageMultiplier,
        DamageType.Ice => IceDamageMultiplier,
        DamageType.Lightning => ShockDamageMultiplier,
        _ => 1.0
    };
}

public static class FloorTypeCatalog
{
    public static readonly FloorTypeDefinition Normal = new(
        FloorType.Normal, '.', ConsoleColor.White, walkable: true);

    public static readonly FloorTypeDefinition Water = new(
        FloorType.Water, '~', ConsoleColor.Blue, walkable: true,
        fireDamageMultiplier: 0.5, shockDamageMultiplier: 1.5);

    public static readonly FloorTypeDefinition Ice = new(
        FloorType.Ice, '=', ConsoleColor.Cyan, walkable: true);

    public static readonly FloorTypeDefinition Fire = new(
        FloorType.Fire, '^', ConsoleColor.Red, walkable: true,
        environmentalDamageEnabled: true, environmentalDamageType: DamageType.Fire,
        fireDamageMultiplier: 1.5, waterDamageMultiplier: 0.5, iceDamageMultiplier: 0.5);

    /// <summary>Same DisplayCharacter as Water ('~') -- deliberately not distinguished by glyph, only by DisplayColor and, authoritatively, Tile.FloorType itself. See the type-level warning on FloorType against ever branching on a tile's character.</summary>
    public static readonly FloorTypeDefinition Lava = new(
        FloorType.Lava, '~', ConsoleColor.DarkRed, walkable: true,
        environmentalDamageEnabled: true, environmentalDamageType: DamageType.Fire,
        fireDamageMultiplier: 1.5, waterDamageMultiplier: 0.5, iceDamageMultiplier: 0.5);

    public static readonly FloorTypeDefinition Mud = new(
        FloorType.Mud, ':', ConsoleColor.DarkYellow, walkable: true);

    public static readonly FloorTypeDefinition Sand = new(
        FloorType.Sand, '.', ConsoleColor.Yellow, walkable: true);

    public static readonly FloorTypeDefinition Grass = new(
        FloorType.Grass, '"', ConsoleColor.Green, walkable: true);

    public static readonly FloorTypeDefinition Swamp = new(
        FloorType.Swamp, ',', ConsoleColor.DarkGreen, walkable: true);

    public static readonly FloorTypeDefinition Ash = new(
        FloorType.Ash, ':', ConsoleColor.Gray, walkable: true);

    private static readonly IReadOnlyDictionary<FloorType, FloorTypeDefinition> All = new Dictionary<FloorType, FloorTypeDefinition>
    {
        [FloorType.Normal] = Normal,
        [FloorType.Water] = Water,
        [FloorType.Ice] = Ice,
        [FloorType.Fire] = Fire,
        [FloorType.Lava] = Lava,
        [FloorType.Mud] = Mud,
        [FloorType.Sand] = Sand,
        [FloorType.Grass] = Grass,
        [FloorType.Swamp] = Swamp,
        [FloorType.Ash] = Ash
    };

    /// <summary>The one FloorType value every "special" (non-Normal) room-floor generator picks from -- see DungeonGenerator.AssignSpecialFloors.</summary>
    public static readonly IReadOnlyList<FloorType> SpecialTypes = All.Keys.Where(t => t != FloorType.Normal).ToList();

    public static FloorTypeDefinition Get(FloorType floorType) => All[floorType];

    /// <summary>Whether standing on this floor type inflicts environmental damage each turn (Fire/Lava, today) -- the one source of truth pathing/AI code should check before "is this tile dangerous to linger/step on," rather than re-deriving its own list of hazardous FloorTypes.</summary>
    public static bool IsHazardous(FloorType floorType) => Get(floorType).EnvironmentalDamageEnabled;
}
