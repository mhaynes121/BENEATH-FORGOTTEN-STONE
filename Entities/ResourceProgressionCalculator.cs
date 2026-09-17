using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// HP/Mana gained on a single level-up. Uses the character's CURRENT
/// adjusted Constitution/Knowledge/Wisdom, so future equipment or buffs
/// affecting those stats change future gains automatically.
/// </summary>
public static class ResourceProgressionCalculator
{
    private const int BaseHpGain = 3;
    private const int BaseManaGain = 2;

    public static int CalculateHpGain(CharacterClass characterClass, CharacterStats stats)
    {
        int constitution = stats.Adjusted(PrimaryAttribute.Constitution);
        return BaseHpGain + (int)Math.Floor(constitution * characterClass.HpMultiplier);
    }

    /// <summary>0 for classes with no ManaStat (Warrior/Thief).</summary>
    public static int CalculateManaGain(CharacterClass characterClass, CharacterStats stats)
    {
        if (characterClass.ManaStat == null)
        {
            return 0;
        }

        int magicStat = stats.Adjusted(characterClass.ManaStat.Value);
        return BaseManaGain + (int)Math.Floor(magicStat * characterClass.ManaMultiplier);
    }
}
