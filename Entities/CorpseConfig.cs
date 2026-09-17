namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>Tunable numbers for the Corpse System, kept in one place per this codebase's established config-class convention.</summary>
public static class CorpseConfig
{
    /// <summary>Generous, matching Chest's own DefaultSlotCapacity -- a corpse should never overflow from ordinary death loot plus a monster's leftover carried items.</summary>
    public const int SlotCapacity = 20;

    // The game's Size enum has 3 tiers (Small/Medium/Large), not the proposal's illustrative
    // 5-tier example (Very Small/Small/Medium/Large/Huge) -- mapped down per the proposal's own
    // "if the current size model does not contain all of these categories, map the available
    // sizes onto an equivalent table" guidance, using its Small/Medium/Large numbers directly.
    public const double SmallCorpseWeight = 10;
    public const double MediumCorpseWeight = 40;
    public const double LargeCorpseWeight = 100;

    public static double WeightFor(Size size) => size switch
    {
        Size.Small => SmallCorpseWeight,
        Size.Medium => MediumCorpseWeight,
        Size.Large => LargeCorpseWeight,
        _ => MediumCorpseWeight
    };
}
