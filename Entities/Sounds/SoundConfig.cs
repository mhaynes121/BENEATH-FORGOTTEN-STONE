using BENEATH_FORGOTTEN_STONE.Dungeon;

namespace BENEATH_FORGOTTEN_STONE.Entities.Sounds;

/// <summary>Centralized ambient-sound tuning knobs -- mirrors ResistanceConfig/FloorAttunementConfig/BossConfig's plain-constants style, so every number the design spec calls out as configurable lives in one place instead of scattered through Core/SoundSystem.cs.</summary>
public static class SoundConfig
{
    // --- Silence/cooldown pacing -- see Core/SoundSystem.cs -----------------------------

    /// <summary>Turns of silence before the rising-chance bonus starts accumulating at all.</summary>
    public const int SoundGracePeriodTurns = 20;

    /// <summary>After any sound displays, ordinary ambient checks are hard-blocked for a randomly chosen number of turns in this range -- the primary anti-spam measure (design spec section 22).</summary>
    public const int MinimumSoundCooldownTurns = 15;
    public const int MaximumSoundCooldownTurns = 30;

    /// <summary>Added to a candidate's chance for every turn of silence beyond SoundGracePeriodTurns.</summary>
    public const double SoundChanceIncreasePerSilentTurn = 0.0015;

    /// <summary>Hard ceiling on any single candidate's chance, however long the silence -- prolonged quiet becomes progressively less likely, never certain (design spec section 28).</summary>
    public const double MaximumAmbientSoundChance = 0.30;

    // --- Hearing range per Loudness -- see RangeFor ------------------------------------

    public const int QuietSoundRange = 5;
    public const int NormalSoundRange = 8;
    public const int LoudSoundRange = 12;
    public const int VeryLoudSoundRange = 15;

    public static int RangeFor(SoundLoudness loudness) => loudness switch
    {
        SoundLoudness.Quiet => QuietSoundRange,
        SoundLoudness.Normal => NormalSoundRange,
        SoundLoudness.Loud => LoudSoundRange,
        _ => VeryLoudSoundRange
    };

    // --- Distance attenuation -- see SoundSystem.DistancePenalty -----------------------
    // Banded rather than a smooth curve, matching the design spec's own suggested bands
    // (section 14): close sources barely lose any chance, far ones lose noticeably more.

    public const int NearDistanceBand = 4;
    public const int MidDistanceBand = 8;
    public const int FarDistanceBand = 12;

    public const double NearDistancePenalty = 0.0;
    public const double MidDistancePenalty = 0.03;
    public const double FarDistancePenalty = 0.06;
    public const double VeryFarDistancePenalty = 0.09;

    // --- Category selection weights -- see SoundSystem.SelectAmongSuccessfulCandidates -
    // A nearby boss should generally win out over a generic ambient bat sound when both
    // succeed their own chance roll in the same turn (design spec section 35).

    public const double EnvironmentSelectionWeight = 3.0;
    public const double BossSelectionWeight = 4.0;
    public const double CreatureAmbientSelectionWeight = 2.0;

    /// <summary>How many of the most recently displayed sound keys are avoided when an alternative candidate exists (design spec section 41).</summary>
    public const int RecentSoundMemoryCount = 3;

    /// <summary>Per-FloorType chance that a newly generated special-floor room also receives an ambient sound source -- not every WATER room should audibly announce itself (design spec section 3). Only FloorTypes with an EnvironmentSoundCatalog entry are looked up here; anything else (or a miss in this table) means 0.</summary>
    private static readonly IReadOnlyDictionary<FloorType, double> EnvironmentSoundSourceChanceByFloorType = new Dictionary<FloorType, double>
    {
        [FloorType.Water] = 0.50,
        [FloorType.Fire] = 0.50,
        [FloorType.Lava] = 0.65,
        [FloorType.Swamp] = 0.35,
        [FloorType.Ice] = 0.30
    };

    public static double EnvironmentSoundSourceChance(FloorType floorType) => EnvironmentSoundSourceChanceByFloorType.GetValueOrDefault(floorType);

    /// <summary>
    /// WIS 1-3 -5%, 4-6 -3%, 7-9 -1%, 10-12 0%, 13-15 +2%, 16-17 +4%, 18 +6% -- the design
    /// spec's own recommended table, expressed as a fraction (0.05 = +5%) rather than a whole
    /// percentage so it composes directly with the other additive terms in
    /// SoundSystem.CalculateSoundChance. Continues scaling above 18 (race/class/items can push
    /// WIS past the table's authored range) at the same +2%-per-point rate the 16-18 bands use,
    /// mirroring ResistanceStatModifiers' own above-18 extrapolation.
    /// </summary>
    public static double GetWisdomHearingModifier(int wisdom) => wisdom switch
    {
        <= 3 => -0.05,
        <= 6 => -0.03,
        <= 9 => -0.01,
        <= 12 => 0.0,
        <= 15 => 0.02,
        <= 17 => 0.04,
        18 => 0.06,
        _ => 0.06 + ((wisdom - 18) * 0.02)
    };
}
