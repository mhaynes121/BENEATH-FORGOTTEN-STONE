namespace BENEATH_FORGOTTEN_STONE.Entities.Proficiency;

/// <summary>
/// One player's progress with one ranked ability -- keyed by Skill.ProficiencyId/Spell.ProficiencyId
/// in Player.Proficiencies. Proficiency is the only value that actually matters mechanically;
/// ValidUses/SuccessfulUses/FailedUses/LuckyInsights are optional diagnostic counters the spec
/// explicitly allows (section 25) and the Adventure Record may someday surface, but nothing reads
/// them to compute Rank.
/// </summary>
public class AbilityProficiency
{
    public double Proficiency { get; set; }
    public ProficiencyRank Rank => ProficiencyScaling.RankFor(Proficiency);

    public int ValidUses { get; set; }
    public int SuccessfulUses { get; set; }
    public int FailedUses { get; set; }
    public int LuckyInsights { get; set; }
}
