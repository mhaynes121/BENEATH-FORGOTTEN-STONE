using BENEATH_FORGOTTEN_STONE.Entities.Components;
using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// One scroll item per Mage-castable spell in SpellCatalog.All (exclusive
/// Mage spells plus the AllSpellcasters-shared ones), built programmatically
/// rather than hand-written like Items.cs -- nothing needs to reference an
/// individual scroll by name (unlike Items.HealthPotion etc.), and this way
/// they can never drift out of sync with the spell catalog. Priest-castable
/// spells get the mirrored Spellbook item instead -- see SpellbookCatalog.
/// A scroll's MinimumLevel always equals its spell's Level, so the existing
/// ItemRequirementValidator/loot-level-window logic gates it correctly with
/// no changes. Reading one (the Learn Spell command) permanently adds the
/// spell to KnownSpells -- see GameLoop.HandleLearnSpell.
/// </summary>
public static class SpellScrollCatalog
{
    public static readonly IReadOnlyList<Item> All = BuildScrolls();

    private static List<Item> BuildScrolls()
    {
        var scrolls = new List<Item>();
        foreach (var spell in SpellCatalog.All.Where(s => s.CanBeCastBy(CharacterClass.Mage)))
        {
            scrolls.Add(new Item(
                $"Scroll of {spell.Name}", '?', $"A scroll that teaches {spell.Name} when read.",
                ItemType.Scroll, charges: 1,
                minimumLevel: spell.Level,
                goldValue: spell.Level * 15,
                teachesSpell: spell,
                size: Size.Small, itemSize: ItemSize.Small, weight: 0.25,
                longDescription: $"Faded arcane script covers this scroll. Reading it (the Learn Spell command) permanently teaches {spell.Name} to a spellcaster capable of casting it."));
        }
        return scrolls;
    }
}
