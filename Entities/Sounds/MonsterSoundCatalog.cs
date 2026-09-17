namespace BENEATH_FORGOTTEN_STONE.Entities.Sounds;

/// <summary>One SoundDefinition per non-None MonsterSoundType -- used for both the level-wide creature ambient pool and a living boss's own sound (see Core/SoundSystem.cs). Messages are deliberately atmospheric, never naming a specific creature or distance (design spec section 45).</summary>
public static class MonsterSoundCatalog
{
    private static readonly IReadOnlyDictionary<MonsterSoundType, SoundDefinition> Definitions = new Dictionary<MonsterSoundType, SoundDefinition>
    {
        [MonsterSoundType.Wings] = new(SoundLoudness.Normal, 0.10,
            "You hear the fluttering of wings.",
            "Something flaps through the darkness.",
            "The faint beating of wings echoes nearby.",
            "You hear wings somewhere in the distance."),

        [MonsterSoundType.Howl] = new(SoundLoudness.Loud, 0.10,
            "You hear howling in the distance.",
            "A lonely howl echoes through the dungeon.",
            "Something howls far away.",
            "A distant howl breaks the silence."),

        [MonsterSoundType.Growl] = new(SoundLoudness.Normal, 0.09,
            "You hear a low growl nearby.",
            "Something growls in the darkness.",
            "A menacing growl echoes softly."),

        [MonsterSoundType.Hiss] = new(SoundLoudness.Quiet, 0.08,
            "You hear a faint hiss nearby.",
            "Something hisses in the darkness.",
            "A low hiss echoes softly."),

        [MonsterSoundType.Skitter] = new(SoundLoudness.Quiet, 0.08,
            "You hear something skittering across stone.",
            "Faint scuttling echoes nearby.",
            "Something scurries in the darkness."),

        [MonsterSoundType.Footsteps] = new(SoundLoudness.Normal, 0.09,
            "You hear footsteps somewhere nearby.",
            "Soft footsteps echo in the distance.",
            "You hear someone -- or something -- walking nearby."),

        [MonsterSoundType.HeavyFootsteps] = new(SoundLoudness.Loud, 0.11,
            "You hear heavy footsteps somewhere ahead.",
            "The ground trembles faintly with heavy steps.",
            "Something large is moving nearby."),

        [MonsterSoundType.ManyFootsteps] = new(SoundLoudness.Loud, 0.10,
            "You hear the sound of many footsteps.",
            "Several sets of footsteps echo nearby.",
            "You hear a group moving somewhere ahead."),

        [MonsterSoundType.Chanting] = new(SoundLoudness.Normal, 0.10,
            "You hear faint chanting somewhere nearby.",
            "A low chant echoes through the darkness.",
            "Strange murmuring reaches your ears."),

        [MonsterSoundType.Rattle] = new(SoundLoudness.Quiet, 0.08,
            "You hear a faint rattling sound.",
            "Something rattles in the darkness.",
            "A dry rattle echoes nearby."),

        [MonsterSoundType.Moan] = new(SoundLoudness.Normal, 0.09,
            "A low moan echoes somewhere nearby.",
            "You hear something groaning in the dark.",
            "A mournful moan drifts through the corridors."),

        [MonsterSoundType.Roar] = new(SoundLoudness.VeryLoud, 0.12,
            "A deep roar echoes through the darkness.",
            "Something roars in the distance.",
            "A terrible roar shakes the corridor walls."),

        [MonsterSoundType.Croak] = new(SoundLoudness.Quiet, 0.08,
            "A chorus of distant croaks echoes through the darkness.",
            "You hear croaking somewhere nearby.",
            "Something croaks softly in the dark."),

        [MonsterSoundType.Buzz] = new(SoundLoudness.Quiet, 0.07,
            "You hear a faint buzzing sound.",
            "Something buzzes in the darkness.",
            "A low buzz drifts through the air."),

        [MonsterSoundType.Scraping] = new(SoundLoudness.Normal, 0.08,
            "You hear something scraping against stone.",
            "A faint scraping sound echoes nearby.",
            "Something drags across the floor somewhere ahead."),

        [MonsterSoundType.Voices] = new(SoundLoudness.Normal, 0.10,
            "You hear faint voices somewhere in the darkness.",
            "Muffled voices echo nearby.",
            "You hear someone talking somewhere ahead."),

        [MonsterSoundType.Fighting] = new(SoundLoudness.VeryLoud, 0.10,
            "You hear the clash of weapons in the distance.",
            "Something is fighting somewhere nearby.",
            "The sound of combat echoes through the corridors."),

        [MonsterSoundType.StoneGrinding] = new(SoundLoudness.Loud, 0.09,
            "You hear stone grinding against stone.",
            "A low grinding sound echoes nearby.",
            "Something heavy scrapes across stone somewhere ahead.")
    };

    /// <summary>Null for MonsterSoundType.None (or any future value never given a definition) -- callers treat that as "no sound," never as an error.</summary>
    public static SoundDefinition Get(MonsterSoundType type) => Definitions.GetValueOrDefault(type);
}
