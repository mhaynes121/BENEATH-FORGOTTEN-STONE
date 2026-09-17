using BENEATH_FORGOTTEN_STONE.Entities;

namespace BENEATH_FORGOTTEN_STONE.Entities.Components;

/// <summary>
/// The five primary attributes (STR/CON/AGI/WIS/KNO), each a full
/// StatBlock. Dictionary-backed rather than five named properties so a
/// sixth attribute is a new PrimaryAttribute value, not a rewrite of
/// every consumer.
/// </summary>
public class CharacterStats
{
    private readonly Dictionary<PrimaryAttribute, StatBlock> stats = new();

    public StatBlock Get(PrimaryAttribute attribute)
    {
        if (!stats.TryGetValue(attribute, out var block))
        {
            block = new StatBlock();
            stats[attribute] = block;
        }
        return block;
    }

    public int Adjusted(PrimaryAttribute attribute) => Get(attribute).Adjusted;

    /// <summary>See StatBlock.AdjustedForAptitude -- the Ability Proficiency System's aptitude/lucky-insight formulas' own variant, excluding OtherModifier.</summary>
    public int AdjustedForAptitude(PrimaryAttribute attribute) => Get(attribute).AdjustedForAptitude;

    /// <summary>Rolls a fresh base value for every attribute within the race's stat range, then applies the class modifier. Used at character creation and on reroll.</summary>
    public static CharacterStats Roll(Race race, CharacterClass characterClass, Random rng)
    {
        var result = new CharacterStats();
        foreach (PrimaryAttribute attribute in Enum.GetValues<PrimaryAttribute>())
        {
            var block = result.Get(attribute);
            var range = race.StatRanges[attribute];
            block.BaseValue = rng.Next(range.Min, range.Max + 1); // inclusive of Max
            block.ClassModifier = characterClass.StatModifiers.GetValueOrDefault(attribute);
        }
        return result;
    }
}
