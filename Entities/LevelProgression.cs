namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Stateless XP-threshold formula: XPRequired = BaseXp * level^GrowthRate.
/// A power curve rather than an exponential-per-level one keeps
/// requirements from blowing up at high levels while still supporting
/// unlimited progression. Reads the per-class BaseXp/GrowthRate on
/// CharacterClass -- no per-level table, no class branching here.
/// </summary>
public static class LevelProgression
{
    public static long GetXpRequiredForLevel(CharacterClass characterClass, int level)
    {
        if (level <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(level), "Level must be positive.");
        }

        double required = characterClass.BaseXp * Math.Pow(level, characterClass.GrowthRate);
        return (long)Math.Ceiling(required);
    }

    public static long GetXpRequiredForNextLevel(CharacterClass characterClass, int currentLevel) =>
        GetXpRequiredForLevel(characterClass, currentLevel + 1);
}
