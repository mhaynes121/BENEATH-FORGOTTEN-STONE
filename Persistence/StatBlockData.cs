namespace BENEATH_FORGOTTEN_STONE.Persistence;

/// <summary>Serializable mirror of Entities.Components.StatBlock -- kept separate so the save-file contract doesn't couple directly to the domain type.</summary>
public class StatBlockData
{
    public int BaseValue { get; set; }
    public int ClassModifier { get; set; }
    public int EquipmentModifier { get; set; }
    public int OtherModifier { get; set; }
}
