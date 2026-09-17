namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>Active: alive and present on some level's Actors/Scheduler. AwaitingRespawn: dead, removed from every level, waiting out its owner-turn delay -- see Pet.RespawnAtOwnerTurn.</summary>
public enum PetLifecycleState
{
    Active,
    AwaitingRespawn
}

/// <summary>
/// Pet and Companion System: a companion actor belonging to an Owner, distinct from Monster
/// (never hostile, never awards XP/gold/loot on death, never targeted by the player's own
/// bump-attack) even though it shares the same base Actor combat/AI/turn-scheduling plumbing.
/// Deliberately NOT a Monster subclass -- Monster carries a long list of hostile-creature-only
/// assumptions (loot generation, death rewards, boss scaling) that a companion must never trigger,
/// and suppressing all of those at every call site would be more invasive than a sibling class.
///
/// Everything here reads Owner rather than assuming "the player" -- see the Pet and Companion
/// System proposal's own emphasis on this: a future monster-owned pet, class summon, or charmed
/// creature can reuse this same class by simply setting Owner to something other than a Player.
/// Today only Player.Pet ever constructs one (see PetFactory), but nothing here hardcodes that.
/// </summary>
public class Pet : Actor
{
    public Actor Owner { get; set; }

    /// <summary>Which PetFactory definition this is ("pet_dog") -- lets persistence and future multi-pet-type code resolve presentation/progression without re-deriving it from Name.</summary>
    public string DefinitionId { get; set; }

    /// <summary>What kind of creature this pet is -- mirrors Monster.CreatureType (Pet doesn't inherit Monster, so it needs its own copy). Used by the Corpse System to record a dead pet's CorpseMetadata the same way a monster's own CreatureType is recorded.</summary>
    public CreatureType CreatureType { get; set; } = CreatureType.Animal;

    public PetLifecycleState LifecycleState { get; set; } = PetLifecycleState.Active;

    /// <summary>Meaningless while LifecycleState is Active. Compared against Owner.TurnCount (a Player's own global completed-turn counter) rather than any Level.TurnNumber -- see the proposal's section 10 on why a floor-specific deadline behaves wrong after stair travel.</summary>
    public long RespawnAtOwnerTurn { get; set; }

    /// <summary>Transient -- deliberately never persisted (see the proposal's section 15: "if a saved target cannot be resolved, discard it safely"). A fresh load/respawn just starts with none and reacquires naturally on its next turn.</summary>
    public Actor PreferredTarget { get; set; }

    /// <summary>Transient, never persisted -- same reasoning as PreferredTarget above.</summary>
    public HashSet<Actor> ThreatMemory { get; } = new();

    public override AttackType AttackType => AttackType.Bite;
}
