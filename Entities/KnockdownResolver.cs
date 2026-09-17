using BENEATH_FORGOTTEN_STONE.Core;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>What happened when a knockdown was attempted -- lets the caller (KnockdownEffect) decide whether to report success/failure text, without duplicating TryApply's own precondition checks.</summary>
public enum KnockdownOutcome
{
    /// <summary>The actor is now prone (it wasn't already, wasn't immune, and was alive/targetable).</summary>
    Applied,

    /// <summary>Already prone -- left untouched; no second ready-action penalty is imposed.</summary>
    AlreadyProne,

    /// <summary>Resisted entirely by Berserker Rage's crowd-control immunity (Actor.CcImmuneUntilTurn).</summary>
    Immune,

    /// <summary>Dead or no longer a valid target -- shouldn't normally be reachable (TargetResolver already filters AffectedActors to alive, CanBeTargeted actors), kept only as a defensive guard.</summary>
    Rejected,

    /// <summary>Resisted by the Priest's Steadfast passive (a 25% chance) -- see CrowdControlResolver.ResistedBySteadfast, the identical check Stun/Frightened use.</summary>
    ResistedBySteadfast
}

/// <summary>
/// Prone/Knockdown System: the single centralized place that ever sets Actor.IsProne, mirroring
/// how StunEffect/SilenceEffect are the only writers of their own Actor fields. Two entry points --
/// TryApply for an ability-driven knockdown (Bash/Trip/Tremor), ApplyEnvironmentalFall for a
/// Water/Ice slip, which deliberately skips both the ready-action penalty (the move onto the tile
/// is already this turn's spent action) and the crowd-control immunity check (an environmental
/// fall isn't the kind of ability-induced control effect Berserker Rage is meant to shrug off).
/// </summary>
public static class KnockdownResolver
{
    /// <summary>Ability-driven knockdown (Bash's shield strike, Trip, Tremor) -- the caller is responsible for any success-chance roll (see KnockdownEffect); once called, the knockdown is considered to have "hit" and only the preconditions below can still stop it.</summary>
    public static KnockdownOutcome TryApply(Actor target, int turnNumber, TurnScheduler scheduler, Random rng)
    {
        if (!target.IsAlive || !target.CanBeTargeted)
        {
            return KnockdownOutcome.Rejected;
        }

        if (target.IsProne)
        {
            return KnockdownOutcome.AlreadyProne;
        }

        if (turnNumber < target.CcImmuneUntilTurn)
        {
            return KnockdownOutcome.Immune;
        }

        if (CrowdControlResolver.ResistedBySteadfast(target, rng))
        {
            return KnockdownOutcome.ResistedBySteadfast;
        }

        target.IsProne = true;
        scheduler.ConsumeReadyActionIfBanked(target);
        return KnockdownOutcome.Applied;
    }

    /// <summary>Water/Ice slip -- see the type-level doc comment for why this bypasses both the ready-action penalty and crowd-control immunity that TryApply enforces. A no-op for a dead actor, for parity, though only the living player ever reaches this today.</summary>
    public static void ApplyEnvironmentalFall(Actor actor)
    {
        if (!actor.IsAlive)
        {
            return;
        }

        actor.IsProne = true;
    }
}
