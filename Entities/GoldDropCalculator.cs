namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>Enemy gold reward, kept separate from XP so either can be rebalanced independently.</summary>
public static class GoldDropCalculator
{
    private const double GoldDropMultiplier = 0.5;
    private const int MinimumGoldDrop = 0;

    public static long CalculateGoldDrop(Monster enemy, Random rng)
    {
        int maximum = Math.Max(MinimumGoldDrop, (int)Math.Floor(enemy.Level * GoldDropMultiplier));
        return rng.Next(MinimumGoldDrop, maximum + 1);
    }
}
