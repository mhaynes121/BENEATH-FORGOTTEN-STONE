namespace BENEATH_FORGOTTEN_STONE.Entities.Components;

public class ExperienceComponent
{
    public long Current { get; set; }
    public long ToNextLevel { get; set; }

    public ExperienceComponent(long toNextLevel)
    {
        ToNextLevel = toNextLevel;
        Current = 0;
    }
}
