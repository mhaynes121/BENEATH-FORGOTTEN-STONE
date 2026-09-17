namespace BENEATH_FORGOTTEN_STONE.Core;

/// <summary>
/// Centralizes the Sleep command's recovery-percentage wording -- one tier table reused by every
/// wake path (manual, forced, ambush) instead of each caller hardcoding its own copy, the same
/// idea as Entities.CombatMessages.SeverityWord. Recovery is measured against what was missing
/// when sleep began (design doc: "health and mana restored / health and mana missing when sleep
/// began"), never exact point values.
/// </summary>
public static class SleepMessages
{
    private enum RecoveryTier
    {
        VeryLittle,
        Some,
        Most,
        NearlyAll
    }

    /// <summary>Tier cutoffs are a judgment call -- the design doc specifies the wording per tier but not the numeric boundaries.</summary>
    private static RecoveryTier Tier(double recovery) => recovery switch
    {
        < 0.25 => RecoveryTier.VeryLittle,
        < 0.60 => RecoveryTier.Some,
        < 0.90 => RecoveryTier.Most,
        _ => RecoveryTier.NearlyAll
    };

    /// <summary>What fraction of the HP+mana missing at sleep's start has been restored, combined into one number so a large mana pool or health pool never dominates unfairly. `missingAtStart` is always &gt; 0 in practice -- sleep is rejected outright when already full.</summary>
    public static double CalculateRecovery(int hpRestored, int manaRestored, int hpMissingAtStart, int manaMissingAtStart)
    {
        int missingAtStart = hpMissingAtStart + manaMissingAtStart;
        if (missingAtStart <= 0)
        {
            return 1.0;
        }
        return Math.Clamp((hpRestored + manaRestored) / (double)missingAtStart, 0.0, 1.0);
    }

    /// <summary>Full sentence for waking via a recognized command or Esc -- nothing else has already told the player they're awake, so this both announces it and describes the recovery.</summary>
    public static string ManualWakeSentence(double recovery) => Tier(recovery) switch
    {
        RecoveryTier.VeryLittle => "You wake before getting much rest. Your condition is scarcely improved.",
        RecoveryTier.Some => "You wake early, feeling somewhat restored.",
        RecoveryTier.Most => "You wake feeling greatly restored, though not yet at your best.",
        _ => "You wake feeling almost fully restored."
    };

    /// <summary>Short trailing sentence for a forced wake (damage, a new harmful effect, an ambush) -- appended after whatever message already explains what happened, so it never repeats "wake."</summary>
    public static string TrailingSentence(double recovery) => Tier(recovery) switch
    {
        RecoveryTier.VeryLittle => "Your condition is scarcely improved.",
        RecoveryTier.Some => "You feel somewhat restored.",
        RecoveryTier.Most => "You feel greatly restored, though not yet at your best.",
        _ => "You feel almost fully restored."
    };

    /// <summary>
    /// The ambush's opening line, by how many monsters actually got placed -- `bossName` non-null
    /// selects the boss-specific 3-tier wording instead (a boss always has a real generated name --
    /// see Monster.DisplayName -- so this always substitutes it for "a powerful foe" rather than
    /// ever using an anonymous phrasing).
    /// </summary>
    public static string AmbushMessage(int count, string bossName)
    {
        if (bossName != null)
        {
            return count switch
            {
                <= 1 => $"A terrible presence jolts you awake. {bossName} towers over your resting place.",
                <= 4 => $"A commanding roar tears you from your sleep! {bossName} has found you, and it did not come alone.",
                _ => $"A commanding roar tears you from your sleep! {bossName} and its hunting pack close around your resting place."
            };
        }

        return count switch
        {
            <= 1 => "A nearby presence jolts you awake! Something has found you while you slept.",
            2 => "Overlapping footsteps jolt you awake! You are no longer alone.",
            <= 4 => "Movement closes in from several directions, tearing you from your sleep!",
            _ => "A chorus of snarls erupts nearby! You wake to find a hunting pack closing around you."
        };
    }
}
