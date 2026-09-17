namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Centralized trader-system tuning knobs -- mirrors BossConfig's style (plain constants, no
/// behavior). Referenced by DungeonGenerator (spawn chance, inventory size), DungeonManager
/// (frequency guarantee), Trader (inventory generation), and ItemPricingCalculator (charisma
/// scaling), so every number the design calls "should be easy to modify during playtesting"
/// lives in one place.
/// </summary>
public static class TraderConfig
{
    /// <summary>Per-floor chance of spawning a trader when the frequency guarantee isn't already forcing one -- see DungeonManager.LevelsSinceLastTrader.</summary>
    public const double TraderSpawnChance = 0.3;

    /// <summary>Once this many consecutive floors have passed without a trader, the next floor forces one -- guarantees at least one trader every MaximumLevelsWithoutTrader + 1 floors.</summary>
    public const int MaximumLevelsWithoutTrader = 2;

    public const int TraderMinInventory = 10;
    public const int TraderMaxInventory = 20;

    /// <summary>
    /// A trader refuses to buy anything more once their stock (grown by selling to them over
    /// a long playthrough -- initial generation never exceeds TraderMaxInventory on its own)
    /// reaches this many items -- see TraderScreen.TrySell. Kept at or below 35 deliberately:
    /// TraderScreen's buy-tab item selection is a single keypress (1-9 then a-z, the same
    /// scheme MenuPrompt.OptionKey uses), which only ever addresses 35 distinct items -- a
    /// 36th item would sit there unable to ever be bought back.
    /// </summary>
    public const int TraderInventoryCap = 35;

    /// <summary>A trader's stock can include items up to this many levels ahead of the floor it spawned on -- gives shops the occasional aspirational item.</summary>
    public const int ItemLevelLookahead = 2;

    /// <summary>Baseline fraction of an item's GoldValue a trader pays when buying it from the player, before charisma adjustment.</summary>
    public const double TraderSellMultiplier = 0.50;

    public const int TraderIdentificationCost = 100;

    // Charisma-adjusted pricing -- baseline-centered at 10 (an average roll), same convention
    // CharacterClass.StrengthAttackMultiplier already uses, so an average character trades at
    // exactly the plain base price/TraderSellMultiplier with no bonus or penalty either way.
    public const double CharismaPriceScalingPerPoint = 0.03;
    public const double MinBuyPriceMultiplier = 0.7;
    public const double MaxBuyPriceMultiplier = 1.3;
    public const double MinSellPriceMultiplier = 0.15;
    public const double MaxSellPriceMultiplier = 0.85;
}
