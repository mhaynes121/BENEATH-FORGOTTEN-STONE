namespace BENEATH_FORGOTTEN_STONE.Dungeon;

/// <summary>
/// What has to happen to a movable RoomObject for its Trigger to fire -- see the design spec's
/// own four example conditions. EnteredTile and "reaches a specific final position" collapse
/// into the single ReachedPosition case below (both are just "is the object now AT this
/// position", whether framed as a checkpoint or a puzzle's win condition) -- there's no
/// behavioral difference to justify two separate enum values for the same check.
/// </summary>
public enum RoomObjectTriggerCondition
{
    /// <summary>Fires the instant the object is successfully pushed at all, regardless of where it ends up. TrackedPosition is meaningless here.</summary>
    Moved,

    /// <summary>Fires the moment the object's position stops being TrackedPosition (i.e. it WAS there before this push, and isn't anymore).</summary>
    LeftPosition,

    /// <summary>Fires the moment the object's position BECOMES TrackedPosition.</summary>
    ReachedPosition
}

/// <summary>
/// What happens when a Trigger fires. RevealHiddenDoor and SpawnCreatures are the two concrete
/// effects built out for this feature (see RoomObjectTriggerProcessor); ActivateFeature is a
/// deliberately unimplemented extension point -- see that class's own doc comment on why.
/// </summary>
public enum RoomObjectTriggerEffect
{
    Message,
    RevealHiddenDoor,
    SpawnCreatures,
    ActivateFeature
}

/// <summary>
/// Data-driven trigger attached to a movable RoomObject -- see RoomObjectTriggerProcessor for
/// the (condition, effect) evaluation this describes. Deliberately generic rather than
/// statue-specific: any current or future movable object type gets puzzle behavior for free by
/// attaching one of these, with zero object-type-specific code anywhere in the push/trigger
/// pipeline (design spec: "a statue puzzle should be possible... without adding statue-specific
/// logic to the push command").
/// </summary>
public class RoomObjectTrigger
{
    public RoomObjectTriggerCondition Condition { get; }

    /// <summary>Meaningful only for LeftPosition/ReachedPosition -- the position being watched for. Null for Moved.</summary>
    public (int X, int Y)? TrackedPosition { get; }

    public RoomObjectTriggerEffect Effect { get; }

    /// <summary>False (the default) means this trigger only ever fires once -- see HasActivated.</summary>
    public bool Repeatable { get; }

    /// <summary>Set the first time this trigger fires. A non-Repeatable trigger checks this before ever re-evaluating its Condition again. Persisted -- see Persistence/RoomObjectData.</summary>
    public bool HasActivated { get; set; }

    /// <summary>Effect.Message's text, or a fallback flavor line appended by the other effects when they don't specify their own (e.g. RevealHiddenDoor's default "Stone grinds against stone somewhere nearby."). Never object-name-specific -- see RoomObjectTriggerProcessor's own defaults for that reasoning.</summary>
    public string Message { get; }

    /// <summary>Effect.RevealHiddenDoor only -- the currently-solid wall tile to carve into a floor with an unlocked door on it.</summary>
    public (int X, int Y)? HiddenDoorPosition { get; }

    /// <summary>Effect.SpawnCreatures only -- every position (skipped if already occupied) a freshly rolled monster appears at.</summary>
    public IReadOnlyList<(int X, int Y)> SpawnPositions { get; }

    /// <summary>Effect.SpawnCreatures only -- the difficultyLevel passed to Monster.CreateRandom for each spawn.</summary>
    public int SpawnDifficultyLevel { get; }

    public RoomObjectTrigger(
        RoomObjectTriggerCondition condition, RoomObjectTriggerEffect effect, bool repeatable = false,
        (int X, int Y)? trackedPosition = null, string message = null, (int X, int Y)? hiddenDoorPosition = null,
        IReadOnlyList<(int X, int Y)> spawnPositions = null, int spawnDifficultyLevel = 1)
    {
        Condition = condition;
        Effect = effect;
        Repeatable = repeatable;
        TrackedPosition = trackedPosition;
        Message = message;
        HiddenDoorPosition = hiddenDoorPosition;
        SpawnPositions = spawnPositions ?? Array.Empty<(int X, int Y)>();
        SpawnDifficultyLevel = spawnDifficultyLevel;
    }
}
