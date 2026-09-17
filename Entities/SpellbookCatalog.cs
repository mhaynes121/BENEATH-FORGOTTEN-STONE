using BENEATH_FORGOTTEN_STONE.Entities.Components;
using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Mirrors SpellScrollCatalog for the Priest side: one spellbook item per
/// Priest-castable spell in SpellCatalog.All (exclusive Priest spells plus
/// the AllSpellcasters-shared ones). A shared spell therefore gets both a
/// Scroll (SpellScrollCatalog) and a Spellbook entry teaching the same
/// Spell instance -- whichever one a given class finds is the one that
/// works for them, since GameLoop.HandleLearnSpell/spell.CanBeCastBy don't
/// care which item type taught it. A spellbook's MinimumLevel always
/// equals its spell's Level, matching the scroll convention.
/// </summary>
public static class SpellbookCatalog
{
    public static readonly IReadOnlyList<Item> All = BuildSpellbooks();

    private static List<Item> BuildSpellbooks()
    {
        var spellbooks = new List<Item>();
        foreach (var spell in SpellCatalog.All.Where(s => s.CanBeCastBy(CharacterClass.Priest)))
        {
            spellbooks.Add(new Item(
                $"Spellbook of {spell.Name}", '+', $"A spellbook that teaches {spell.Name} when read.",
                ItemType.Spellbook, charges: 1,
                minimumLevel: spell.Level,
                goldValue: spell.Level * 15,
                teachesSpell: spell,
                size: Size.Small, itemSize: ItemSize.Medium, weight: 1.5,
                longDescription: $"A worn, leather-bound tome of devotional workings. Reading it (the Learn Spell command) permanently teaches {spell.Name} to a spellcaster capable of casting it."));
        }
        return spellbooks;
    }
}
