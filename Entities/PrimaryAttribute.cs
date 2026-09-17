namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// The seven rolled primary attributes. Named distinctly from
/// BENEATH_FORGOTTEN_STONE.Entities.Spells.Stat (a different, already-shipped
/// concept: PhysicalAttack/MagicalAttack/Armor/MagicResistance/
/// MovementSpeed) to avoid confusing the two.
/// </summary>
public enum PrimaryAttribute
{
    Strength,
    Constitution,
    Agility,
    Wisdom,
    Knowledge,

    /// <summary>Social grace/presence -- drives trader buy/sell pricing (see ItemPricingCalculator). Otherwise unused, same as every other primary attribute has its own narrow set of consumers.</summary>
    Charisma,

    /// <summary>
    /// Rolled and shown alongside every other attribute (character creation and the character
    /// sheet) but deliberately inert -- no combat, loot, search, trap, crit, or item-loss system
    /// reads it. See the Revised Luck proposal: a visible attribute with racial flavor (Halfling
    /// rolls highest) that isn't wired into anything yet, rather than a mechanic bolted on before
    /// its actual effect is designed.
    /// </summary>
    Luck
}
