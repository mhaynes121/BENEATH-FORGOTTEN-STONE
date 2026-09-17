namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>What happened when Stun or Frightened was attempted -- mirrors KnockdownOutcome's shape exactly, kept as a separate enum since the two crowd-control families (Stun/Frightened vs. Knockdown) have slightly different preconditions (Knockdown additionally rejects an already-prone target; Stun/Frightened have no "already active" rejection -- reapplying just refreshes to the longer duration, same as before this resolver existed).</summary>
public enum CrowdControlOutcome
{
    /// <summary>The condition's turn-deadline was set (or extended, if already active).</summary>
    Applied,

    /// <summary>Resisted entirely by Berserker Rage's crowd-control immunity (Actor.CcImmuneUntilTurn).</summary>
    Immune,

    /// <summary>Resisted by the Priest's Steadfast passive (a 25% chance, rolled only for a Player who knows it).</summary>
    ResistedBySteadfast
}

/// <summary>
/// Centralizes Stun and Frightened application (New Priest Skill Progression proposal's
/// "Centralize application of: Stun, Frightened, Knockdown") -- both follow the same
/// precondition order the proposal specifies for Steadfast: 1) the ability's own application-
/// chance roll (done by the calling effect, before this is ever invoked), 2) complete
/// crowd-control immunity, 3) Steadfast's flat resistance chance, 4) apply. Knockdown's
/// equivalent centralization lives in KnockdownResolver (which additionally rejects an
/// already-prone target and removes a banked action) rather than here, since its shape
/// differs enough that folding it into this class would just mean branching on which kind
/// of condition it is -- both classes share the exact same outcome-enum shape and Steadfast
/// check instead.
/// </summary>
public static class CrowdControlResolver
{
    private const double SteadfastResistChance = 0.25;

    public static CrowdControlOutcome TryApplyStun(Actor target, int scaledDuration, int turnNumber, Random rng)
    {
        if (turnNumber < target.CcImmuneUntilTurn)
        {
            return CrowdControlOutcome.Immune;
        }
        if (ResistedBySteadfast(target, rng))
        {
            return CrowdControlOutcome.ResistedBySteadfast;
        }

        target.StunnedUntilTurn = Math.Max(target.StunnedUntilTurn, turnNumber + scaledDuration);
        return CrowdControlOutcome.Applied;
    }

    public static CrowdControlOutcome TryApplyFrightened(Actor target, int scaledDuration, int turnNumber, Random rng)
    {
        if (turnNumber < target.CcImmuneUntilTurn)
        {
            return CrowdControlOutcome.Immune;
        }
        if (ResistedBySteadfast(target, rng))
        {
            return CrowdControlOutcome.ResistedBySteadfast;
        }

        target.FrightenedUntilTurn = Math.Max(target.FrightenedUntilTurn, turnNumber + scaledDuration);
        return CrowdControlOutcome.Applied;
    }

    /// <summary>Only ever true for a Player who has learned Steadfast -- a Monster (including one under a copy of this same condition) never resists this way. See KnockdownResolver.TryApply for the identical check applied to knockdown.</summary>
    internal static bool ResistedBySteadfast(Actor target, Random rng) =>
        target is Player player && player.HasSkill(SkillCatalog.Steadfast) && rng.NextDouble() < SteadfastResistChance;
}
