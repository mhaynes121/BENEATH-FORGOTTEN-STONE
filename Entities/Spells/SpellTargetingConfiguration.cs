using BENEATH_FORGOTTEN_STONE.Entities.Proficiency;

namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

public enum SpellTargetType
{
    Self,
    SingleTarget,
    Tile,

    /// <summary>Targets an item in the caster's own inventory/equipment rather than an Actor or a location -- see SpellCastingContext.TargetItem, TargetResolver's no-op case for it, and GameLoop.HandleCastSpell's item-picker branch. Only Identify uses this today.</summary>
    Item,

    /// <summary>An AoE centered on the CASTER's own current position (not a resolved target/tile) -- see AreaOfEffectRadius (used as the radius here, not an add-on) and CreatureTypeFilter. Only Turn Undead uses this today; unlike Self, this collects every eligible actor within range, not just the caster.</summary>
    SelfCenteredArea
}

/// <summary>
/// Only Circle-shaped AoE is supported (AreaOfEffectRadius around the
/// resolved target/tile) -- Line/Cone/Square are cut since no required
/// spell needs them; a real shape enum can replace the bare radius later
/// if one does.
/// </summary>
public class SpellTargetingConfiguration
{
    public SpellTargetType TargetType { get; }
    public int Range { get; }
    public int AreaOfEffectRadius { get; }
    public bool RequiresLineOfSight { get; }

    /// <summary>
    /// Ability Proficiency System: true only for the handful of abilities whose spec profile
    /// explicitly lists "Range" as a scaled category (Teleport, Blink, Blink Step, Shadowstep) --
    /// TargetResolver scales Range by the caster's RankScaling before checking it. False (the
    /// default) for every other ability, so an attack/utility spell's max range never
    /// accidentally shrinks for a low-rank caster -- the spec never calls that out.
    /// </summary>
    public bool ScalesRangeWithProficiency { get; }

    /// <summary>SelfCenteredArea only: restricts eligible actors to this CreatureType (e.g. Turn Undead's CreatureType.Undead) -- null means no filter (every actor in range/LOS is eligible). Meaningless for every other TargetType.</summary>
    public CreatureType? CreatureTypeFilter { get; }

    public SpellTargetingConfiguration(
        SpellTargetType targetType, int range, int areaOfEffectRadius = 0, bool requiresLineOfSight = false,
        bool scalesRangeWithProficiency = false, CreatureType? creatureTypeFilter = null)
    {
        TargetType = targetType;
        Range = range;
        AreaOfEffectRadius = areaOfEffectRadius;
        RequiresLineOfSight = requiresLineOfSight;
        ScalesRangeWithProficiency = scalesRangeWithProficiency;
        CreatureTypeFilter = creatureTypeFilter;
    }

    /// <summary>Range, scaled by rank if (and only if) ScalesRangeWithProficiency is set -- the single place both the initial ray-based target acquisition (GameLoop.BuildContext/BuildSkillContext) and TargetResolver's own validation compute the effective range, so a rank change actually widens/narrows what the player can select, not just what later validates.</summary>
    public int EffectiveRange(RankScaling rankScaling) =>
        ScalesRangeWithProficiency ? rankScaling.ScaleDuration(Range) : Range;
}
