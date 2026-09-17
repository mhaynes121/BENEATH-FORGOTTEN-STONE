using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// The 3 size classes x 3 slot capacities x 4 weight-reduction tiers = 36 portable bag templates
/// (Persistent Containers and Magical Bags proposal), built programmatically rather than
/// hand-authored -- same "generate the combinatorial matrix" approach SpellScrollCatalog/
/// SpellbookCatalog already use for their own per-spell item lists. Every entry is
/// ItemType.Container with a non-null `container:`, so ContainerCatalog.All never needs manual
/// upkeep as the mechanical matrix itself doesn't change. Rarity is gated the same way every
/// other late-game item in this catalog already is -- MinimumLevel -- rather than inventing a
/// second, bag-specific rarity system.
/// </summary>
public static class ContainerCatalog
{
    private static readonly (ContainerSizeClass Size, string Name, double BaseWeight, int GoldBase)[] Sizes =
    {
        (ContainerSizeClass.Small, "Pouch", 0.5, 10),
        (ContainerSizeClass.Medium, "Satchel", 1.5, 20),
        (ContainerSizeClass.Large, "Adventurer's Pack", 3.0, 35)
    };

    private static readonly int[] SlotCapacities = { 4, 6, 8 };

    private static readonly (double Reduction, string Suffix, int MinimumLevel, int GoldMultiplier)[] ReductionTiers =
    {
        (0.00, "", 0, 1),
        (0.25, " of Lightening", 2, 2),
        (0.50, " of Greater Lightening", 5, 4),
        (0.75, " of Weightlessness", 9, 8)
    };

    public static readonly IReadOnlyList<Item> All = Build();

    private static List<Item> Build()
    {
        var bags = new List<Item>();
        foreach (var (size, name, baseWeight, goldBase) in Sizes)
        {
            var itemSize = size switch
            {
                ContainerSizeClass.Small => ItemSize.Small,
                ContainerSizeClass.Medium => ItemSize.Medium,
                _ => ItemSize.Large
            };

            foreach (var slotCapacity in SlotCapacities)
            {
                // A small extra level-gate per capacity step within the same reduction tier --
                // an 8-slot bag is a slightly later find than a 4-slot one even at the same
                // magical tier.
                int capacityLevelOffset = slotCapacity switch { 4 => 0, 6 => 1, _ => 2 };

                foreach (var (reduction, suffix, minimumLevel, goldMultiplier) in ReductionTiers)
                {
                    // Deliberately just "{name}{suffix}" (e.g. "Pouch of Lightening") with no
                    // size-class word or slot-count qualifier -- both are already surfaced
                    // elsewhere (ItemSize on the examine screen, "Slots: X/Y" in ContainerScreen,
                    // the Inventory tab's own container summary line), so keeping them in the
                    // name itself was redundant. This means the 4/6/8-slot versions of the same
                    // tier share an identical Name -- SaveManager.ResolveItem disambiguates them
                    // at load time using the saved Container shape (see its own doc comment).
                    string displayName = $"{name}{suffix}";
                    int goldValue = (goldBase + slotCapacity * 2) * goldMultiplier;

                    bags.Add(new Item(
                        displayName, '(', $"A {name.ToLowerInvariant()} with {slotCapacity} slots{(reduction > 0 ? $", magically lightened by {reduction:0%}" : "")}.",
                        ItemType.Container,
                        longDescription: BuildLongDescription(name, slotCapacity, reduction),
                        size: Size.Small, itemSize: itemSize, weight: baseWeight,
                        minimumLevel: minimumLevel + capacityLevelOffset,
                        goldValue: goldValue,
                        container: new ContainerComponent(ContainerKind.PortableBag, size, slotCapacity, reduction)));
                }
            }
        }
        return bags;
    }

    private static string BuildLongDescription(string name, int slotCapacity, double reduction) => reduction switch
    {
        0.00 => $"A sturdy {name.ToLowerInvariant()} with {slotCapacity} interior slots -- nothing magical about it, just honest storage.",
        0.25 => $"A {name.ToLowerInvariant()} stitched with a faintly shimmering thread. Whatever's carried inside feels noticeably lighter than it should.",
        0.50 => $"A {name.ToLowerInvariant()} woven through with glowing sigils. Its contents feel like a fraction of their true weight.",
        _ => $"A {name.ToLowerInvariant()} that seems to swallow weight entirely. Even packed full, it rests on the shoulder like it's nearly empty."
    };
}
