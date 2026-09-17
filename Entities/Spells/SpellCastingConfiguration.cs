namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

/// <summary>
/// CastTime/Interruptible/NoiseRadius from the original spec are cut: every
/// cast in this game resolves instantly (there's no animation/interrupt
/// window in the turn loop to hang them on), and no required spell needs
/// them. Add them here if a future spell genuinely needs a cast delay.
/// </summary>
public class SpellCastingConfiguration
{
    public int ManaCost { get; }

    /// <summary>Turns before this spell can be cast again by the same caster. 0 = no cooldown.</summary>
    public int Cooldown { get; }

    public SpellCastingConfiguration(int manaCost, int cooldown = 0)
    {
        ManaCost = manaCost;
        Cooldown = cooldown;
    }
}
