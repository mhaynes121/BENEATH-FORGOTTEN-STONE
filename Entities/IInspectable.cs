namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Implemented by anything the Look command can inspect. ShortDescription
/// is shown for a distant glance; LongDescription for an adjacent or
/// explicit examination. Descriptions live on the object's own data --
/// LookService only decides which one to show, never generates text.
/// </summary>
public interface IInspectable
{
    string ShortDescription { get; }
    string LongDescription { get; }
}
