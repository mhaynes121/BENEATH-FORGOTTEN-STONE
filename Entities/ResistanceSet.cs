namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// A reusable bundle of the six ResistanceType values -- used identically for a Race's or
/// CharacterClass's innate modifiers, an Item's ResistanceModifiers, and a Monster's
/// BaseResistances, so there's exactly one shape for "a set of resistance numbers" rather than
/// six unrelated top-level fields repeated across every one of those types. Values here are
/// unclamped modifier CONTRIBUTIONS (can be any sign/magnitude) -- clamping to
/// ResistanceConfig.Minimum/MaximumResistance only ever happens once, on the final summed
/// effective value (see ResistanceCalculator), never on an individual source.
/// </summary>
public class ResistanceSet
{
    public int Fire { get; }
    public int Water { get; }
    public int Ice { get; }
    public int Shock { get; }
    public int Poison { get; }
    public int Magic { get; }

    /// <summary>All-zero -- the default for a Race/Class/Item/Monster that doesn't declare any resistance modifiers, so old data (and anything simply never given one) behaves as "no effect" rather than needing a null check everywhere.</summary>
    public static readonly ResistanceSet Zero = new();

    public ResistanceSet(int fire = 0, int water = 0, int ice = 0, int shock = 0, int poison = 0, int magic = 0)
    {
        Fire = fire;
        Water = water;
        Ice = ice;
        Shock = shock;
        Poison = poison;
        Magic = magic;
    }

    public int Get(ResistanceType type) => type switch
    {
        ResistanceType.Fire => Fire,
        ResistanceType.Water => Water,
        ResistanceType.Ice => Ice,
        ResistanceType.Shock => Shock,
        ResistanceType.Poison => Poison,
        ResistanceType.Magic => Magic,
        _ => 0
    };

    /// <summary>Adds two sets component-wise -- e.g. combining several equipped items' ResistanceModifiers into one subtotal.</summary>
    public static ResistanceSet Combine(ResistanceSet a, ResistanceSet b) => new(
        a.Fire + b.Fire, a.Water + b.Water, a.Ice + b.Ice, a.Shock + b.Shock, a.Poison + b.Poison, a.Magic + b.Magic);
}
