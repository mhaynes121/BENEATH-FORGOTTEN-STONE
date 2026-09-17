using BENEATH_FORGOTTEN_STONE.Entities;
using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Core.Screens;

/// <summary>Rolls every primary attribute (STR/CON/AGI/WIS/KNO/CHA/LCK) after class/race selection, displays the breakdown, and lets the player keep or reroll indefinitely.</summary>
public static class StatRollScreen
{
    private static readonly Random rng = new();

    public static CharacterStats Prompt(CharacterClass characterClass, Race race)
    {
        var stats = CharacterStats.Roll(race, characterClass, rng);

        using var margin = new ScreenMargin();
        while (true)
        {
            ConsoleSafety.TryClear();
            ConsoleSafety.TrySetCursorPosition(0, 0);
            Console.WriteLine("Your rolled stats:");
            Console.WriteLine();

            foreach (var attribute in Enum.GetValues<PrimaryAttribute>())
            {
                var block = stats.Get(attribute);
                var range = race.StatRanges[attribute];
                Console.WriteLine(
                    $"{Abbreviate(attribute)}: {block.Adjusted} " +
                    $"(Race: {race.Name} {range.Min}-{range.Max} | Rolled: {block.BaseValue} | " +
                    $"Class: {characterClass.Name} {FormatModifier(block.ClassModifier)})");
            }

            Console.WriteLine();
            Console.Write("(K)eep these stats or (R)eroll? ");

            while (true)
            {
                var key = Console.ReadKey(intercept: true).Key;
                if (key == ConsoleKey.K || key == ConsoleKey.Enter)
                {
                    return stats;
                }
                if (key == ConsoleKey.R)
                {
                    stats = CharacterStats.Roll(race, characterClass, rng);
                    break;
                }
            }
        }
    }

    private static string Abbreviate(PrimaryAttribute attribute) => attribute switch
    {
        PrimaryAttribute.Strength => "STR",
        PrimaryAttribute.Constitution => "CON",
        PrimaryAttribute.Agility => "AGI",
        PrimaryAttribute.Wisdom => "WIS",
        PrimaryAttribute.Knowledge => "KNO",
        PrimaryAttribute.Charisma => "CHA",
        PrimaryAttribute.Luck => "LCK",
        _ => attribute.ToString()
    };

    private static string FormatModifier(int modifier) => modifier >= 0 ? $"+{modifier:00}" : $"-{Math.Abs(modifier):00}";
}
