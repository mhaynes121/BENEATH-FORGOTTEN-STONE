namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

public class SpellCastResult
{
    public bool Success { get; set; }
    public string FailureReason { get; set; } = "";
    public int ManaConsumed { get; set; }
    public int DamageDealt { get; set; }
    public int HealingDone { get; set; }

    /// <summary>
    /// Set by WeaponDamageEffect from the underlying Actor.PhysicalAttack roll -- null for any
    /// ability with no attack roll at all (a spell, a self-buff, a utility skill), true/false for
    /// a hit/miss. The Ability Proficiency System uses this to decide whether a legitimately-used
    /// physical skill counts as a "successful use" (2 points) or a "failed use" (1 point) -- see
    /// ProficiencyTracker.
    /// </summary>
    public bool? AttackHit { get; set; }

    /// <summary>One entry per actor actually damaged (0-damage hits aren't recorded), each pre-classified by CombatMessages.ClassifySeverity -- lets BuildMessage report severity words instead of raw numbers, same as melee combat, even for AoE spells that hit several targets at once.</summary>
    public List<(Actor Target, HitSeverity Severity)> DamageInstances { get; } = new();

    /// <summary>Same idea as DamageInstances but for healing -- recipients already at full health aren't recorded (nothing was actually restored).</summary>
    public List<(Actor Target, HealSeverity Severity)> HealInstances { get; } = new();

    /// <summary>Human-readable summary; feeds straight into GameLoop.AddStatusMessage instead of a separate event system.</summary>
    public string Message { get; set; } = "";

    /// <summary>Set by IdentifyEffect -- the Item-targeted spell equivalent of DamageInstances/HealInstances, since an item isn't an Actor and can't share those lists. Read by SpellCaster.BuildMessage.</summary>
    public string IdentifiedItemName { get; set; }

    /// <summary>Set by StatBasedIdentifyEffect when a probabilistic identify roll fails (Thief's Identify skill) -- IdentifiedItemName stays null in that case. Read by SpellCaster/SkillCaster.BuildMessage.</summary>
    public string FailedIdentifyItemName { get; set; }

    /// <summary>
    /// One-off flavor lines appended after the standard damage/heal/identify clauses -- set by
    /// KnockdownEffect/StunEffect/FrightenedEffect for a Steadfast resist or ability-supplied
    /// success/failure flavor (e.g. Tremor's "thrown from its feet"), and by WeaponDamageEffect's
    /// optional hit/miss templates (Exorcism). Empty for any ability that stays silent, the same
    /// way Bash/Trip's own knockdown always has. Read by SpellCaster/SkillCaster.BuildMessage.
    /// </summary>
    public List<string> FlavorMessages { get; } = new();
}
