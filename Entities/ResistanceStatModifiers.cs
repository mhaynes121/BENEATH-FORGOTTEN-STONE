namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Constitution's contribution to Fire/Water/Ice/Shock/Poison resistance, and Wisdom's
/// contribution to Magic resistance -- one shared banded table (the design spec's own
/// recommended values) rather than duplicating it per resistance type. Both CON and WIS use the
/// identical band shape, just applied to a different stat and a different (disjoint) set of
/// resistance types, so a single lookup covers both -- see GetConstitutionModifier/
/// GetWisdomMagicModifier below, which just call this with the relevant stat value.
/// </summary>
public static class ResistanceStatModifiers
{
    /// <summary>CON 1-3 -6, 4-6 -4, 7-9 -2, 10-12 0, 13-15 +2, 16-17 +4, 18 +6 -- continues scaling by +2 per 2 points above 18 if race/class ever push CON that high, rather than capping at the table's last defined band.</summary>
    public static int GetConstitutionModifier(int constitution) => GetBandedModifier(constitution);

    /// <summary>Same band shape as Constitution's, applied to Wisdom for Magic resistance instead.</summary>
    public static int GetWisdomMagicModifier(int wisdom) => GetBandedModifier(wisdom);

    private static int GetBandedModifier(int statValue) => statValue switch
    {
        <= 3 => -6,
        <= 6 => -4,
        <= 9 => -2,
        <= 12 => 0,
        <= 15 => 2,
        <= 17 => 4,
        18 => 6,
        // Above the table's authored range (only reachable via race/class bonuses stacking on
        // an already-high roll) -- keep scaling at the same +2-per-point rate the 16-18 bands
        // already use, instead of silently flattening out at +6 forever.
        _ => 6 + ((statValue - 18) * 2)
    };
}
