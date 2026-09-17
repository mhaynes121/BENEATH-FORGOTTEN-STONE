using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Centralizes which item categories combine into one inventory/trader-stock entry with a
/// running count -- Charges, the exact mechanism Lockpicks ("a set of six") already used
/// before this existed, now generalized to potions/scrolls/spellbooks/keys/throwables too,
/// instead of every consumer duplicating its own "is this the same kind of item" check.
/// Every catalog entry this applies to is given an explicit charges: N (see Items.cs,
/// SpellScrollCatalog, SpellbookCatalog) so DungeonGenerator/LootGenerator/Trader's existing
/// "Charges != null -> Clone()" spawn-time rule already keeps every stack independent with no
/// changes needed there -- the same rule that already protected Lockpicks/Wands from sharing
/// mutable state now protects every newly-stackable category too.
/// </summary>
public static class ItemStacking
{
    /// <summary>Potions (and every other Consumable) are capped -- 5 of any one kind is already a lot to be carrying, and an unbounded potion stack would trivialize inventory management entirely. Every other stackable category (scrolls, spellbooks, keys, lockpicks, throwables) has no cap, matching Lockpicks' own long-established "a growing set" precedent.</summary>
    public const int MaxPotionStackSize = 5;

    /// <summary>
    /// A CanBeThrown item additionally needs Charges set to count as stackable -- true for every
    /// ordinary throwable (Dart, Rock, Shuriken, ThrowingKnife/Axe, Short Spear all set
    /// charges: 1), but NOT for Long Spear, which deliberately omits it (stays charges: null) so
    /// it's carried and equipped individually rather than stacked -- see the design doc's own
    /// "Short Spear stackable, Long Spear individual" distinction, and
    /// EquipmentCompatibility.GetCompatibleSlots(Item), which uses this same check to decide a
    /// hybrid spear's default slot preference.
    /// </summary>
    public static bool IsStackable(Item item) =>
        item.Type is ItemType.Consumable or ItemType.Scroll or ItemType.Spellbook or ItemType.Key or ItemType.Lockpick or ItemType.Ammunition
        || (item.CanBeThrown && item.Charges.HasValue);

    /// <summary>Null means unlimited.</summary>
    public static int? MaxStackSizeFor(Item item) => item.Type == ItemType.Consumable ? MaxPotionStackSize : null;

    /// <summary>
    /// Two items belong in the same stack only if they're both stackable, share the same
    /// catalog Name (their true identity -- DisplayName is masked while unidentified, but
    /// merging by the true Name is still safe: two items that only *look* alike while hidden
    /// but are actually different catalog entries are correctly kept apart), and agree on
    /// IsIdentified so a hidden and a revealed copy of the same rare case never merge into one
    /// display that would either prematurely reveal or wrongly re-hide it.
    /// </summary>
    public static bool CanStackTogether(Item a, Item b) =>
        IsStackable(a) && IsStackable(b) && a.Name == b.Name && a.IsIdentified == b.IsIdentified;

    /// <summary>The first existing stack in `items` that `item` could merge into -- same kind, and still under its cap (or uncapped). Null if none has room, meaning `item` needs its own new entry.</summary>
    public static Item FindStackWithRoom(IEnumerable<Item> items, Item item)
    {
        int? maxSize = MaxStackSizeFor(item);
        return items.FirstOrDefault(existing =>
            CanStackTogether(existing, item) && (maxSize == null || (existing.Charges ?? 1) < maxSize.Value));
    }

    /// <summary>
    /// Uses up exactly one unit of `item` -- decrements Charges and only removes it from
    /// `inventory` once the count reaches zero, the same decrement-not-remove shape
    /// BreakLockpickOnFailure/UseSkeletonKeyOnDoor already used before stacking generalized to
    /// potions/scrolls/spellbooks/throwables too. Safe on a non-stacked single-charge item as
    /// well (Charges null or 1 removes it outright, matching the old unconditional RemoveItem
    /// every one-off consumer used before this existed).
    /// </summary>
    public static void ConsumeOne(InventoryComponent inventory, Item item)
    {
        int remaining = (item.Charges ?? 1) - 1;
        if (remaining <= 0)
        {
            inventory.RemoveItem(item);
        }
        else
        {
            item.Charges = remaining;
        }
    }

    /// <summary>
    /// Same decrement-not-remove shape as ConsumeOne, but for a STACK sitting in an equipment
    /// slot (EquipmentSlot.Ammunition) instead of loose inventory -- see GameLoop's readied-item
    /// firing flow. Calls Player.Unequip when the count reaches zero (reusing its existing
    /// stat-modifier-removal/derived-stat recalculation path) rather than InventoryComponent.
    /// RemoveItem, since there's nothing here to remove from a list -- the slot itself just
    /// empties. Returns true if the slot was cleared (the final unit was just used), so the
    /// caller can report "you fire/throw your last X."
    /// </summary>
    public static bool ConsumeOneFromEquippedSlot(Player player, EquipmentSlot slot)
    {
        var item = player.Equipment.Get(slot);
        if (item == null)
        {
            return false;
        }

        int remaining = (item.Charges ?? 1) - 1;
        if (remaining <= 0)
        {
            player.Unequip(slot);
            return true;
        }

        item.Charges = remaining;
        return false;
    }

    /// <summary>Same decrement-not-remove shape as ConsumeOne, generalized to an arbitrary quantity -- see GameLoop.HandleDropItem's quantity prompt, which uses this to peel a chosen bundle off a carried stack while leaving the remainder untouched.</summary>
    public static void ConsumeMany(InventoryComponent inventory, Item item, int quantity)
    {
        int remaining = (item.Charges ?? 1) - quantity;
        if (remaining <= 0)
        {
            inventory.RemoveItem(item);
        }
        else
        {
            item.Charges = remaining;
        }
    }

    /// <summary>
    /// Merges any entries that CanStackTogether into as few entries as possible, respecting
    /// MaxStackSizeFor caps -- a one-time cleanup pass for stacks that ended up split for a
    /// historical reason (e.g. a type only just added to IsStackable's own list, so a character
    /// who picked up several before that change is carrying them as separate entries instead of
    /// one running count). A no-op when nothing needs merging, so it's safe to call
    /// opportunistically -- see TraderScreen.Show, which runs this once whenever the screen opens
    /// so the sell list never shows what should be one stack as several duplicate rows.
    /// </summary>
    public static void ConsolidateStacks(InventoryComponent inventory)
    {
        var items = inventory.Items;
        for (int i = 0; i < items.Count; i++)
        {
            var target = items[i];
            if (!IsStackable(target))
            {
                continue;
            }

            int? maxSize = MaxStackSizeFor(target);
            for (int j = items.Count - 1; j > i; j--)
            {
                var candidate = items[j];
                if (!CanStackTogether(target, candidate))
                {
                    continue;
                }
                if (maxSize.HasValue && (target.Charges ?? 1) >= maxSize.Value)
                {
                    break; // target is already full -- no earlier scan of later entries will help either
                }

                int room = maxSize.HasValue ? maxSize.Value - (target.Charges ?? 1) : int.MaxValue;
                int moving = Math.Min(room, candidate.Charges ?? 1);
                target.Charges = (target.Charges ?? 1) + moving;

                int remaining = (candidate.Charges ?? 1) - moving;
                if (remaining <= 0)
                {
                    items.RemoveAt(j);
                }
                else
                {
                    candidate.Charges = remaining;
                }
            }
        }
    }
}
