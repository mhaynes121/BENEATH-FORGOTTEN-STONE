namespace BENEATH_FORGOTTEN_STONE.Dungeon;

/// <summary>
/// Per-level ambient-sound bookkeeping -- lives on Level itself (a fresh instance per newly
/// constructed Level, same as TurnNumber defaulting to 0), so a level visited for the first
/// time always starts silent (design spec section 30) while a level the player leaves and
/// later returns to in the same session keeps its own running state, exactly like TurnNumber
/// already does. Deliberately not persisted across save/load -- a reloaded save's levels all
/// start quiet again, a minor and harmless simplification since nothing about this state
/// affects correctness, only atmosphere pacing.
/// </summary>
public class DungeonSoundState
{
    /// <summary>Turns since the last successful ambient sound (or since entering the level, if none yet) -- drives the rising silence bonus once past SoundConfig.SoundGracePeriodTurns. See Core/SoundSystem.cs.</summary>
    public int TurnsSinceLastSound { get; set; }

    /// <summary>While positive, no ambient sound check happens at all -- the hard anti-spam floor (design spec section 22). Decremented once per eligible turn.</summary>
    public int SoundCooldownRemaining { get; set; }

    /// <summary>How many sound messages have successfully displayed on this level so far -- not shown to the player; useful for balance tuning/testing (design spec section 31).</summary>
    public int SoundsHeardThisLevel { get; set; }

    /// <summary>The most recent SoundConfig.RecentSoundMemoryCount sound identities displayed (oldest first) -- see Core/SoundSystem.cs's candidate filtering, which avoids repeating one of these when another valid candidate exists (design spec section 41).</summary>
    public List<string> RecentSoundKeys { get; } = new();

    /// <summary>The exact message text last shown for each sound key -- avoids repeating the identical sentence back-to-back for the same SoundType/FloorType when that definition has other message variants available (design spec section 42).</summary>
    public Dictionary<string, string> LastMessageByKey { get; } = new();
}
