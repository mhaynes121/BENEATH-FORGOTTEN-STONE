using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// One centralized table answering "what beats this element" -- used by the tile-attuned
/// monster system (Monster.ElementalAffinity/OpposingElement) instead of scattering
/// Fire-vs-Water/Earth-vs-Air comparisons through combat code. Deliberately reuses DamageType
/// (extended with Earth/Air) rather than a separate ElementalAffinity enum, since Fire/Water/Ice
/// already exist there and drive FloorTypeDefinition's own multipliers -- one enum, one set of
/// elemental concepts.
///
/// Fire and Water oppose each other mutually, as do Earth and Air. Ice's opposition is
/// one-directional: Fire melts Ice, so GetOpposingElement(Ice) is Fire, but Fire's own listed
/// opposite stays Water (its mutual pair) -- Fire isn't specifically weak to Ice in return. This
/// keeps the table a simple lookup rather than requiring every element to pair with exactly one
/// other.
/// </summary>
public static class ElementalOpposition
{
    private static readonly IReadOnlyDictionary<DamageType, DamageType> Oppositions = new Dictionary<DamageType, DamageType>
    {
        [DamageType.Fire] = DamageType.Water,
        [DamageType.Water] = DamageType.Fire,
        [DamageType.Earth] = DamageType.Air,
        [DamageType.Air] = DamageType.Earth,
        [DamageType.Ice] = DamageType.Fire
    };

    /// <summary>Null in, null out -- an element with no defined opposite (e.g. Physical, or a monster with no ElementalAffinity at all) simply has no OpposingElement.</summary>
    public static DamageType? GetOpposingElement(DamageType? element) =>
        element.HasValue && Oppositions.TryGetValue(element.Value, out var opposing) ? opposing : null;
}
