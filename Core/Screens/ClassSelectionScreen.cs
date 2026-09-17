using BENEATH_FORGOTTEN_STONE.Entities;

namespace BENEATH_FORGOTTEN_STONE.Core.Screens;

public static class ClassSelectionScreen
{
    public static CharacterClass Prompt()
    {
        var options = CharacterClass.All;
        var labels = options.Select(c => $"{c.Name} - {c.Description}").ToList();
        int index = MenuPrompt.Choose("Choose your class:", labels).Value;
        return options[index];
    }
}
