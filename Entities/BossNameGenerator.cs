namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>How BossName/BossEpithet combine into a boss's DisplayName -- see Monster.DisplayName. Direct: "Lormax Golden Wing". The: "Norro the Eternal".</summary>
public enum EpithetFormat
{
    Direct,
    The
}

/// <summary>One generated boss identity -- see BossNameGenerator.GenerateBossIdentity.</summary>
public readonly record struct BossIdentity(string Name, string Epithet, EpithetFormat Format);

/// <summary>
/// Generates a boss's player-facing identity -- a proper name plus an epithet -- completely
/// independent of the underlying creature's Name/CreatureType (see "Boss Monster Naming --
/// Epithet System.txt"). A boss should read as an individual legendary creature ("Lormax
/// Golden Wing") rather than a stronger version of its species ("Giant Cockroach
/// Chieftain"); the underlying creature type stays discoverable through the monster's
/// ordinary description instead.
///
/// Curated lists rather than procedural word generation for now, per the design doc's
/// "prefer curated lists initially" guidance -- easy to expand later. Epithets are grouped
/// into categories purely for readability/maintenance; the generator itself picks a random
/// category first (so one large category can't dominate the odds) and then a random entry
/// within it. Names and epithets are deliberately NOT tied to any particular creature type,
/// so the same epithet could land on a boss of any archetype -- exactly as the doc specifies.
/// </summary>
public static class BossNameGenerator
{
    private static readonly string[] Names =
    {
        "Lormax", "Norro", "Varkesh", "Tharok", "Zelmar", "Grimnak", "Keldor", "Vorren",
        "Malrax", "Durnak", "Ravok", "Zareth", "Korvax", "Velrik", "Thalmar", "Brakor",
        "Xanthes", "Morvant", "Skarn", "Ulthek", "Drevanor", "Neshka", "Volgrim", "Aszra", "Krethor"
    };

    private readonly record struct EpithetOption(string Text, EpithetFormat Format);

    // Noun-phrase epithets describing a physical trait -- stand alone after the name ("Lormax Ironhide").
    private static readonly EpithetOption[] PhysicalEpithets =
    {
        new("Ironhide", EpithetFormat.Direct), new("Golden Wing", EpithetFormat.Direct),
        new("Longclaw", EpithetFormat.Direct), new("Redhand", EpithetFormat.Direct),
        new("Scarback", EpithetFormat.Direct), new("Blackfang", EpithetFormat.Direct),
        new("Stonehide", EpithetFormat.Direct), new("Silverclaw", EpithetFormat.Direct),
        new("Ironjaw", EpithetFormat.Direct), new("Grimscale", EpithetFormat.Direct),
        new("Bloodfang", EpithetFormat.Direct), new("Steelhorn", EpithetFormat.Direct)
    };

    // Adjective epithets describing combat prowess -- used after "the" ("Norro the Relentless").
    private static readonly EpithetOption[] CombatEpithets =
    {
        new("Relentless", EpithetFormat.The), new("Unbroken", EpithetFormat.The),
        new("Unyielding", EpithetFormat.The), new("Conqueror", EpithetFormat.The),
        new("Defiant", EpithetFormat.The), new("Unbowed", EpithetFormat.The),
        new("Ruthless", EpithetFormat.The), new("Undaunted", EpithetFormat.The),
        new("Swift", EpithetFormat.The), new("Fearless", EpithetFormat.The), new("Dauntless", EpithetFormat.The)
    };

    // Adjective epithets describing reputation -- used after "the".
    private static readonly EpithetOption[] ReputationEpithets =
    {
        new("Dreaded", EpithetFormat.The), new("Forgotten", EpithetFormat.The),
        new("Eternal", EpithetFormat.The), new("Exiled", EpithetFormat.The),
        new("Merciless", EpithetFormat.The), new("Cruel", EpithetFormat.The),
        new("Silent", EpithetFormat.The), new("Ravenous", EpithetFormat.The),
        new("Notorious", EpithetFormat.The), new("Infamous", EpithetFormat.The), new("Vengeful", EpithetFormat.The)
    };

    // Adjective epithets describing age -- used after "the".
    private static readonly EpithetOption[] AgeEpithets =
    {
        new("Ancient", EpithetFormat.The), new("Timeless", EpithetFormat.The),
        new("Ageless", EpithetFormat.The), new("Undying", EpithetFormat.The),
        new("Elder", EpithetFormat.The), new("Primeval", EpithetFormat.The)
    };

    // Noun-phrase epithets with an elemental theme -- stand alone after the name.
    private static readonly EpithetOption[] ElementalEpithets =
    {
        new("Ashheart", EpithetFormat.Direct), new("Frostborn", EpithetFormat.Direct),
        new("Stormcaller", EpithetFormat.Direct), new("Emberblood", EpithetFormat.Direct),
        new("Stormborn", EpithetFormat.Direct), new("Frostfang", EpithetFormat.Direct),
        new("Cinderclaw", EpithetFormat.Direct), new("Galewing", EpithetFormat.Direct)
    };

    // Mystical epithets -- a mix of noun phrases and "the"-adjectives, since occult/arcane
    // flavor reads naturally either way ("Skarn Voidwalker" / "Skarn the Arcane").
    private static readonly EpithetOption[] MysticalEpithets =
    {
        new("Shadowmind", EpithetFormat.Direct), new("Voidwalker", EpithetFormat.Direct),
        new("Doomweaver", EpithetFormat.Direct), new("Soulbinder", EpithetFormat.Direct),
        new("Nightshade", EpithetFormat.Direct), new("Cursed", EpithetFormat.The),
        new("Arcane", EpithetFormat.The), new("Fated", EpithetFormat.The),
        new("Unseen", EpithetFormat.The), new("Forsaken", EpithetFormat.The)
    };

    private static readonly EpithetOption[][] EpithetCategories =
    {
        PhysicalEpithets, CombatEpithets, ReputationEpithets, AgeEpithets, ElementalEpithets, MysticalEpithets
    };

    /// <summary>
    /// Best-effort session-wide uniqueness -- since there's only ever one boss per floor,
    /// this is already more than the doc requires ("preventing duplicate names on the same
    /// floor is already sufficient"), but a plain in-memory set costs nothing and avoids the
    /// same boss identity showing up twice in one playthrough. Resets naturally every run
    /// (a fresh process), so it never needs saving/loading.
    /// </summary>
    private static readonly HashSet<string> usedIdentities = new();

    /// <summary>Generates one boss identity, retrying a bounded number of times to avoid repeating an identity already used this session -- see usedIdentities. Call once per boss, at creation (Monster.CreateBoss), and persist the result; never regenerate it later.</summary>
    public static BossIdentity GenerateBossIdentity(Random rng)
    {
        const int maxAttempts = 20;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var candidate = RollIdentity(rng);
            if (usedIdentities.Add($"{candidate.Name}|{candidate.Epithet}"))
            {
                return candidate;
            }
        }

        // Every retry collided with an already-used identity -- vanishingly unlikely given
        // the list sizes, and a repeated name is a cosmetic issue at worst, so just return
        // whatever this last roll produced rather than looping forever.
        return RollIdentity(rng);
    }

    private static BossIdentity RollIdentity(Random rng)
    {
        string name = Names[rng.Next(Names.Length)];
        var category = EpithetCategories[rng.Next(EpithetCategories.Length)];
        var epithet = category[rng.Next(category.Length)];
        return new BossIdentity(name, epithet.Text, epithet.Format);
    }
}
