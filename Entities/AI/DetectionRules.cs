using BENEATH_FORGOTTEN_STONE.Entities.Proficiency;

namespace BENEATH_FORGOTTEN_STONE.Entities.AI;

/// <summary>Centralizes the detection radius ChaseAI and SpellCasterAI previously duplicated as two independent constants, plus Sneak's halving -- one source of truth instead of two numbers that could drift apart.</summary>
public static class DetectionRules
{
    public const int BaseRadius = 8;

    /// <summary>
    /// Sneak's own radius no longer just halves BaseRadius -- it reads the Ability Proficiency
    /// System's bespoke Sneak table (5/5/4/3/2 by rank; Proficient's "4" matches the old fixed
    /// BaseRadius/2 exactly, so an un-ranked/Proficient sneaker sees no behavior change).
    /// </summary>
    public static int EffectiveRadius(Actor target) =>
        target is Player { IsSneaking: true } player
            ? ProficiencyScaling.SneakDetectionRadius(player.RankOf(SkillCatalog.Sneak.ProficiencyId))
            : BaseRadius;
}
