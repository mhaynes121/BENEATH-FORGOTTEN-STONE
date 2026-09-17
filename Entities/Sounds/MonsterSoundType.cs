namespace BENEATH_FORGOTTEN_STONE.Entities.Sounds;

/// <summary>
/// A monster archetype's ambient sound identity -- used both for the level-wide creature
/// ambient pool (Core/SoundSystem.cs, built live from every currently-living non-boss
/// monster's SoundType) and, when the monster IS the boss, for the boss's own distance-gated
/// sound (design spec sections 5-8: a boss's sound is just its own archetype's SoundType,
/// evaluated differently). None (the default for every archetype that doesn't declare one) means
/// naturally silent -- never contributes to either pool. Extensible: add a member here plus a
/// MonsterSoundCatalog entry to give a future archetype a new flavor of ambient sound.
/// </summary>
public enum MonsterSoundType
{
    None,
    Wings,
    Howl,
    Growl,
    Hiss,
    Skitter,
    Footsteps,
    HeavyFootsteps,
    ManyFootsteps,
    Chanting,
    Rattle,
    Moan,
    Roar,
    Croak,
    Buzz,
    Scraping,
    Voices,
    Fighting,
    StoneGrinding
}
