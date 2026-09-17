using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Entities.Proficiency;

/// <summary>
/// Ability Proficiency System's award pipeline (spec section 27, steps 6-15) -- called once from
/// GameLoop right after SkillCaster.Cast/SpellCaster.Cast returns for any ranked ability (a
/// non-null ProficiencyId). Bails immediately, with zero side effects, for an unsuccessful cast,
/// a detected non-legitimate use (a failed stat-based roll -- section 16), or an ability already
/// at Master rank -- there is nothing left to award or report in any of those cases.
/// </summary>
public static class ProficiencyTracker
{
    public static void Process(
        Player player, string proficiencyId, string abilityName, PrimaryAttribute governingAttribute, int abilityLevel,
        SpellCastResult result, int? targetMonsterLevel, Random rng, Action<string, ConsoleColor> addMessage)
    {
        if (string.IsNullOrEmpty(proficiencyId) || !result.Success)
        {
            return;
        }

        // Section 16's cheaply-detectable "non-legitimate use" case: a failed stat-based roll
        // (Thief Identify) reflects no practical progress, unlike a physical skill's own attack
        // roll missing, which still counts as a legitimate (if unsuccessful) attempt below.
        if (result.FailedIdentifyItemName != null)
        {
            return;
        }

        if (!player.Proficiencies.TryGetValue(proficiencyId, out var proficiency))
        {
            proficiency = new AbilityProficiency();
            player.Proficiencies[proficiencyId] = proficiency;
        }

        if (proficiency.Rank == ProficiencyRank.Master)
        {
            return; // Master stops all further gains -- nothing left to track.
        }

        var oldRank = proficiency.Rank;
        bool isFailure = result.AttackHit == false;

        double basePoints = isFailure ? ProficiencyConfig.FailurePoints : ProficiencyConfig.SuccessPoints;
        double challengeMultiplier = targetMonsterLevel.HasValue
            ? ProficiencyScaling.ChallengeMultiplier(targetMonsterLevel.Value, player.Level)
            : 1.0; // self/utility abilities have no target to be "too easy" relative to -- full credit.

        int adjustedAttribute = player.Stats.AdjustedForAptitude(governingAttribute);
        double aptitudeRate = ProficiencyScaling.AptitudeLearningRate(adjustedAttribute);
        double levelRate = ProficiencyScaling.CatalogLevelLearningModifier(abilityLevel);

        proficiency.ValidUses++;
        if (isFailure) proficiency.FailedUses++; else proficiency.SuccessfulUses++;
        proficiency.Proficiency += basePoints * challengeMultiplier * aptitudeRate * levelRate;

        int adjustedLuck = player.Stats.AdjustedForAptitude(PrimaryAttribute.Luck);
        bool luckyInsight = challengeMultiplier > 0 && rng.NextDouble() < ProficiencyScaling.LuckyInsightChance(adjustedLuck);
        if (luckyInsight)
        {
            proficiency.LuckyInsights++;
            proficiency.Proficiency += ProficiencyConfig.LuckyInsightBonusPoints; // flat -- never scaled by aptitude/level.
        }

        var newRank = proficiency.Rank;
        bool rankedUp = newRank > oldRank;

        if (!rankedUp && !luckyInsight)
        {
            return;
        }

        if (rankedUp && newRank == ProficiencyRank.Master)
        {
            string suffix = luckyInsight ? " A sudden flash of insight carried you the rest of the way." : "";
            addMessage($"You have achieved Mastery of {abilityName}!{suffix}", ConsoleColor.Yellow);
        }
        else if (rankedUp)
        {
            // A rank-up message subsumes the ordinary lucky-insight line when both happen on the
            // same use (section 13) -- one combined yellow line, not two.
            string suffix = luckyInsight ? " -- a flash of insight speeds your progress!" : ".";
            addMessage($"Your skill with {abilityName} has grown to {newRank}{suffix}", ConsoleColor.Yellow);
        }
        else
        {
            addMessage(LuckyInsightFlavorLine(isFailure, result, rng), ConsoleColor.Yellow);
        }
    }

    private static readonly string[] AttackFailedFlavor =
    {
        "Even in failure, you notice something you can use next time.",
        "The miss teaches you something a clean hit wouldn't have.",
        "You catch your own mistake mid-swing -- a flash of insight."
    };

    private static readonly string[] AttackSucceededFlavor =
    {
        "Everything clicks into place for a moment -- a flash of insight.",
        "You feel a sudden, sharper understanding of the technique.",
        "That one lands better than it should have -- you take note why."
    };

    private static readonly string[] SpellFlavor =
    {
        "The weave of magic momentarily reveals itself to you.",
        "A flash of arcane insight sharpens your understanding of the spell.",
        "For an instant, the spell's true shape is obvious to you."
    };

    private static readonly string[] UtilityFlavor =
    {
        "A small flash of insight makes the trick suddenly obvious.",
        "You notice a shortcut you'd missed every time before.",
        "Something about the technique suddenly makes more sense."
    };

    private static string LuckyInsightFlavorLine(bool isFailure, SpellCastResult result, Random rng)
    {
        string[] pool = result.AttackHit.HasValue
            ? (isFailure ? AttackFailedFlavor : AttackSucceededFlavor)
            : result.IdentifiedItemName != null
                ? UtilityFlavor
                : SpellFlavor;
        return pool[rng.Next(pool.Length)];
    }
}
