namespace BENEATH_FORGOTTEN_STONE.Entities.Proficiency;

/// <summary>
/// Precomputed multipliers/adjustments for one specific rank -- attached to SpellCastingContext
/// (see SpellCastingContext.RankScaling) so every SpellEffect.Apply implementation can read it
/// without any change to the Apply method signature itself. Neutral is the "no rank tracked"
/// default: every multiplier is a no-op (1.0/0/0), reproducing exactly today's numbers -- so any
/// existing or future caller that never sets RankScaling (monster spellcasting, in particular)
/// is completely unaffected by this feature.
/// </summary>
public class RankScaling
{
    public static readonly RankScaling Neutral = new(ProficiencyRank.Proficient);

    public ProficiencyRank Rank { get; }
    public double StandardMultiplier { get; }
    public double PercentagePointAdjustment { get; }
    public double SecondaryEffectChance { get; }
    public int D20Adjustment { get; }

    public RankScaling(ProficiencyRank rank)
    {
        Rank = rank;
        StandardMultiplier = ProficiencyScaling.StandardMultiplier(rank);
        PercentagePointAdjustment = ProficiencyScaling.PercentagePointAdjustment(rank);
        SecondaryEffectChance = ProficiencyScaling.SecondaryEffectChance(rank);
        D20Adjustment = ProficiencyScaling.D20Adjustment(rank);
    }

    /// <summary>Scales a duration by StandardMultiplier, rounded to the nearest whole turn/tile, never below 1 -- the spec's own rounding rule for every duration/range value.</summary>
    public int ScaleDuration(int baseDuration) => Math.Max(1, (int)Math.Round(baseDuration * StandardMultiplier));

    /// <summary>Scales a magnitude (damage/healing/mana/buff/debuff amount) by StandardMultiplier, rounded to the nearest whole number.</summary>
    public int ScaleMagnitude(int baseMagnitude) => (int)Math.Round(baseMagnitude * StandardMultiplier);
}
