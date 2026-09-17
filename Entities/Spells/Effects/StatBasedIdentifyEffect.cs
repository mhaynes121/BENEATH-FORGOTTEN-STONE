namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

/// <summary>
/// Thief's Identify skill counterpart to IdentifyEffect -- succeeds only on a stat-scaled roll
/// instead of always, mirroring Actor.PhysicalAttack's hit-chance formula shape (a base chance,
/// scaled by how far the chosen stat sits from an average-roll baseline of 10, clamped to a
/// min/max band) rather than the lockpicking-style d20-plus-stat-vs-difficulty roll, since there's
/// no natural per-item "difficulty" to compare against here. Sets the same
/// SpellCastResult.IdentifiedItemName/FailedIdentifyItemName fields IdentifyEffect and
/// SkillCaster/SpellCaster's BuildMessage already know how to report.
/// </summary>
public class StatBasedIdentifyEffect : SpellEffect
{
    private const double BaseChance = 0.50;
    private const double ScalingPerPoint = 0.03;
    private const double MinChance = 0.10;
    private const double MaxChance = 0.95;
    private const int BaselineStat = 10;

    public PrimaryAttribute Stat { get; }

    public StatBasedIdentifyEffect(PrimaryAttribute stat)
    {
        Stat = stat;
    }

    /// <summary>Exposed for Diagnostics/SelfTest.cs -- a pure function of the adjusted stat value, so the formula is directly testable without needing a full Player/roll to hit specific stat values.</summary>
    internal static double ChanceForStat(int adjustedStat) =>
        Math.Clamp(BaseChance + (adjustedStat - BaselineStat) * ScalingPerPoint, MinChance, MaxChance);

    public override void Apply(SpellCastingContext context, SpellCastResult result)
    {
        if (context.TargetItem == null || context.Caster is not Player player)
        {
            return;
        }

        double chance = ChanceForStat(player.Stats.Adjusted(Stat)) + context.RankScaling.PercentagePointAdjustment;
        if (context.Rng.NextDouble() >= chance)
        {
            result.FailedIdentifyItemName = context.TargetItem.DisplayName;
            return;
        }

        context.TargetItem.IsIdentified = true;
        result.IdentifiedItemName = context.TargetItem.Name;
    }
}
