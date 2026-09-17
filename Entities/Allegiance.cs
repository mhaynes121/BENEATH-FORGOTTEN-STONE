namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Pet and Companion System: the one centralized "are these two friendly" check every
/// hostility-adjacent system (bump-attacks, AoE/projectile friendly fire, monster target
/// acquisition) should consult instead of assuming every non-player actor is fair game. Reads
/// Pet.Owner rather than checking for the player directly, per the proposal's own emphasis --
/// see Pet's own doc comment.
/// </summary>
public static class Allegiance
{
    public static bool AreFriendly(Actor a, Actor b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }
        if (a is Pet petA && ReferenceEquals(petA.Owner, b))
        {
            return true;
        }
        if (b is Pet petB && ReferenceEquals(petB.Owner, a))
        {
            return true;
        }
        if (a is Pet pa && b is Pet pb && pa.Owner != null && ReferenceEquals(pa.Owner, pb.Owner))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Pet and Companion System section 13: walks Pet.Owner chains from a direct damage source up
    /// to whoever should actually receive kill rewards -- "a hostile creature killed by the dog
    /// should grant normal experience, gold, and kill-record credit to the player owner," while
    /// "monster-owned pets must never grant their kills to the player merely because all pets use
    /// the same system." Cycle-protected (a visited set plus a depth cap) even though no ownership
    /// chain longer than one hop exists yet, per the proposal's own "prepares the system for
    /// summoned minions owned by another companion" framing. Returns `damageSource` unchanged
    /// when it isn't a Pet at all (the ordinary player-kills-a-monster case).
    /// </summary>
    public static Actor ResolveRewardBeneficiary(Actor damageSource)
    {
        var current = damageSource;
        var visited = new HashSet<Actor>();
        while (current is Pet pet && pet.Owner != null && visited.Add(current) && visited.Count <= 8)
        {
            current = pet.Owner;
        }
        return current;
    }
}
