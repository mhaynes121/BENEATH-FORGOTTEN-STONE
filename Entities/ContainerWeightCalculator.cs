using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// The one place "how much does this item actually weigh, accounting for whatever it might be
/// carrying" is computed -- reused by EncumbranceCalculator (top-level inventory), pickup/drop
/// (a filled bag's complete weight), and ContainerTransferService (removing from a magical bag).
/// Nesting is disallowed (see ContainerRules), so this never recurses more than one level deep in
/// practice, but is written recursively anyway since that costs nothing and stays correct if a
/// bag sits inside a chest (the bag still applies its own reduction to its own contents for
/// display purposes, even though the chest itself never reduces anything).
/// </summary>
public static class ContainerWeightCalculator
{
    public static double EffectiveWeight(Item item) =>
        item.Container == null
            ? item.Weight
            : item.Weight + RawContentWeight(item.Container) * (1 - item.Container.WeightReduction);

    /// <summary>The discounted total of everything inside -- NOT the container's own weight, which the caller (EffectiveWeight) adds separately.</summary>
    public static double RawContentWeight(ContainerComponent container) =>
        container.Contents.Items.Sum(EffectiveWeight);
}
