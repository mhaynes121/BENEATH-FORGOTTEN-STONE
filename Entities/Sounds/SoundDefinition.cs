namespace BENEATH_FORGOTTEN_STONE.Entities.Sounds;

/// <summary>
/// A reusable sound archetype -- shared shape for both MonsterSoundCatalog (keyed by
/// MonsterSoundType) and EnvironmentSoundCatalog (keyed by FloorType), so "a set of possible
/// messages with a loudness and base chance" is defined exactly once regardless of what
/// triggers it (design spec section 9). MaxRange is deliberately not a field here -- it's
/// always derived from Loudness via SoundConfig.RangeFor, so every sound of the same loudness
/// shares one tunable range instead of needing to be kept in sync per definition.
/// </summary>
public class SoundDefinition
{
    public SoundLoudness Loudness { get; }

    /// <summary>Chance (0-1) this sound is chosen once it's already an eligible candidate this turn -- combined with Wisdom/silence/distance modifiers in SoundSystem.CalculateSoundChance, then clamped to SoundConfig.MaximumAmbientSoundChance.</summary>
    public double BaseChance { get; }

    /// <summary>At least two variants per definition (design spec section 10) so back-to-back occurrences of the same SoundType don't read as identical.</summary>
    public IReadOnlyList<string> Messages { get; }

    public SoundDefinition(SoundLoudness loudness, double baseChance, params string[] messages)
    {
        Loudness = loudness;
        BaseChance = baseChance;
        Messages = messages;
    }
}
