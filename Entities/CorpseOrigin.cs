namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Corpse System: what kind of actor a corpse came from -- future raising logic (a Necromancer)
/// can treat these differently (e.g. refuse to raise a Boss, or require a special ability) without
/// this implementation deciding those balance rules now. Summon has no current source (no
/// summoning class exists yet) but is included per the proposal's own forward-looking framing.
/// </summary>
public enum CorpseOrigin
{
    Ordinary,
    Boss,
    Pet,
    Summon
}
