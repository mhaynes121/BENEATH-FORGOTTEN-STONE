namespace BENEATH_FORGOTTEN_STONE.Persistence;

/// <summary>
/// Serializable reference to a catalog Item (Items.cs) -- resolved back
/// to the shared template by Name on load. Charges is only meaningful
/// for charge-limited items (e.g. a wand) and mirrors Item.Charges so a
/// partially-used wand doesn't come back full. IsIdentified mirrors
/// Item.IsIdentified the same way, so an unidentified item doesn't come
/// back identified after a reload -- see SaveManager.ToItemData/ResolveItem.
/// </summary>
public class ItemData
{
    public string Name { get; set; }
    public int? Charges { get; set; }
    public bool IsIdentified { get; set; }

    /// <summary>Mirrors Item.IsLit/RemainingLightDuration -- only meaningful for a light-emitting item (Candle/Torch/Lantern), harmlessly false/0 for everything else. See SaveManager.ToItemData/ResolveItem.</summary>
    public bool IsLit { get; set; }

    public int RemainingLightDuration { get; set; }

    /// <summary>Null for every ordinary item -- non-null mirrors Item.Container (a portable bag) and its own contents recursively. See SaveManager.ToItemData/ResolveItem.</summary>
    public ContainerData Container { get; set; }

    /// <summary>Corpse System: non-null only for a portable corpse item. A corpse has no catalog template at all (built fresh per death, never authored in Items.cs), so SaveManager.ResolveItem resolves it directly from this embedded metadata instead of the ordinary Name-based catalog lookup every other item uses.</summary>
    public CorpseMetadataData Corpse { get; set; }
}
