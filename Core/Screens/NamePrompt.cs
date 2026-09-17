namespace BENEATH_FORGOTTEN_STONE.Core.Screens;

public static class NamePrompt
{
    /// <summary>Flavor variants for the name-entry line -- one is picked at random per Prompt() call instead of always showing the same plain "Enter your character's name:".</summary>
    private static readonly string[] Prompts =
    {
        "Whose remains lie before us?",
        "How reads the final epitaph?",
        "By what title do you court death?",
        "Whose bones do we unearth today?",
        "Identify the next carcass:",
        "The name of the next casualty:",
        "Whose blood will paint these stones?",
        "Who signs the execution warrant?",
        "Name the doomed soul:",
        "Whose flesh enters the grinder?",
        "Who comes to rot here?",
        "Whose scream will echo next?",
        "Label the fresh meat:",
        "What name will the maggots forget?",
        "Whose ghost shall haunt these halls?",
        "Who claims this shallow grave?",
        "Name the target of the curse:",
        "Whose ashes will dust the floor?",
        "Who brings their bones to the heap?",
        "What title fades upon your tombstone?",
        "Whose light is about to fail?",
        "Identify the soon-to-be fallen:",
        "Who offers their blood to the stone?",
        "What name is written in charcoal?",
        "Whose marrow will the monsters pick?",
        "Speak your true name to the dark.",
        "Who steps into the endless abyss?",
        "What label haunts these empty walls?",
        "Whisper your moniker to the void.",
        "Who calls from across the threshold?",
        "What shape does the shadow take?",
        "Whose reflection fades in the mirror?",
        "State the word that binds your soul.",
        "What sigil marks your lineage?",
        "Who walks the path of no return?",
        "By what vibration are you known?",
        "The cipher of your identity:",
        "What glyph represents your spirit?",
        "Who wakes from the forgotten dream?",
        "Name the echo in the mist:",
        "Who seeks the unwritten truth?",
        "What alias hides your past?",
        "Who listens to the walls breathe?",
        "State your secret designation:",
        "Whose mask has slipped tonight?",
        "Who wanders between the frames?",
        "What name did the wind whisper?",
        "Who follows the silent guide?",
        "Identify the anomaly:",
        "What code opens the inner gate?",
        "Who seeks the bottom floor?",
        "State your birth-mark or title.",
        "Designation of the lone wanderer:",
        "Who bears the flickering torch?",
        "Name the bold adventurer:",
        "Who knocks at the dungeon gate?",
        "State your lineage and house:",
        "What name goes into the ledger?",
        "Who dares defy the labyrinth?",
        "Enter the hero's true call-sign:",
        "Who draws the rusted blade?",
        "Name the seeker of fortune:",
        "Who tumbles into the deep?",
        "State your name, brave traveler.",
        "Who answers the ancient call?",
        "What title do the taverns know?",
        "Who breaks the seal tonight?",
        "Name the explorer of ruins:",
        "Who tracks the beast home?",
        "State your moniker, sellsword.",
        "Who maps the dark places?",
        "What name commands the blade?",
        "Who risks it all for gold?",
        "Name the outrider from the surface:",
        "Who enters the grid?",
        "Whose teeth will line the pit?",
        "What name dies on your lips?",
        "Who will the vultures feed on?",
        "Identify the next pile of ash:",
        "Whose final mistake begins right now?",
        "Name the meat for the crows:",
        "Who leaves their sanity at the door?",
        "Whose sanity will break here first?",
        "What name shall the soil drink?",
        "What word awakens the sleeping gears?",
        "Who steps out of the static?",
        "What name is carved inside your skull?",
        "Who answers to the shifting runes?",
        "What phantom pulls at the strings?",
        "Who is the architect of this descent?",
        "State the name you left behind:",
        "Which shadow claims to be alive?",
        "Who carries the map of the deep?",
        "Name the defiler of ancient shrines:",
        "Who breaks the peace of the dead?",
        "State your oath and your title:",
        "Who hunts for the hidden hoard?",
        "What name shall the bards sing?",
        "Who claims the right of entry?"
    };

    public static string Prompt()
    {
        // TrimEnd guards against a stray trailing space on any entry above -- Prompt() always
        // appends its own single space before the cursor, so a trailing space in the array
        // would otherwise double up.
        string prompt = Prompts[new Random().Next(Prompts.Length)].TrimEnd();

        using var margin = new ScreenMargin();
        while (true)
        {
            ConsoleSafety.TryClear();
            ConsoleSafety.TrySetCursorPosition(0, 0);
            Console.WriteLine("Welcome to the Kingdom of Aldren, traveller.");
            Console.WriteLine();
            Console.Write($"{prompt} ");

            ConsoleSafety.TrySetCursorVisible(true);
            string input = Console.ReadLine();
            ConsoleSafety.TrySetCursorVisible(false);

            if (!string.IsNullOrWhiteSpace(input))
            {
                return input.Trim();
            }
        }
    }
}
