namespace BENEATH_FORGOTTEN_STONE.Core;

/// <summary>The one outcome a dropped/thrown item's landing resolves to -- see ItemLandingResolver, which guarantees exactly one of these per landing.</summary>
public enum ItemLandingOutcome
{
    LandsNormally,
    Misplaced,

    /// <summary>Unused until Stage 2 -- ItemLandingResolver never produces this yet, but the value already exists so Stage 2 doesn't need to touch every switch that already handles this enum.</summary>
    Displaced,
    PermanentlyLost,
    Destroyed,

    /// <summary>Impact breakage (see ItemBreakageRules) -- deliberately distinct from Destroyed (environmental fire/lava) and PermanentlyLost (the random loss roll), even though all three remove the item the same way. Environmental destruction is checked first and takes priority; breakage is checked next, before the loss/misplaced roll.</summary>
    Broken,

    /// <summary>Informational only -- GameLoop's own ThrownWeaponBehavior.ReturnsToThrower branch never calls ItemLandingResolver at all, so this value is never actually produced by it today.</summary>
    ReturnedToThrower
}
