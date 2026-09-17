namespace BENEATH_FORGOTTEN_STONE.Entities.Proficiency;

/// <summary>
/// Five-rank ability proficiency progression (Ability Proficiency System spec) -- the player never
/// sees the underlying hidden proficiency total, only this rank name beside the ability. Computed
/// from ProficiencyScaling.RankFor, never stored directly, so rebalancing the thresholds in
/// ProficiencyConfig never requires a save migration.
/// </summary>
public enum ProficiencyRank
{
    Novice,
    Practiced,
    Proficient,
    Expert,
    Master
}
