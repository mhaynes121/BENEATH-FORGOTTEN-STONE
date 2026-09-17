using BENEATH_FORGOTTEN_STONE.Entities;
using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Core.Screens;

public static class ConfirmationScreen
{
    public static bool Confirm(string name, CharacterClass characterClass, Race race, CharacterStats stats)
    {
        using var margin = new ScreenMargin();
        ConsoleSafety.TryClear();
        ConsoleSafety.TrySetCursorPosition(0, 0);
        Console.WriteLine("Confirm your character:");
        Console.WriteLine();
        Console.WriteLine($"Name:  {name}");
        Console.WriteLine($"Class: {characterClass.Name}");
        Console.WriteLine($"Race:  {race.Name}");
        Console.WriteLine();
        Console.WriteLine(
            $"STR: {stats.Adjusted(PrimaryAttribute.Strength)}   " +
            $"CON: {stats.Adjusted(PrimaryAttribute.Constitution)}   " +
            $"AGI: {stats.Adjusted(PrimaryAttribute.Agility)}   " +
            $"WIS: {stats.Adjusted(PrimaryAttribute.Wisdom)}   " +
            $"KNO: {stats.Adjusted(PrimaryAttribute.Knowledge)}   " +
            $"CHA: {stats.Adjusted(PrimaryAttribute.Charisma)}   " +
            $"LCK: {stats.Adjusted(PrimaryAttribute.Luck)}");
        Console.WriteLine();
        Console.Write("Begin adventure? (y/n): ");

        while (true)
        {
            var key = Console.ReadKey(intercept: true).Key;
            if (key == ConsoleKey.Y)
            {
                return true;
            }
            if (key == ConsoleKey.N)
            {
                return false;
            }
        }
    }
}
