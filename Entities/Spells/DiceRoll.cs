namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

/// <summary>Standard tabletop dice notation, e.g. 2d6+3.</summary>
public class DiceRoll
{
    public int NumberOfDice { get; }
    public int NumberOfSides { get; }
    public int Modifier { get; }

    public DiceRoll(int numberOfDice, int numberOfSides, int modifier = 0)
    {
        NumberOfDice = numberOfDice;
        NumberOfSides = numberOfSides;
        Modifier = modifier;
    }

    public int Roll(Random rng)
    {
        int total = Modifier;
        for (int i = 0; i < NumberOfDice; i++)
        {
            total += rng.Next(1, NumberOfSides + 1);
        }
        return total;
    }
}
