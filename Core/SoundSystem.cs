using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities;
using BENEATH_FORGOTTEN_STONE.Entities.Sounds;

namespace BENEATH_FORGOTTEN_STONE.Core;

/// <summary>
/// Ambient sound / hearing system (design spec: "Ambient Sound / Hearing System Complete
/// Implementation Specification"). Called once per completed normal player turn from GameLoop's
/// centralized turn block -- the exact same spot EnvironmentalFloorEffects/regen/EffectProcessor
/// already run from, which is what naturally satisfies the spec's "projectile-only updates don't
/// count, Wait does" rule for free: every action in this game (move, wait, attack, cast,
/// fire/throw) already consumes exactly one normal turn through that one shared block, and a
/// projectile's own multi-tile travel animation happens entirely WITHIN that single turn, never
/// as extra ticks of its own. No separate "is this a real turn" check is needed here.
///
/// Text messages only -- see AddStatusMessage at the call site. Never blocks input, never
/// consumes an extra turn, never reveals exact position/distance (design spec section 45).
/// </summary>
public static class SoundSystem
{
    internal class SoundCandidate
    {
        public SoundCategory Category;
        public string Key;
        public SoundDefinition Definition;
        public int? Distance;
        public double Chance;
    }

    /// <returns>A single atmospheric message if a sound occurred this turn, otherwise null.</returns>
    public static string ProcessTurn(Level level, Player player, Random rng)
    {
        var state = level.SoundState;
        state.TurnsSinceLastSound++;

        if (state.SoundCooldownRemaining > 0)
        {
            // Hard anti-spam floor -- no check at all while a cooldown from the last sound is
            // still running (design spec section 22). TurnsSinceLastSound still climbs during
            // this window (it's tracking "how long since a sound," not "how long since eligible"),
            // so the silence bonus is already partly built up by the time checks resume.
            state.SoundCooldownRemaining--;
            return null;
        }

        var candidates = BuildCandidates(level, player, state);
        if (candidates.Count == 0)
        {
            return null;
        }

        // Each eligible candidate rolls independently against its own chance -- distance/
        // category naturally make some candidates likelier than others without needing one
        // single global "does anything happen" gate first (design spec sections 34-36).
        var successes = candidates.Where(c => rng.NextDouble() < c.Chance).ToList();
        if (successes.Count == 0)
        {
            return null; // no hard guarantee at any silence length -- see CalculateSoundChance's clamp
        }

        var chosen = SelectWeighted(successes, rng);
        string message = PickMessage(chosen, state, rng);

        state.TurnsSinceLastSound = 0;
        state.SoundCooldownRemaining = rng.Next(SoundConfig.MinimumSoundCooldownTurns, SoundConfig.MaximumSoundCooldownTurns + 1);
        state.SoundsHeardThisLevel++;
        state.RecentSoundKeys.Add(chosen.Key);
        while (state.RecentSoundKeys.Count > SoundConfig.RecentSoundMemoryCount)
        {
            state.RecentSoundKeys.RemoveAt(0);
        }
        state.LastMessageByKey[chosen.Key] = message;

        return message;
    }

    /// <summary>
    /// Builds every currently valid sound candidate -- environment sources and a living boss
    /// within their own Loudness-derived range, plus one candidate per distinct SoundType among
    /// every OTHER living monster on the level (no distance requirement at all -- design spec
    /// section 36). Recently used keys are excluded unless doing so would empty the pool
    /// entirely (section 41's "reduced priority... if other valid alternatives exist").
    /// </summary>
    internal static List<SoundCandidate> BuildCandidates(Level level, Player player, DungeonSoundState state)
    {
        var candidates = new List<SoundCandidate>();
        var playerPos = (player.X, player.Y);

        foreach (var source in level.AmbientSoundSources)
        {
            var definition = EnvironmentSoundCatalog.Get(source.FloorType);
            if (definition == null)
            {
                continue;
            }
            int maxRange = SoundConfig.RangeFor(definition.Loudness);
            if (!SoundDistance.TryGetDistance(level, playerPos, (source.X, source.Y), maxRange, out int distance))
            {
                continue;
            }
            candidates.Add(new SoundCandidate
            {
                Category = SoundCategory.Environment,
                Key = $"env:{source.FloorType}",
                Definition = definition,
                Distance = distance,
                Chance = CalculateSoundChance(definition, player, distance, state)
            });
        }

        // At most one boss per level (see Monster.CreateBoss/DungeonGenerator.TrySpawnBossRoom) --
        // its sound stops the instant it dies, since a dead boss simply fails this IsAlive check
        // and stops being a candidate at all (design spec section 39). Uses the boss's own
        // current position, not a cached spawn point, so it stays accurate if it wanders.
        var boss = level.Actors.OfType<Monster>().FirstOrDefault(m => m.IsBoss && m.IsAlive);
        if (boss != null && boss.SoundType != MonsterSoundType.None)
        {
            var definition = MonsterSoundCatalog.Get(boss.SoundType);
            if (definition != null)
            {
                int maxRange = SoundConfig.RangeFor(definition.Loudness);
                if (SoundDistance.TryGetDistance(level, playerPos, (boss.X, boss.Y), maxRange, out int distance))
                {
                    candidates.Add(new SoundCandidate
                    {
                        Category = SoundCategory.Boss,
                        Key = $"boss:{boss.SoundType}",
                        Definition = definition,
                        Distance = distance,
                        Chance = CalculateSoundChance(definition, player, distance, state)
                    });
                }
            }
        }

        foreach (var soundType in GetCreatureAmbientSoundTypes(level))
        {
            var definition = MonsterSoundCatalog.Get(soundType);
            if (definition == null)
            {
                continue;
            }
            candidates.Add(new SoundCandidate
            {
                Category = SoundCategory.CreatureAmbient,
                Key = $"creature:{soundType}",
                Definition = definition,
                Distance = null,
                Chance = CalculateSoundChance(definition, player, null, state)
            });
        }

        var notRecentlyUsed = candidates.Where(c => !state.RecentSoundKeys.Contains(c.Key)).ToList();
        return notRecentlyUsed.Count > 0 ? notRecentlyUsed : candidates;
    }

    /// <summary>
    /// Every distinct non-None SoundType among currently LIVING, non-boss monsters on the level
    /// -- computed live rather than cached, so a SoundType automatically stops appearing the
    /// moment the last monster capable of it dies (design spec section 38's preferred behavior)
    /// with no separate invalidation bookkeeping needed. Cheap even for a busy floor (a handful
    /// of monsters at most), and only runs at all once the cooldown has already elapsed.
    /// </summary>
    internal static IReadOnlyList<MonsterSoundType> GetCreatureAmbientSoundTypes(Level level) =>
        level.Actors.OfType<Monster>()
            .Where(m => m.IsAlive && !m.IsBoss && m.SoundType != MonsterSoundType.None)
            .Select(m => m.SoundType)
            .Distinct()
            .ToList();

    /// <summary>Chance = BaseChance + WisdomModifier + SilenceBonus - DistancePenalty, clamped to [0, MaximumAmbientSoundChance] -- the one centralized formula every candidate uses (design spec section 19), so hearing calculations never get duplicated per call site.</summary>
    internal static double CalculateSoundChance(SoundDefinition definition, Player player, int? distance, DungeonSoundState state)
    {
        double wisdomModifier = SoundConfig.GetWisdomHearingModifier(player.Stats.Adjusted(PrimaryAttribute.Wisdom));
        double silenceBonus = state.TurnsSinceLastSound <= SoundConfig.SoundGracePeriodTurns
            ? 0.0
            : (state.TurnsSinceLastSound - SoundConfig.SoundGracePeriodTurns) * SoundConfig.SoundChanceIncreasePerSilentTurn;
        double distancePenalty = DistancePenalty(distance);

        return Math.Clamp(definition.BaseChance + wisdomModifier + silenceBonus - distancePenalty, 0.0, SoundConfig.MaximumAmbientSoundChance);
    }

    /// <summary>Null (no source position -- a creature-ambient candidate) means no penalty at all. Otherwise banded per design spec section 14: close is barely penalized, far is penalized more.</summary>
    private static double DistancePenalty(int? distance) => distance switch
    {
        null => 0.0,
        <= SoundConfig.NearDistanceBand => SoundConfig.NearDistancePenalty,
        <= SoundConfig.MidDistanceBand => SoundConfig.MidDistancePenalty,
        <= SoundConfig.FarDistanceBand => SoundConfig.FarDistancePenalty,
        _ => SoundConfig.VeryFarDistancePenalty
    };

    /// <summary>Weighted pick among candidates that already succeeded their own chance roll -- a nearby boss should generally win out over a generic ambient sound when both happen to succeed the same turn (design spec section 35).</summary>
    private static SoundCandidate SelectWeighted(List<SoundCandidate> successes, Random rng)
    {
        double totalWeight = successes.Sum(c => WeightFor(c.Category));
        double roll = rng.NextDouble() * totalWeight;
        double cumulative = 0;
        foreach (var candidate in successes)
        {
            cumulative += WeightFor(candidate.Category);
            if (roll < cumulative)
            {
                return candidate;
            }
        }
        return successes[^1];
    }

    private static double WeightFor(SoundCategory category) => category switch
    {
        SoundCategory.Boss => SoundConfig.BossSelectionWeight,
        SoundCategory.Environment => SoundConfig.EnvironmentSelectionWeight,
        _ => SoundConfig.CreatureAmbientSelectionWeight
    };

    /// <summary>Avoids repeating the exact same sentence back-to-back for the same sound key when another variant exists (design spec section 42) -- falls back to the full message list if every variant was somehow already the last one shown (only possible for a single-message definition).</summary>
    private static string PickMessage(SoundCandidate candidate, DungeonSoundState state, Random rng)
    {
        var messages = candidate.Definition.Messages;
        if (messages.Count == 1)
        {
            return messages[0];
        }

        state.LastMessageByKey.TryGetValue(candidate.Key, out string lastMessage);
        var eligible = messages.Where(m => m != lastMessage).ToList();
        var pool = eligible.Count > 0 ? eligible : messages;
        return pool[rng.Next(pool.Count)];
    }
}
