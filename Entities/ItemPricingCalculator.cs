using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Trader buy/sell pricing off an item's own GoldValue (see Item.ComputeDefaultGoldValue),
/// adjusted by the buyer/seller's Charisma -- high Charisma buys cheaper and sells for more,
/// low Charisma the reverse. Baseline-centered at 10 (an average roll), the same convention
/// CharacterClass.StrengthAttackMultiplier already uses, so an average character trades at
/// exactly GoldValue/GoldValue*TraderSellMultiplier with no bonus or penalty either way.
/// </summary>
public static class ItemPricingCalculator
{
    public static int CalculateBuyPrice(Item item, Player buyer)
    {
        double charismaBonus = (buyer.Stats.Adjusted(PrimaryAttribute.Charisma) - 10) * TraderConfig.CharismaPriceScalingPerPoint;
        double multiplier = Math.Clamp(1.0 - charismaBonus, TraderConfig.MinBuyPriceMultiplier, TraderConfig.MaxBuyPriceMultiplier);
        return Math.Max(1, (int)Math.Round(item.GoldValue * multiplier));
    }

    public static int CalculateSellPrice(Item item, Player seller)
    {
        double charismaBonus = (seller.Stats.Adjusted(PrimaryAttribute.Charisma) - 10) * TraderConfig.CharismaPriceScalingPerPoint;
        double multiplier = Math.Clamp(TraderConfig.TraderSellMultiplier + charismaBonus, TraderConfig.MinSellPriceMultiplier, TraderConfig.MaxSellPriceMultiplier);
        return Math.Max(0, (int)Math.Round(item.GoldValue * multiplier));
    }
}
