using BENEATH_FORGOTTEN_STONE.Entities;

namespace BENEATH_FORGOTTEN_STONE.Core.Screens;

public static class RaceSelectionScreen
{
    public static Race Prompt()
    {
        var options = Race.All;
        var labels = options.Select(r => $"{r.Name} - {r.Description}").ToList();
        int index = MenuPrompt.Choose("Choose your race:", labels).Value;
        return options[index];
    }
}
