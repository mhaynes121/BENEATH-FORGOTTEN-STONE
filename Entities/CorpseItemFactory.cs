using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>Builds the portable corpse Item form from CorpseMetadata -- the "emptied remains" state (proposal section 7). Never a catalog template: every instance is built fresh, one per corpse, and never shared or cloned from a shared reference.</summary>
public static class CorpseItemFactory
{
    public const char Glyph = '%';

    public static Item CreatePortableItem(CorpseMetadata metadata) => new(
        name: FormatName(metadata),
        symbol: Glyph,
        description: $"The mortal remains of {Subject(metadata)}.",
        type: ItemType.Corpse,
        // A real explicit override, not 0 (which this codebase's own convention treats as "use
        // the computed default" -- see Item.GoldValue) -- a corpse is "Sellable: No, unless
        // corpse trading is deliberately added later," so this is deliberately near-worthless
        // rather than actually zero.
        goldValue: 1,
        canDropAsLoot: false,
        canBeLost: false,
        isIdentified: true,
        weight: metadata.Weight,
        corpseMetadata: metadata);

    /// <summary>"corpse of a giant rat" / "corpse of Lormax Golden Wing" (a boss's generated proper name never takes an article) -- shared by the item's own display Name and by corpse-selection menu labels (see GameLoop's open-container flow), so both always agree.</summary>
    public static string FormatName(CorpseMetadata metadata) => $"corpse of {Subject(metadata)}";

    private static string Subject(CorpseMetadata metadata) =>
        metadata.Origin == CorpseOrigin.Boss ? metadata.OriginalDisplayName : CombatMessages.WithArticle(metadata.OriginalDisplayName);
}
