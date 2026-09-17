namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>Tunable numbers for the Pet and Companion System, kept in one place per this codebase's established config-class convention.</summary>
public static class PetConfig
{
    /// <summary>Completed OWNER turns (Player.TurnCount), not any per-level Level.TurnNumber -- see Pet.RespawnAtOwnerTurn's own doc comment.</summary>
    public const int RespawnDelayOwnerTurns = 20;

    /// <summary>Revised Pet Movement Proposal: Chebyshev radius of the owner-centered area an idle pet roams within before it's considered "out of range" and begins returning -- a configurable value rather than a literal `3` embedded throughout PetAI, per the proposal's own section 8.</summary>
    public const int RoamingRadius = 3;
}
