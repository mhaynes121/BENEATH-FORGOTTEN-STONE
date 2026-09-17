namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Single choke point for turning a requested damage amount into an actual HP change AND the
/// matching Adventure Record statistic, so a future damage source can't quietly forget to track
/// itself the way it could if every call site rolled its own Health.TakeDamage + bookkeeping.
/// Every damage site in the game (melee, spells, physical skills, projectiles, thrown weapons,
/// DoTs, thorns, traps, environmental Fire/Lava, off-preferred-terrain attrition) routes through
/// here instead of calling Health.TakeDamage directly.
/// </summary>
public static class CombatStatsTracker
{
    /// <summary>
    /// Applies `amount` damage to `target.Health` and returns the actual HP removed -- never more
    /// than the target actually had, so overkill is never double-counted (HealthComponent.TakeDamage
    /// already clamps at 0; the before/after diff here is what turns that clamp into the right
    /// number). `owner` is the actor responsible for this specific instance of damage -- the
    /// original attacker/caster/thrower, or the ActiveEffect's stored Owner for a delayed DoT tick;
    /// null for damage with no attributable owner (a trap, environmental Fire/Lava, off-preferred-
    /// terrain attrition). Does NOT touch LastDamageSource/LastDamageOwner -- callers keep setting
    /// those themselves exactly as before, since the death-message text and kill-credit rules are
    /// unrelated to lifetime statistics tracking.
    /// </summary>
    public static int ApplyDamage(Actor target, int amount, Actor owner)
    {
        if (target.Health == null)
        {
            return 0;
        }

        // New Priest Skill Progression (Intercession): the single centralized lethal-damage
        // interception point -- every damage source in the game (melee, spells, physical skills,
        // projectiles, DoTs, thorns, traps, environmental Fire/Lava, off-preferred-terrain
        // attrition) already routes through this one method, so this is the only place that
        // needs to know about Intercession at all. Only ever fires for damage that would
        // otherwise reduce the player to 0 HP or below; a non-lethal hit is completely unaffected.
        if (target is Player interceded && interceded.IntercessionActive && amount > 0 && amount >= target.Health.Current)
        {
            int beforeIntercession = target.Health.Current;
            interceded.IntercessionActive = false;
            target.Health.SetCurrent(1);
            interceded.PendingInterceptionMessage = "Divine light flares around you, preserving you at the brink of death!";
            return beforeIntercession - 1;
        }

        int before = target.Health.Current;
        target.Health.TakeDamage(amount);
        int actual = before - target.Health.Current;
        if (actual <= 0)
        {
            return actual;
        }

        if (target is Player targetPlayer)
        {
            targetPlayer.AdventureRecord.DamageTaken += actual;
        }
        // Pet and Companion System: a pet's own damage counts toward its owner's lifetime
        // DamageDealt too -- see Allegiance.ResolveRewardBeneficiary's own doc comment.
        if (target is Monster && Allegiance.ResolveRewardBeneficiary(owner) is Player ownerPlayer)
        {
            ownerPlayer.AdventureRecord.DamageDealt += actual;
        }

        // Pet and Companion System: the "combat notification" system a pet's own AI reads to pick
        // its target (see PetAI/Actor.LastAttacker/LastCombatTarget's own doc comments) -- every
        // damage source in the game already funnels through this one method, so stamping both
        // directions here covers melee/ranged/thrown/spell/skill/DoT damage for free, with no
        // separate instrumentation needed at any individual attack site.
        if (owner != null && owner.IsAlive)
        {
            target.LastAttacker = owner;
            owner.LastCombatTarget = target;
        }

        return actual;
    }
}
