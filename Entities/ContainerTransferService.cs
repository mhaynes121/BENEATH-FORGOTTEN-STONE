using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Entities;

public class ContainerTransferResult
{
    public bool Success { get; init; }
    public string Message { get; init; }
}

/// <summary>
/// The only two ways an item ever moves in or out of a container -- matching the two-pane
/// Container screen (container contents | player's own top-level inventory), not a general
/// multi-endpoint transfer graph. Moving a chest item into a carried bag is therefore two
/// ordinary steps (Remove into inventory, then Put from inventory into the bag) rather than one
/// direct container-to-container transfer -- this matches the proposal's own example screen
/// layout and avoids a combinatorial API. All validation happens before either collection is
/// mutated, so a failed transfer can never lose or duplicate an item.
/// </summary>
public static class ContainerTransferService
{
    /// <summary>Moves an item from player.Inventory.Items into container.Contents. Never weight-checked -- moving INTO a bag can only reduce or preserve total carried weight (WeightReduction is always >= 0), and moving into a Chest removes the item from carried weight entirely.</summary>
    public static ContainerTransferResult TryPut(Player player, ContainerComponent container, Item item, int? quantity = null)
    {
        if (!player.Inventory.Items.Contains(item))
        {
            return new ContainerTransferResult { Success = false, Message = "" };
        }

        if (!ContainerRules.CanInsert(container, item, out string reason))
        {
            return new ContainerTransferResult { Success = false, Message = reason };
        }

        var existingStack = ItemStacking.FindStackWithRoom(container.Contents.Items, item);
        if (existingStack == null && container.Contents.Items.Count >= container.SlotCapacity)
        {
            return new ContainerTransferResult { Success = false, Message = $"{container.SlotCapacity} slots are already full." };
        }

        int stackCount = ItemStacking.IsStackable(item) ? item.Charges ?? 1 : 1;
        int moveQuantity = Math.Clamp(quantity ?? stackCount, 1, stackCount);

        if (existingStack != null)
        {
            existingStack.Charges = (existingStack.Charges ?? 1) + moveQuantity;
            ItemStacking.ConsumeMany(player.Inventory, item, moveQuantity);
        }
        else if (moveQuantity >= stackCount)
        {
            player.Inventory.RemoveItem(item);
            container.Contents.AddItem(item);
        }
        else
        {
            var portion = item.Clone();
            portion.Charges = moveQuantity;
            ItemStacking.ConsumeMany(player.Inventory, item, moveQuantity);
            container.Contents.AddItem(portion);
        }

        return new ContainerTransferResult { Success = true, Message = $"You put the {item.DisplayName} away." };
    }

    /// <summary>Moves an item from container.Contents into player.Inventory.Items. Weight-checked: removing from a Chest is exactly a pickup (reuses EncumbranceCalculator.CanCarry directly); removing from a magical bag can push the player over capacity since the item stops being discounted the moment it leaves the bag. `containerDisplayName` is used only for the rejection message's wording (ContainerComponent has no back-reference to the Item/Chest that owns it).</summary>
    public static ContainerTransferResult TryRemove(Player player, ContainerComponent container, Item item, string containerDisplayName, int? quantity = null)
    {
        if (!container.Contents.Items.Contains(item))
        {
            return new ContainerTransferResult { Success = false, Message = "" };
        }

        if (container.Kind == ContainerKind.Chest)
        {
            if (!EncumbranceCalculator.CanCarry(player, item, out string chestReason))
            {
                return new ContainerTransferResult { Success = false, Message = chestReason };
            }
        }
        else
        {
            double resultingWeight = EncumbranceCalculator.CurrentWeight(player) + (item.Weight * container.WeightReduction);
            if (resultingWeight > EncumbranceCalculator.MaxCapacity(player))
            {
                return new ContainerTransferResult
                {
                    Success = false,
                    Message = $"Removing the {item.DisplayName} from the {containerDisplayName} would put you over your carrying capacity."
                };
            }
        }

        int stackCount = ItemStacking.IsStackable(item) ? item.Charges ?? 1 : 1;
        int moveQuantity = Math.Clamp(quantity ?? stackCount, 1, stackCount);

        var existingStack = ItemStacking.FindStackWithRoom(player.Inventory.Items, item);
        if (existingStack != null)
        {
            existingStack.Charges = (existingStack.Charges ?? 1) + moveQuantity;
            ItemStacking.ConsumeMany(container.Contents, item, moveQuantity);
        }
        else if (moveQuantity >= stackCount)
        {
            container.Contents.RemoveItem(item);
            player.Inventory.AddItem(item);
        }
        else
        {
            var portion = item.Clone();
            portion.Charges = moveQuantity;
            ItemStacking.ConsumeMany(container.Contents, item, moveQuantity);
            player.Inventory.AddItem(portion);
        }

        return new ContainerTransferResult { Success = true, Message = $"You take the {item.DisplayName}." };
    }
}
