using BENEATH_FORGOTTEN_STONE.Entities;

namespace BENEATH_FORGOTTEN_STONE.Core;

/// <summary>
/// Energy/speed-based turn scheduler (not simple round-robin), so
/// faster actors act more often than slower ones. Each actor banks
/// `Speed` energy per tick; once an actor's energy crosses the
/// threshold it's eligible to act. Ties go to whichever actor has
/// accumulated the most energy.
/// </summary>
public class TurnScheduler
{
    private const int ActionThreshold = 100;
    private readonly List<Actor> actors = new();

    /// <summary>
    /// The actor GetNextActor most recently handed back without GameLoop having called
    /// ConsumeEnergy on it yet (e.g. the player opened a menu and cancelled, so the same
    /// physical turn gets re-polled). See the comment in GetNextActor -- this is the ONLY
    /// actor exempt from gaining energy on a given poll; every other actor keeps
    /// accumulating even while already at/above threshold.
    /// </summary>
    private Actor lastOffered;

    public void Register(Actor actor) => actors.Add(actor);
    public void Unregister(Actor actor) => actors.Remove(actor);

    public Actor GetNextActor()
    {
        while (true)
        {
            actors.RemoveAll(a => !a.IsAlive);
            if (actors.Count == 0)
            {
                lastOffered = null;
                return null;
            }

            Actor ready = null;

            foreach (var actor in actors)
            {
                // Everyone except lastOffered keeps accumulating energy on every poll, even
                // past the threshold -- otherwise an actor sitting exactly at the threshold
                // (never re-selected because something else keeps momentarily overshooting
                // past it) would freeze there forever and could starve for good on a busy
                // floor with many actors (e.g. a boss room's entourage). lastOffered alone is
                // exempt: it's the one actor whose turn is still pending (ConsumeEnergy
                // hasn't run for it), and every poll of a "free" action -- a blocked move, a
                // cancelled menu -- would otherwise keep banking it more energy each retry,
                // breaking its own speed pacing.
                //
                // Math.Max(1, ...) guarantees forward progress even if Speed is 0 or
                // negative -- e.g. Crippling Strike's -4 MovementSpeed debuff landing on a
                // low-Speed monster archetype. Without this floor, every actor sitting at
                // Speed <= 0 simultaneously means Energy never crosses ActionThreshold and
                // this while(true) spins forever. No effect on the normal case (Speed >= 1
                // already gives the same result), so pacing for every ordinary actor is
                // unchanged; a crippled actor just acts at the slowest possible rate instead
                // of never again.
                if (actor != lastOffered)
                {
                    actor.Energy += Math.Max(1, actor.Speed);
                }

                if (actor.Energy >= ActionThreshold && (ready == null || actor.Energy > ready.Energy))
                {
                    ready = actor;
                }
            }

            if (ready != null)
            {
                lastOffered = ready;
                return ready;
            }
        }
    }

    public void ConsumeEnergy(Actor actor, int amount = ActionThreshold)
    {
        actor.Energy -= amount;
        // The pending turn genuinely completed -- future polls should accumulate energy for
        // this actor normally again, same as everyone else.
        lastOffered = null;
    }

    /// <summary>
    /// Prone/Knockdown System: removes one ready action from a knockdown victim who already has
    /// enough banked energy to act -- represents the fall interrupting whatever they were about
    /// to do. Does nothing if the victim doesn't currently have a ready action banked (no
    /// over-penalizing a target who hadn't accumulated enough energy yet). Deliberately separate
    /// from ConsumeEnergy: this fires as a SIDE EFFECT of some other actor's action landing on the
    /// victim, not because the victim's own turn was just resolved by GetNextActor/Run's main
    /// loop, so it must never touch lastOffered -- doing so would incorrectly clear the "same
    /// physical turn re-polled" tracking for whichever actor (almost always someone else) is
    /// actually pending right now. See KnockdownResolver.TryApply.
    /// </summary>
    public void ConsumeReadyActionIfBanked(Actor actor)
    {
        if (actor.Energy >= ActionThreshold)
        {
            actor.Energy -= ActionThreshold;
        }
    }
}
