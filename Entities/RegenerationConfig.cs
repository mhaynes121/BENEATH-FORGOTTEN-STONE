namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>Tunable knob for natural HP/Mana regeneration -- see ResourceRegenerationCalculator. Per-class rates themselves live on CharacterClass.HpRegenDivisor/ManaRegenDivisor (a divisor is a per-class balance choice, not a single global one), leaving only the combat penalty as a true global constant.</summary>
public static class RegenerationConfig
{
    /// <summary>Applied on top of (not instead of) the normal per-turn rate while in combat -- see GameLoop's "is the player in combat" check. A trickle, not a stop: 1/10th of the normal rate, so actively fighting something no longer lets natural regen meaningfully offset the damage being traded.</summary>
    public const double CombatRegenMultiplier = 0.10;

    /// <summary>Applied to the out-of-combat rate while the Sleep command is active -- see ResourceRegenerationCalculator.ApplyRegen's restMultiplier parameter and GameLoop.CheckSleepInterrupts. Combat can't coexist with sleeping (any attack wakes the player immediately), so this and CombatRegenMultiplier are never both in effect at once.</summary>
    public const double SleepRegenMultiplier = 4.0;
}
