using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Central stat-scaling function for a Pet, given its owner's level -- the proposal's own
/// section 9 requirement: computing the pet's COMPLETE expected stats from level, rather than
/// scattered incremental adjustments, is what prevents drift after saving, loading, respawning,
/// or several owner level-ups landing at once. Every caller (creation, level-up, respawn, load)
/// funnels through SyncToLevel instead of poking Health/BasePhysicalAttackPower individually.
/// </summary>
public static class PetProgression
{
    public const int BaseMaxHp = 12;
    public const int HpPerLevel = 4;
    public const int BaseBitePower = 3;

    /// <summary>No initial scaling per the proposal's own recommendation -- "begin at a modest fixed value... unless playtesting shows it is needed."</summary>
    public const int FixedDefense = 1;
    public const int FixedAgility = 10;
    public const int FixedSpeed = 10;

    public static int MaxHpFor(int ownerLevel) => BaseMaxHp + (ownerLevel - 1) * HpPerLevel;

    public static int BitePowerFor(int ownerLevel) => BaseBitePower + (ownerLevel - 1) / 2;

    /// <summary>
    /// Recomputes Level/MaxHp/BitePower/Defense from scratch for `ownerLevel`, preserving the
    /// pet's current-health PERCENTAGE (rounded in its favor) rather than its raw HP -- so a
    /// level-up never fully heals a wounded pet, but always heals it for at least 1 (the
    /// proposal's own explicit guarantee). Replaces the HealthComponent outright (mirrors
    /// Monster.CreateBoss's own HP-rescale pattern) since IncreaseMax's delta-based growth
    /// doesn't fit a "recompute the complete expected stats" model.
    /// </summary>
    public static void SyncToLevel(Pet pet, int ownerLevel)
    {
        pet.Level = ownerLevel;
        int newMax = Math.Max(1, MaxHpFor(ownerLevel));

        if (pet.Health == null)
        {
            pet.Health = new HealthComponent(newMax);
        }
        else if (newMax != pet.Health.Max)
        {
            int oldMax = pet.Health.Max;
            int oldCurrent = pet.Health.Current;
            int newCurrent = oldMax > 0
                ? Math.Max(1, (int)Math.Ceiling(oldCurrent * (double)newMax / oldMax))
                : newMax;
            pet.Health = new HealthComponent(newMax);
            pet.Health.SetCurrent(Math.Min(newCurrent, newMax));
        }

        pet.BasePhysicalAttackPower = BitePowerFor(ownerLevel);
        pet.DefensePower = FixedDefense;
    }
}
