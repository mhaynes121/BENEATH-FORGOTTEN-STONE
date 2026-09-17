using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities;

namespace BENEATH_FORGOTTEN_STONE.Core;

/// <summary>
/// Pure roll math for finding a concealed GroundItem -- Active Search (GameLoop.HandleSearch) and
/// passive discovery (GameLoop's movement/RecomputeFov/HandleLookHere hooks) both go through here
/// rather than rolling inline, so the two stay consistent and independently testable. Never
/// touches Console or mutates a GroundItem itself -- callers apply IsConcealed = false on success.
/// </summary>
public static class ItemDiscoveryRules
{
    /// <summary>Standard "every 2 points away from 10 shifts the modifier by 1" attribute-to-modifier curve, rounded down (so a modifier below 10 rounds further negative, not toward zero).</summary>
    public static int AttributeModifier(int attributeValue) => (int)Math.Floor((attributeValue - 10) / 2.0);

    /// <summary>d20 + Knowledge modifier + half the Agility modifier (rounded down) + a per-class bonus, against the item's stored ConcealmentDifficulty.</summary>
    public static bool RollActiveSearch(Player player, GroundItem groundItem, Random rng)
    {
        int roll = rng.Next(1, 21);
        int knowledgeMod = AttributeModifier(player.Stats.Adjusted(PrimaryAttribute.Knowledge));
        int agilityMod = AttributeModifier(player.Stats.Adjusted(PrimaryAttribute.Agility));
        int halfAgilityMod = (int)Math.Floor(agilityMod / 2.0);
        int total = roll + knowledgeMod + halfAgilityMod + ClassSearchBonus(player.Class);
        return total >= groundItem.ConcealmentDifficulty;
    }

    /// <summary>d20 + Knowledge modifier + a per-class bonus + a flat bonus while the player is standing directly on the item's tile -- deliberately weaker than RollActiveSearch (no Agility term, no way to boost it beyond class/standing) so Active Search stays meaningfully better than waiting to stumble onto something.</summary>
    public static bool RollPassiveDiscovery(Player player, GroundItem groundItem, bool playerStandingOnTile, Random rng)
    {
        int roll = rng.Next(1, 21);
        int knowledgeMod = AttributeModifier(player.Stats.Adjusted(PrimaryAttribute.Knowledge));
        int standingBonus = playerStandingOnTile ? PassiveStandingBonus : 0;
        int total = roll + knowledgeMod + ClassPassiveDiscoveryBonus(player.Class) + standingBonus;
        return total >= groundItem.ConcealmentDifficulty;
    }

    private const int PassiveStandingBonus = 4;

    /// <summary>Stage 2 fills this in per class (Thief +3, per the Phase 2 design doc) using the same centralized-per-class-table pattern as CharacterClass.AllowedThrowableCategories -- every class gets 0 for now.</summary>
    private static int ClassSearchBonus(CharacterClass characterClass) => 0;

    /// <summary>Stage 2 fills this in per class (Thief +2, Mage +2 while using magical illumination, Priest +2 for blessed items) -- every class gets 0 for now.</summary>
    private static int ClassPassiveDiscoveryBonus(CharacterClass characterClass) => 0;
}
