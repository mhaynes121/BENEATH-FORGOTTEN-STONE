namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Applies natural per-turn HP/Mana regeneration -- called exactly once from GameLoop's already-
/// centralized end-of-turn phase (the same "did the player just consume a normal turn" spot
/// that increments Player.TurnCount and runs EffectProcessor.Tick), never from individual
/// commands. Stat-based rather than tied to MaxHP/MaxMana on purpose: those grow unboundedly
/// with level, and a regen formula anchored to them would eventually let waiting trivialize
/// combat attrition -- see CharacterClass.HpRegenDivisor/ManaRegenDivisor for the actual tuning
/// knobs.
///
/// Most classes regenerate well under 1 point per turn at realistic stat values (e.g. a Mage's
/// 18 CON heals only 0.25 HP/turn), so the raw per-turn rate is a double, banked in
/// Player.HpRegenAccumulator/ManaRegenAccumulator across turns and only actually applied to
/// Health/Mana once the accumulated fraction crosses a whole point -- a 0.25/turn rate heals
/// exactly +1 every 4th turn, forever, rather than "almost always 0, occasionally forced up to a
/// minimum of 1" the way a plain per-turn floor would produce.
/// </summary>
public static class ResourceRegenerationCalculator
{
    /// <param name="inCombat">See GameLoop's "is the player in combat" check -- true if the player or an adjacent monster attacked this turn or last.</param>
    /// <param name="restMultiplier">Applied to the out-of-combat rate only -- see RegenerationConfig.SleepRegenMultiplier and GameLoop.CheckSleepInterrupts. Defaults to 1.0 (no change) so every existing caller is unaffected.</param>
    public static void ApplyRegen(Player player, bool inCombat, double restMultiplier = 1.0)
    {
        ApplyHp(player, inCombat, restMultiplier);
        ApplyMana(player, inCombat, restMultiplier);
    }

    private static void ApplyHp(Player player, bool inCombat, double restMultiplier)
    {
        player.HpRegenAccumulator += CalculateHpRegenRate(player, inCombat, restMultiplier);
        int whole = (int)Math.Floor(player.HpRegenAccumulator);
        if (whole <= 0)
        {
            return;
        }
        player.HpRegenAccumulator -= whole;
        player.Health.Heal(whole);
    }

    private static void ApplyMana(Player player, bool inCombat, double restMultiplier)
    {
        double rate = CalculateManaRegenRate(player, inCombat, restMultiplier);
        if (rate == 0 && player.ManaRegenAccumulator == 0)
        {
            return; // no ManaStat at all (Warrior/Thief) -- nothing to accumulate, ever
        }

        player.ManaRegenAccumulator += rate;
        int whole = (int)Math.Floor(player.ManaRegenAccumulator);
        if (whole <= 0)
        {
            return;
        }
        player.ManaRegenAccumulator -= whole;
        player.Mana.Restore(whole);
    }

    /// <summary>HP per turn = Adjusted(CON) / Class.HpRegenDivisor, halved further (via RegenerationConfig.CombatRegenMultiplier) while in combat, or scaled up by restMultiplier while resting (never both -- combat always ends sleep immediately). Internal so Diagnostics/SelfTest.cs can verify the raw rate directly, without needing to tick many turns through ApplyRegen's accumulator.</summary>
    internal static double CalculateHpRegenRate(Player player, bool inCombat, double restMultiplier = 1.0)
    {
        double rate = player.Stats.Adjusted(PrimaryAttribute.Constitution) / player.Class.HpRegenDivisor;
        return inCombat ? rate * RegenerationConfig.CombatRegenMultiplier : rate * restMultiplier;
    }

    /// <summary>0 for a class with no ManaStat at all (Warrior/Thief) -- see CharacterClass.ManaStat/IsSpellcaster. Otherwise Adjusted(ManaStat) / Class.ManaRegenDivisor, same combat/rest scaling as HP.</summary>
    internal static double CalculateManaRegenRate(Player player, bool inCombat, double restMultiplier = 1.0)
    {
        if (player.Class.ManaStat == null)
        {
            return 0;
        }

        double rate = player.Stats.Adjusted(player.Class.ManaStat.Value) / player.Class.ManaRegenDivisor;
        return inCombat ? rate * RegenerationConfig.CombatRegenMultiplier : rate * restMultiplier;
    }
}
