namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Centralized tile-attuned-monster tuning knobs -- mirrors BossConfig/TraderConfig/
/// RegenerationConfig's style (plain constants, no behavior). See Monster's PreferredFloorType/
/// ElementalAffinity/OpposingElement, Entities/AI/FloorAttunedAI, Entities/AI/TerrainPathfinder,
/// and Entities/FloorDamageCalculator.
/// </summary>
public static class FloorAttunementConfig
{
    /// <summary>Fraction of MaxHP a floor-attuned monster loses per completed turn spent off its PreferredFloorType -- see Core/EnvironmentalFloorEffects.ApplyFloorAttunementAttrition. Always calculated from MaxHP, never CurrentHP, and always at least 1 (see the same method).</summary>
    public const double DefaultOffPreferredFloorDamagePercent = 0.025;

    /// <summary>
    /// A floor-attuned monster's resistance to its own OpposingElement is always penalized by
    /// this much, everywhere -- representing "pulled away from its terrain, quite vulnerable."
    /// See Monster.GetEffectiveResistance. Combined with PreferredFloorEnvironmentalResistanceBonus
    /// below, this exactly reproduces the old special 0.5x-on/1.5x-off damage multiplier this
    /// replaced (Resistance System spec sections 35-37: represent terrain protection as
    /// resistance, not a second parallel mechanic) -- resistance -50 is precisely a 1.5x damage
    /// multiplier, and -50+100=+50 is precisely a 0.5x multiplier.
    /// </summary>
    public const int OpposingElementBaseResistance = -50;

    /// <summary>Added on top of OpposingElementBaseResistance only while standing on PreferredFloorType -- see Monster.GetEffectiveResistance and OpposingElementBaseResistance's own doc comment for the exact multiplier equivalence this preserves.</summary>
    public const int PreferredFloorEnvironmentalResistanceBonus = 100;

    /// <summary>TerrainPathfinder's per-step cost for moving onto a floor-attuned monster's own PreferredFloorType.</summary>
    public const int PreferredFloorPathCost = 1;

    /// <summary>TerrainPathfinder's per-step cost for moving onto anything else -- steep enough that a slightly longer route through preferred terrain is chosen over a short cut across it, but never infinite (see TerrainPathfinder -- non-preferred tiles stay traversable, never impassable).</summary>
    public const int NonPreferredFloorPathCost = 5;

    /// <summary>Per monster picked in a special-floor room (see DungeonGenerator.SpawnMonsters), the chance that pick draws from the room's FloorType-matching attuned roster instead of the level's normal difficulty-banded roster. Well under 1.0 so an attuned room still mixes in ordinary monsters (design spec section 37) rather than being entirely single-species.</summary>
    public const double FloorAttunedSpawnChance = 0.5;
}
