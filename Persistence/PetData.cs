using BENEATH_FORGOTTEN_STONE.Entities;

namespace BENEATH_FORGOTTEN_STONE.Persistence;

/// <summary>
/// Pet and Companion System save data. Deliberately does NOT persist MaxHp/BasePhysicalAttackPower/
/// DefensePower -- those are always re-derived deterministically from Level via
/// PetProgression.SyncToLevel, and persisting them separately would risk exactly the stat drift
/// the central stat-scaling function exists to prevent (if the formula is ever retuned, an old
/// save would otherwise keep stale numbers forever). Only CurrentHp is persisted, to preserve the
/// exact wounded state across a save/load. PreferredTarget/ThreatMemory are transient and never
/// persisted at all -- see Pet's own doc comment.
/// </summary>
public class PetData
{
    public string DefinitionId { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Level { get; set; }
    public int CurrentHp { get; set; }
    public int Energy { get; set; }
    public PetLifecycleState LifecycleState { get; set; }
    public long RespawnAtOwnerTurn { get; set; }
    public List<ActiveEffectData> ActiveEffects { get; set; } = new();
}
