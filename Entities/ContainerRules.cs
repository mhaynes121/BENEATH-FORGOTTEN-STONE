using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Whether an item may be inserted into a container -- size/category eligibility and the
/// no-nesting rule only. Slot-availability and carry-weight checks are ContainerTransferService's
/// job, not this class's -- this is purely "would this ever be allowed to sit in here."
/// </summary>
public static class ContainerRules
{
    public static bool CanInsert(ContainerComponent destination, Item item, out string reason)
    {
        // A single guard here is what makes "no bag inside a bag" AND "a bag inside a chest
        // can't hold another bag" the same rule with no special-casing -- it's evaluated at
        // whatever the DESTINATION container is, regardless of what (if anything) currently
        // holds that destination.
        if (item.Container != null && destination.Kind == ContainerKind.PortableBag)
        {
            reason = "A container cannot be placed inside another container.";
            return false;
        }

        if (destination.Kind == ContainerKind.Chest)
        {
            reason = "";
            return true; // a chest accepts any ordinary item, including bags (checked above)
        }

        bool eligible = destination.SizeClass switch
        {
            ContainerSizeClass.Small => item.ItemSize is ItemSize.VerySmall or ItemSize.Small,
            ContainerSizeClass.Medium => item.ItemSize is ItemSize.VerySmall or ItemSize.Small or ItemSize.Medium
                || item.Type is ItemType.Scroll or ItemType.Spellbook,
            ContainerSizeClass.Large => true,
            _ => false
        };

        reason = eligible ? "" : $"The {item.DisplayName} does not fit in this container.";
        return eligible;
    }
}
