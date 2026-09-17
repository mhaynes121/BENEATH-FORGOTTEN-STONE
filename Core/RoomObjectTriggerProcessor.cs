using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities;

namespace BENEATH_FORGOTTEN_STONE.Core;

/// <summary>
/// Evaluates and fires a RoomObject's Trigger after it's successfully pushed -- entirely
/// data-driven (Condition + Effect, see RoomObjectTrigger), so a statue puzzle needs zero
/// statue-specific code here or in GameLoop.HandlePush; any current or future movable object
/// with a configured Trigger works identically. Called exactly once per successful push, right
/// after the object's position actually changes.
/// </summary>
public static class RoomObjectTriggerProcessor
{
    /// <returns>A status message describing what happened, if the trigger fired -- null if nothing did (no trigger configured, condition not met, or an already-activated one-shot).</returns>
    public static string ProcessAfterMove(RoomObject roomObject, (int X, int Y) previousPosition, Level level, Random rng)
    {
        var trigger = roomObject.Trigger;
        if (trigger == null || (trigger.HasActivated && !trigger.Repeatable))
        {
            return null;
        }

        var currentPosition = (roomObject.X, roomObject.Y);
        bool conditionMet = trigger.Condition switch
        {
            RoomObjectTriggerCondition.Moved => true,
            RoomObjectTriggerCondition.LeftPosition => trigger.TrackedPosition == previousPosition && currentPosition != previousPosition,
            RoomObjectTriggerCondition.ReachedPosition => trigger.TrackedPosition == currentPosition,
            _ => false
        };

        if (!conditionMet)
        {
            return null;
        }

        trigger.HasActivated = true;
        return ApplyEffect(trigger, level, rng);
    }

    private static string ApplyEffect(RoomObjectTrigger trigger, Level level, Random rng)
    {
        switch (trigger.Effect)
        {
            case RoomObjectTriggerEffect.Message:
                return trigger.Message ?? "You sense something has changed nearby.";

            case RoomObjectTriggerEffect.RevealHiddenDoor:
                if (trigger.HiddenDoorPosition is { } doorPos && level.IsInBounds(doorPos.X, doorPos.Y)
                    && level.GetDoorAt(doorPos.X, doorPos.Y) == null)
                {
                    // Carves what was solid wall into a floor tile with a fresh, unlocked door on
                    // it -- this game's only "hidden door" concept: not a special Door flag, just
                    // a wall that doesn't become a door until something reveals it. Tile.IsWalkable/
                    // IsTransparent are read fresh every call, so nothing needs telling about the
                    // change beyond the mutation itself.
                    level.Tiles[doorPos.X, doorPos.Y] = Tile.CreateFloor();
                    level.Doors.Add(new Door(doorPos.X, doorPos.Y, isLocked: false, isPickable: true, isBashable: true, difficulty: 0, maxHealth: 20));
                    // A hidden door can newly connect a previously sealed-off pocket to the rest
                    // of the graph -- see Level.RecomputeReachability/Tile.IsReachable.
                    level.RecomputeReachability();
                }
                return trigger.Message ?? "Stone grinds against stone somewhere nearby.";

            case RoomObjectTriggerEffect.SpawnCreatures:
                foreach (var (x, y) in trigger.SpawnPositions)
                {
                    if (!level.IsInBounds(x, y) || level.GetActorAt(x, y) != null)
                    {
                        continue; // never overwrite something already standing there
                    }
                    var monster = Monster.CreateRandom(x, y, trigger.SpawnDifficultyLevel, rng);
                    // Safe Monster Spawning and Hazard-Aware Movement: skip this fixed position
                    // rather than spawn a monster somewhere it would immediately start dying --
                    // a missing monster is preferable.
                    if (!ActorTerrainSafety.IsSafeForActor(level, monster, x, y))
                    {
                        continue;
                    }
                    level.Actors.Add(monster);
                    level.Scheduler.Register(monster);
                }
                return trigger.Message ?? "Something stirs in the shadows nearby.";

            case RoomObjectTriggerEffect.ActivateFeature:
                // Deliberately unimplemented -- "activate another dungeon feature" (the design
                // spec's own wording) is too open-ended to build a real behavior for without a
                // concrete target feature in mind; the spec lists it as one of several EXAMPLE
                // effects, not a required one. The enum value and this case exist purely as a
                // clean extension point -- a future feature (a lever raising a portcullis, a
                // switch lighting braziers, ...) plugs in here without touching RoomObject/
                // RoomObjectTrigger/HandlePush/this processor's dispatch at all.
                return trigger.Message;

            default:
                return null;
        }
    }
}
