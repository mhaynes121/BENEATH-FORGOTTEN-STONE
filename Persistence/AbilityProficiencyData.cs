namespace BENEATH_FORGOTTEN_STONE.Persistence;

/// <summary>Serializable mirror of Entities.Proficiency.AbilityProficiency -- see SaveManager, Player.Proficiencies.</summary>
public class AbilityProficiencyData
{
    public double Proficiency { get; set; }
    public int ValidUses { get; set; }
    public int SuccessfulUses { get; set; }
    public int FailedUses { get; set; }
    public int LuckyInsights { get; set; }
}
