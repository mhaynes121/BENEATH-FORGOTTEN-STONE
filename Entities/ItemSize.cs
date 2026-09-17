namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// A dedicated five-tier physical-size classification for items -- independent of the existing
/// three-tier Size enum, which stays exactly as it was (monster size, and Item.Size's own
/// existing job of monster-loot eligibility and EquipmentCompatibility's shield/weapon class-size
/// gating). Expanding Size in place would have silently widened those two unrelated systems;
/// ItemSize exists purely to drive the Lost Items system (see ItemLossRules/ItemLossConfig) and
/// the item-inspection "Size:" display. See ItemSizeMapping.MinimumMonsterSizeForLoot for the one
/// place these two size systems intentionally meet.
/// </summary>
public enum ItemSize
{
    VerySmall,
    Small,
    Medium,
    Large,
    VeryLarge
}

public static class ItemSizeExtensions
{
    /// <summary>"VeryLarge" -&gt; "Very Large" -- the bare enum name reads fine for the other three tiers, but the two compound ones need a space for player-facing display (item inspection, loss messages).</summary>
    public static string ToDisplayString(this ItemSize size) => size switch
    {
        ItemSize.VerySmall => "Very Small",
        ItemSize.VeryLarge => "Very Large",
        _ => size.ToString()
    };
}

/// <summary>
/// Bridges the new five-tier ItemSize to the existing three-tier monster/loot Size, so
/// LootGenerator's eligibility check keeps using a single Size comparison against Monster.Size
/// without needing its own parallel five-tier monster scale. VerySmall and Small both map to the
/// same minimum (Small) -- a Very Small item is, if anything, easier for a small monster to have
/// been carrying, not harder -- and VeryLarge maps to the existing ceiling (Large), so a Large
/// monster can still drop a Very Large weapon exactly as it could a Large one before this system
/// existed.
/// </summary>
public static class ItemSizeMapping
{
    public static Size MinimumMonsterSizeForLoot(ItemSize itemSize) => itemSize switch
    {
        ItemSize.VerySmall => Size.Small,
        ItemSize.Small => Size.Small,
        ItemSize.Medium => Size.Medium,
        ItemSize.Large => Size.Large,
        ItemSize.VeryLarge => Size.Large,
        _ => Size.Large
    };
}
