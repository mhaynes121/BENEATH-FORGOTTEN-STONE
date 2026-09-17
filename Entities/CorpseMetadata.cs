namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Corpse System: everything a future Necromancer (or the UI) needs about the actor a corpse came
/// from, WITHOUT preserving a live copy of that actor -- no temporary effects, current combat
/// target, energy, or cooldowns. DefinitionId is the future-facing field: a stable catalog key
/// (Monster.Name, e.g. "giant rat" -- never DisplayName, which for a boss is a generated proper
/// name) that can look up an appropriate creature to recreate later, independent of the
/// presentation text (OriginalDisplayName) shown in menus today. Shared, immutable identity data
/// for both lifecycle states a corpse passes through -- see Dungeon.Corpse (loot-bearing) and
/// CorpseItemFactory (the portable Item form) -- so neither needs its own copy of this information.
/// </summary>
public class CorpseMetadata
{
    public Guid CorpseId { get; }

    /// <summary>Stable creature/actor definition key -- Monster.Name for a monster (including a boss, whose Name is untouched by boss-naming), Pet.DefinitionId for a pet. Never a generated display name.</summary>
    public string DefinitionId { get; }

    /// <summary>Presentation text only -- a boss's generated proper name, or an ordinary monster/pet's common name. See CorpseItemFactory.FormatName for how this becomes "corpse of a giant rat" / "corpse of Lormax Golden Wing".</summary>
    public string OriginalDisplayName { get; }

    public CreatureType CreatureType { get; }
    public int OriginalLevel { get; }
    public Size OriginalSize { get; }

    /// <summary>
    /// Resolved once at creation from OriginalSize (see CorpseConfig.WeightFor) and frozen forever
    /// after -- the proposal's own explicit rule: "store the resolved weight... do not recalculate
    /// it later from mutable creature definitions." A future rebalance of the weight table must
    /// never retroactively change an already-existing corpse's weight.
    /// </summary>
    public double Weight { get; }

    public CorpseOrigin Origin { get; }

    /// <summary>
    /// Owner's display name, when applicable (a Pet's corpse only) -- null otherwise. A plain name
    /// string rather than a reference or a numeric ID: this codebase has no persistent actor-ID
    /// system at all (Monsters/Players are never referenced by a stable ID, only by in-memory
    /// object reference), so this mirrors that existing convention rather than inventing one just
    /// for corpses.
    /// </summary>
    public string OwnerName { get; }

    public CorpseMetadata(string definitionId, string originalDisplayName, CreatureType creatureType,
        int originalLevel, Size originalSize, CorpseOrigin origin, string ownerName = null)
        : this(Guid.NewGuid(), definitionId, originalDisplayName, creatureType, originalLevel, originalSize,
              CorpseConfig.WeightFor(originalSize), origin, ownerName)
    {
    }

    /// <summary>Restore-from-save path: preserves the ORIGINAL CorpseId and the already-resolved Weight instead of re-deriving either.</summary>
    public CorpseMetadata(Guid corpseId, string definitionId, string originalDisplayName, CreatureType creatureType,
        int originalLevel, Size originalSize, double weight, CorpseOrigin origin, string ownerName)
    {
        CorpseId = corpseId;
        DefinitionId = definitionId;
        OriginalDisplayName = originalDisplayName;
        CreatureType = creatureType;
        OriginalLevel = originalLevel;
        OriginalSize = originalSize;
        Weight = weight;
        Origin = origin;
        OwnerName = ownerName;
    }
}
