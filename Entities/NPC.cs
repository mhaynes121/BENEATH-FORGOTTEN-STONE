namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Base for non-combat NPC types (Trader today; a future Healer/QuestGiver/Innkeeper would
/// also derive from this) -- distinct from Monster, which is always a potential combat
/// participant. Centralizing CanBeTargeted's override here means every future NPC type gets
/// full combat/spell/status immunity for free, without touching GameLoop/TargetResolver/
/// EffectProcessor again. Stationary by construction: an NPC is added to Level.Actors but
/// deliberately never registered with Level.Scheduler, so it's fully visible/blocking/
/// interactable but is simply never offered a turn -- see Trader's own doc comment.
/// </summary>
public abstract class NPC : Actor
{
    public override bool CanBeTargeted => false;
}
