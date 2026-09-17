namespace BENEATH_FORGOTTEN_STONE.Entities.Components;

public class HealthComponent
{
    public int Max { get; private set; }
    public int Current { get; private set; }

    public HealthComponent(int max)
    {
        Max = max;
        Current = max;
    }

    /// <summary>Grows the cap by `amount` and grants the same amount of usable HP -- does not restore to full, so existing damage taken is preserved.</summary>
    public void IncreaseMax(int amount)
    {
        if (amount <= 0)
        {
            return;
        }
        Max += amount;
        Current += amount;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0)
        {
            return;
        }
        Current = Math.Max(0, Current - amount);
    }

    public void Heal(int amount)
    {
        if (amount <= 0)
        {
            return;
        }
        Current = Math.Min(Max, Current + amount);
    }

    public void SetCurrent(int value) => Current = Math.Clamp(value, 0, Max);
}
