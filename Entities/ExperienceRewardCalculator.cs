namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Centralizes enemy XP so combat/damage code never needs to know a
/// reward formula exists -- GameLoop's death-sweep is the only caller.
/// XPReward = BaseEnemyXp * DifficultyRating * DepthModifier *
/// LevelDifferenceModifier, unless the monster specifies an explicit
/// override (story bosses, uniques, ...).
/// </summary>
public static class ExperienceRewardCalculator
{
    private const double BaseEnemyXp = 100;
    private const double DepthExponent = 0.75;

    public static long CalculateEnemyExperience(Monster enemy, Player player, int dungeonDepth)
    {
        if (enemy.XpRewardOverride.HasValue)
        {
            return enemy.XpRewardOverride.Value;
        }

        double depthModifier = CalculateDepthModifier(dungeonDepth);
        double levelDifferenceModifier = CalculateLevelDifferenceModifier(player.Level, enemy.Level);
        double xp = BaseEnemyXp * enemy.DifficultyRating * depthModifier * levelDifferenceModifier;

        return Math.Max(0, (long)Math.Round(xp));
    }

    public static double CalculateDepthModifier(int depth) => Math.Pow(Math.Max(1, depth), DepthExponent);

    /// <summary>Discourages farming enemies far below the player's level without penalizing fights against equal-or-stronger ones.</summary>
    public static double CalculateLevelDifferenceModifier(int playerLevel, int enemyLevel)
    {
        int levelsBelow = playerLevel - enemyLevel;
        if (levelsBelow <= 0) return 1.0;
        if (levelsBelow <= 3) return 0.90;
        if (levelsBelow <= 6) return 0.60;
        if (levelsBelow <= 10) return 0.25;
        return 0.05;
    }
}
