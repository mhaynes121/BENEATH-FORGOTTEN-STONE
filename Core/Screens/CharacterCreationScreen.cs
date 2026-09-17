using BENEATH_FORGOTTEN_STONE.Entities;
using BENEATH_FORGOTTEN_STONE.Entities.Components;
using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Core.Screens;

/// <summary>
/// Walks the player through name -> race -> class -> roll/reroll stats ->
/// confirm. Declining the confirmation restarts from the name prompt
/// (rerolling stats fresh too), per design. Starting weapon/armor are
/// deliberately rolled only after acceptance -- see RevealStartingGear --
/// so they're never shown, or reroll-able, during creation itself.
/// </summary>
public static class CharacterCreationScreen
{
    private static readonly Random rng = new();

    public static Player Run()
    {
        while (true)
        {
            string name = NamePrompt.Prompt();
            var race = RaceSelectionScreen.Prompt();
            var characterClass = ClassSelectionScreen.Prompt();
            var stats = StatRollScreen.Prompt(characterClass, race);

            if (ConfirmationScreen.Confirm(name, characterClass, race, stats))
            {
                var player = new Player(name, characterClass, race, stats);
                PetFactory.CreateDog(player);
                var gear = StartingGearGenerator.EquipStartingGear(player, rng);
                RevealStartingGear(gear.Weapon, gear.Armor, gear.RangedCompanion, player.KnownSpells.FirstOrDefault());
                return player;
            }
        }
    }

    private static void RevealStartingGear(Item weapon, Item armor, Item rangedCompanion, Spell startingSpell)
    {
        var pieces = new List<string>();
        if (weapon != null) pieces.Add(weapon.Name);
        if (armor != null) pieces.Add(armor.Name);
        if (rangedCompanion != null) pieces.Add(rangedCompanion.Name);

        string message = pieces.Count > 0
            ? $"You set out with a {string.Join(" and a ", pieces)} already in hand."
            : "You set out with nothing but your wits.";

        if (startingSpell != null)
        {
            message += $"\n\nYou already know {startingSpell.Name}.";
        }

        message += "\n\nPress any key to begin...";

        Renderer.RenderMessage(message);
        Console.ReadKey(true);
    }
}
