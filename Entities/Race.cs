using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// A player character race (Human, Elf, Dwarf, Halfling, ...). Rather than
/// a flat additive modifier, a race defines the inclusive roll RANGE for
/// each stat -- e.g. a Dwarf's STR is rolled somewhere in 7-18, not rolled
/// 1-18 then adjusted. See CharacterStats.Roll for how the range combines
/// with CharacterClass.StatModifiers.
/// </summary>
public class Race
{
    public string Name { get; }
    public string Description { get; }
    public IReadOnlyDictionary<PrimaryAttribute, StatRange> StatRanges { get; }

    /// <summary>Small innate elemental/magic resistance tendencies -- see the Resistance System spec. Defaults to ResistanceSet.Zero (no effect) so a race that hasn't been given explicit values behaves exactly as before this existed.</summary>
    public ResistanceSet ResistanceModifiers { get; }

    private Race(string name, string description, IReadOnlyDictionary<PrimaryAttribute, StatRange> statRanges,
        ResistanceSet resistanceModifiers = null)
    {
        Name = name;
        Description = description;
        StatRanges = statRanges;
        ResistanceModifiers = resistanceModifiers ?? ResistanceSet.Zero;
    }

    public static readonly Race Human = new(
        "Human", "Balanced and adaptable, favored by no particular calling.",
        new Dictionary<PrimaryAttribute, StatRange>
        {
            [PrimaryAttribute.Strength] = new(5, 17),
            [PrimaryAttribute.Constitution] = new(5, 17),
            [PrimaryAttribute.Agility] = new(5, 17),
            [PrimaryAttribute.Wisdom] = new(5, 17),
            [PrimaryAttribute.Knowledge] = new(5, 17),
            [PrimaryAttribute.Charisma] = new(5, 17),
            [PrimaryAttribute.Luck] = new(5, 17)
        });

    public static readonly Race Dwarf = new(
        "Dwarf", "Stout and resilient, built to endure what others cannot.",
        new Dictionary<PrimaryAttribute, StatRange>
        {
            [PrimaryAttribute.Strength] = new(7, 18),
            [PrimaryAttribute.Constitution] = new(8, 18),
            [PrimaryAttribute.Agility] = new(3, 14),
            [PrimaryAttribute.Wisdom] = new(4, 15),
            [PrimaryAttribute.Knowledge] = new(4, 15),
            [PrimaryAttribute.Charisma] = new(3, 14),
            [PrimaryAttribute.Luck] = new(4, 15)
        },
        resistanceModifiers: new ResistanceSet(fire: 3, poison: 5, magic: -3));

    public static readonly Race Elf = new(
        "Elf", "Graceful and quick-witted, at home with magic and the bow.",
        new Dictionary<PrimaryAttribute, StatRange>
        {
            [PrimaryAttribute.Strength] = new(3, 14),
            [PrimaryAttribute.Constitution] = new(4, 15),
            [PrimaryAttribute.Agility] = new(8, 18),
            [PrimaryAttribute.Wisdom] = new(7, 18),
            [PrimaryAttribute.Knowledge] = new(7, 18),
            [PrimaryAttribute.Charisma] = new(7, 18),
            [PrimaryAttribute.Luck] = new(6, 17)
        },
        resistanceModifiers: new ResistanceSet(ice: 3, poison: -3, magic: 5));

    public static readonly Race Halfling = new(
        "Halfling", "Small and nimble, quicker underfoot than most ever expect.",
        new Dictionary<PrimaryAttribute, StatRange>
        {
            [PrimaryAttribute.Strength] = new(3, 14),
            [PrimaryAttribute.Constitution] = new(5, 16),
            [PrimaryAttribute.Agility] = new(9, 18),
            [PrimaryAttribute.Wisdom] = new(7, 17),
            [PrimaryAttribute.Knowledge] = new(6, 16),
            [PrimaryAttribute.Charisma] = new(7, 17),
            [PrimaryAttribute.Luck] = new(9, 18)
        },
        resistanceModifiers: new ResistanceSet(shock: -3, poison: 5, magic: 3));

    public static readonly IReadOnlyList<Race> All = new[] { Human, Elf, Dwarf, Halfling };
}
