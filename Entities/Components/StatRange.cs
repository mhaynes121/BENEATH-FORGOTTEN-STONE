namespace BENEATH_FORGOTTEN_STONE.Entities.Components;

/// <summary>Inclusive roll range for one stat -- see Race.StatRanges and CharacterStats.Roll.</summary>
public readonly struct StatRange
{
    public int Min { get; }
    public int Max { get; }

    public StatRange(int min, int max)
    {
        Min = min;
        Max = max;
    }
}
