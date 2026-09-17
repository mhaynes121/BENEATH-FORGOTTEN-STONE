namespace BENEATH_FORGOTTEN_STONE.Entities.Components;

public class ManaComponent
{
    public int Max { get; private set; }
    public int Current { get; private set; }

    public ManaComponent(int max)
    {
        Max = max;
        Current = max;
    }

    /// <summary>Grows the cap by `amount` and grants the same amount of usable mana -- does not restore to full.</summary>
    public void IncreaseMax(int amount)
    {
        if (amount <= 0)
        {
            return;
        }
        Max += amount;
        Current += amount;
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0 || amount > Current)
        {
            return false;
        }
        Current -= amount;
        return true;
    }

    public void Restore(int amount)
    {
        if (amount <= 0)
        {
            return;
        }
        Current = Math.Min(Max, Current + amount);
    }

    public void SetCurrent(int value) => Current = Math.Clamp(value, 0, Max);
}
