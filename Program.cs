using BENEATH_FORGOTTEN_STONE.Core;
using BENEATH_FORGOTTEN_STONE.Core.Screens;
using BENEATH_FORGOTTEN_STONE.Diagnostics;
using BENEATH_FORGOTTEN_STONE.Persistence;

namespace BENEATH_FORGOTTEN_STONE;

internal static class Program
{
    private static void Main(string[] args)
    {
        // Headless logic smoke test -- see Diagnostics/SelfTest.cs. Returns before any
        // console setup below, since it never opens an interactive screen.
        if (args.Length > 0 && args[0] == "--selftest")
        {
            Environment.Exit(SelfTest.Run());
        }

        ConsoleSafety.TrySetCursorVisible(false);
        ConsoleSafety.TryFitConsole(60, 24);

        while (true)
        {
            var option = MainMenuScreen.Show();

            switch (option)
            {
                case MainMenuOption.CreateCharacter:
                    StoryScreen.ShowOpeningStory();
                    var player = CharacterCreationScreen.Run();
                    new GameLoop(player).Run();
                    break;

                case MainMenuOption.LoadGame:
                    RunLoadedGame();
                    break;

                case MainMenuOption.Graveyard:
                    GraveyardScreen.Show();
                    break;

                case MainMenuOption.Credits:
                    CreditsScreen.Show();
                    break;

                case MainMenuOption.Exit:
                    return;
            }
        }
    }

    private static void RunLoadedGame()
    {
        var data = SaveManager.Load();
        if (data == null)
        {
            Renderer.RenderMessage("No saved game found.\n\nPress any key to return...");
            Console.ReadKey(true);
            return;
        }

        var player = SaveManager.ToPlayer(data);
        var floors = SaveManager.ToLevels(data);
        new GameLoop(player, data.FloorIndex, floors, data.LevelsSinceLastTrader ?? 0).Run();
    }
}
