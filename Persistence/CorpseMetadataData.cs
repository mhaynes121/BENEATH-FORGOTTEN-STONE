using BENEATH_FORGOTTEN_STONE.Entities;

namespace BENEATH_FORGOTTEN_STONE.Persistence;

/// <summary>Serializable mirror of Entities.CorpseMetadata -- shared by a loot-bearing Corpse (via CorpseData.Metadata) and a portable corpse Item (via ItemData.Corpse).</summary>
public class CorpseMetadataData
{
    public string CorpseId { get; set; }
    public string DefinitionId { get; set; }
    public string OriginalDisplayName { get; set; }
    public CreatureType CreatureType { get; set; }
    public int OriginalLevel { get; set; }
    public Size OriginalSize { get; set; }

    /// <summary>The already-resolved weight, persisted verbatim -- never recomputed from OriginalSize on load, per the proposal's own "do not recalculate it later" rule.</summary>
    public double Weight { get; set; }

    public CorpseOrigin Origin { get; set; }
    public string OwnerName { get; set; }
}
