using BENEATH_FORGOTTEN_STONE.Dungeon;

namespace BENEATH_FORGOTTEN_STONE.Entities.Sounds;

/// <summary>
/// One SoundDefinition per FloorType that can ever produce an ambient environmental sound --
/// deliberately only Water/Fire/Lava/Swamp/Ice (design spec section 2: NORMAL never produces
/// one, and Grass/Sand/Mud/Ash stay silent "unless suitable sound definitions are later added").
/// Keyed by FloorType directly rather than a name/display-character check, per the spec's own
/// "do not hard-code this based on display characters" rule.
/// </summary>
public static class EnvironmentSoundCatalog
{
    private static readonly IReadOnlyDictionary<FloorType, SoundDefinition> Definitions = new Dictionary<FloorType, SoundDefinition>
    {
        [FloorType.Water] = new(SoundLoudness.Loud, 0.12,
            "You hear the sound of flowing water.",
            "A faint rushing of water echoes through the dungeon.",
            "You hear water dripping somewhere nearby.",
            "The sound of moving water reaches you from the darkness."),

        [FloorType.Fire] = new(SoundLoudness.Loud, 0.12,
            "You hear the crackle of flames.",
            "A faint roar of fire echoes nearby.",
            "You hear something burning in the distance.",
            "The soft crackling of fire reaches your ears."),

        [FloorType.Lava] = new(SoundLoudness.VeryLoud, 0.14,
            "You hear a deep bubbling sound.",
            "A low hiss and crackle echoes through the dungeon.",
            "You hear the slow churn of something molten.",
            "A deep bubbling sound rises from somewhere nearby."),

        [FloorType.Swamp] = new(SoundLoudness.Normal, 0.08,
            "You hear wet splashing somewhere nearby.",
            "A chorus of distant croaks echoes through the darkness.",
            "You hear something moving through shallow water."),

        [FloorType.Ice] = new(SoundLoudness.Quiet, 0.06,
            "You hear a faint cracking sound.",
            "Something creaks like shifting ice nearby.",
            "A sharp crack echoes through the dungeon.")
    };

    /// <summary>Null for Normal/Grass/Sand/Mud/Ash -- no environmental sound source can ever be generated for those (see DungeonGenerator's ambient-source rolling step).</summary>
    public static SoundDefinition Get(FloorType floorType) => Definitions.GetValueOrDefault(floorType);
}
