namespace BENEATH_FORGOTTEN_STONE.Entities.Skills;

/// <summary>Mirrors SpellCastingConfiguration minus ManaCost -- physical classes have no magic resource, so an active skill is gated by its cooldown alone.</summary>
public class SkillCastingConfiguration
{
    /// <summary>Turns before this skill can be used again. 0 = no cooldown.</summary>
    public int Cooldown { get; }

    public SkillCastingConfiguration(int cooldown = 0)
    {
        Cooldown = cooldown;
    }
}
