namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>Builds a CorpseMetadata snapshot from a dying actor -- the only place that decides how each actor kind maps onto the metadata fields.</summary>
public static class CorpseMetadataFactory
{
    public static CorpseMetadata ForMonster(Monster monster) => new(
        definitionId: monster.Name,
        originalDisplayName: monster.DisplayName,
        creatureType: monster.CreatureType,
        originalLevel: monster.Level,
        originalSize: monster.Size,
        origin: monster.IsBoss ? CorpseOrigin.Boss : CorpseOrigin.Ordinary);

    public static CorpseMetadata ForPet(Pet pet) => new(
        definitionId: pet.DefinitionId,
        originalDisplayName: pet.DisplayName,
        creatureType: pet.CreatureType,
        originalLevel: pet.Level,
        originalSize: Size.Small,
        origin: CorpseOrigin.Pet,
        ownerName: OwnerNameFor(pet.Owner));

    private static string OwnerNameFor(Actor owner) => owner switch
    {
        Player player => player.Name,
        Monster monster => monster.DisplayName,
        _ => null
    };
}
