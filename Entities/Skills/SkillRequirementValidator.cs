namespace BENEATH_FORGOTTEN_STONE.Entities.Skills;

/// <summary>
/// Mirrors SpellRequirementValidator, but simpler: unlike a spell (which can
/// be learned from a scroll before reaching its required level -- see
/// Player.StartingSpellsFor), a skill is only ever known because
/// Player.GrantSkillsForLevel already granted it at the exact level it
/// unlocks, so a known skill is by definition already level-eligible. No
/// separate level check is needed.
/// </summary>
public static class SkillRequirementValidator
{
    public static bool CanUse(Player player, Skill skill, out string reason)
    {
        if (!player.KnownSkills.Contains(skill))
        {
            reason = $"You don't know {skill.Name}.";
            return false;
        }

        if (!skill.AllowedClasses.Contains(player.Class))
        {
            reason = $"Your class cannot use {skill.Name}.";
            return false;
        }

        reason = "";
        return true;
    }
}
