using BENEATH_FORGOTTEN_STONE.Entities.AI;
using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Builds the temporary test pet dog and wires it to an owner. Per the proposal's section 16,
/// the dog's own definition here holds only presentation and base stat profile -- no targeting
/// or lifecycle rules, which live on Pet/PetAI/GameLoop instead.
/// </summary>
public static class PetFactory
{
    public const string DogDefinitionId = "pet_dog";

    /// <summary>X/Y are left at their default (0,0) -- GameLoop places a freshly created pet next to its owner the first time it enters a level; a restored pet's saved position is applied by the caller instead.</summary>
    public static Pet CreateDog(Actor owner)
    {
        var pet = new Pet
        {
            DefinitionId = DogDefinitionId,
            Name = "pet dog",
            Symbol = 'd',
            Color = ConsoleColor.DarkYellow,
            ShortDescription = "A loyal dog, ears perked and alert to your every move.",
            LongDescription = "Your dog presses close to your side, muscles coiled and ready to spring to your defense at the first sign of trouble.",
            Speed = PetProgression.FixedSpeed,
            Mana = new ManaComponent(0),
            AI = new PetAI()
        };

        PetProgression.SyncToLevel(pet, owner.Level);
        pet.Health.SetCurrent(pet.Health.Max);

        AttachToOwner(pet, owner);
        return pet;
    }

    /// <summary>
    /// Sets the owner/pet cross-references and (for a Player owner) subscribes to OnLevelUp so
    /// the pet's stats resync automatically on every future level gained -- called for a freshly
    /// created pet AND for one just restored from a save (a fresh Player instance has no
    /// subscribers of its own, so this must run again after every reconstruction, not just once
    /// at creation). Idempotent to call more than once for the same pair: CharacterClass/Player
    /// never call this twice in practice, but nothing breaks if they did beyond a duplicate
    /// (harmless) resync on the next level-up.
    /// </summary>
    public static void AttachToOwner(Pet pet, Actor owner)
    {
        pet.Owner = owner;

        if (owner is Player player)
        {
            player.Pet = pet;
            player.OnLevelUp += (_, newLevel, _, _) =>
            {
                if (player.Pet == pet)
                {
                    PetProgression.SyncToLevel(pet, newLevel);
                    player.PendingPetMessage = "Your pet dog grows stronger alongside you!";
                }
            };
        }
    }
}
