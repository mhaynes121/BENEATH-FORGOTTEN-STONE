using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Centralizes carry-weight math -- mirrors ExperienceRewardCalculator/
/// GoldDropCalculator (a static, stateless formula class) rather than
/// living inside InventoryComponent, which knows nothing about Strength
/// or the separate EquipmentComponent it needs to sum against.
/// </summary>
public static class EncumbranceCalculator
{
    private const int LbsPerStrength = 10;

    /// <summary>STR * 10, plus any active Bull's Strength / Elixir of Burden bonus (Actor.CarryCapacityBonus).</summary>
    public static double MaxCapacity(Player player) =>
        (player.Stats.Adjusted(PrimaryAttribute.Strength) * LbsPerStrength) + player.CarryCapacityBonus;

    /// <summary>
    /// Sum of everything equipped plus everything carried -- moving an item between the two
    /// (equip/unequip) never changes this total, so only acquiring a new item (picking one up)
    /// needs a capacity check. A carried bag's complete effective weight (its own weight plus
    /// its discounted contents -- see ContainerWeightCalculator) is used for top-level inventory
    /// items, so a filled magical bag's weight reduction is reflected automatically. Equipped
    /// items always count at full RAW weight -- no equipment slot can hold a bag, and equipped
    /// items are never subject to container discounting either way.
    /// </summary>
    public static double CurrentWeight(Player player)
    {
        double weight = 0;
        foreach (var item in player.Inventory.Items)
        {
            weight += ContainerWeightCalculator.EffectiveWeight(item);
        }
        foreach (var kvp in player.Equipment.AllEquipped)
        {
            weight += kvp.Value.Weight;
        }
        return weight;
    }

    public static bool CanCarry(Player player, Item item, out string reason)
    {
        // A filled bag's COMPLETE effective weight (its own weight plus discounted contents) is
        // checked here, not just the bag's bare weight -- the proposal's own "picking up a full
        // bag must not be allowed based only on the empty bag's weight" rule.
        if (CurrentWeight(player) + ContainerWeightCalculator.EffectiveWeight(item) > MaxCapacity(player))
        {
            reason = $"The {item.DisplayName} is too heavy -- you're already at your carry capacity.";
            return false;
        }
        reason = "";
        return true;
    }
}
