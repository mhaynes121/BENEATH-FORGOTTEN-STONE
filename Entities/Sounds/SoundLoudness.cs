namespace BENEATH_FORGOTTEN_STONE.Entities.Sounds;

/// <summary>How far a sound can possibly be heard from -- see SoundConfig.RangeFor. Never stored as a redundant per-definition range; always derived from this single value so every sound of the same loudness shares one tunable range.</summary>
public enum SoundLoudness
{
    Quiet,
    Normal,
    Loud,
    VeryLoud
}
