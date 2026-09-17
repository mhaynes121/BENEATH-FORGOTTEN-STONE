namespace BENEATH_FORGOTTEN_STONE.Entities;

public enum HitSeverity
{
    Minor,
    Middling,
    Moderate,
    Major,
    Critical,
    Mortal
}

/// <summary>Mirrors HitSeverity's tiering but for healing -- Full replaces Mortal as the top tier (fully topping off missing health, or overhealing) since "healing severity" needs its own words, not damage's.</summary>
public enum HealSeverity
{
    Minor,
    Middling,
    Moderate,
    Major,
    Critical,
    Full
}

/// <summary>Outcome of one PhysicalAttack -- built by Actor.PhysicalAttack, turned into player-facing text by CombatMessages.</summary>
public class AttackResult
{
    public Actor Attacker { get; init; }
    public Actor Defender { get; init; }
    public bool Hit { get; init; }
    public int Damage { get; init; }
    public HitSeverity? Severity { get; init; }

    /// <summary>How the attacker delivered this attack -- Actor.AttackType at the moment of the swing (a monster's innate type, or the player's equipped weapon). Drives the verb in CombatMessages.Format.</summary>
    public AttackType AttackType { get; init; }
}

/// <summary>
/// Centralizes hit-severity classification and combat message text so
/// neither lives inside Actor or the code that calls it.
/// </summary>
public static class CombatMessages
{
    /// <summary>Percentage is of PRE-damage health. Lethal damage is always Mortal regardless of percentage.</summary>
    public static HitSeverity ClassifySeverity(int damage, int preDamageHealth)
    {
        if (preDamageHealth <= 0 || damage >= preDamageHealth)
        {
            return HitSeverity.Mortal;
        }

        double percent = (double)damage / preDamageHealth;
        if (percent <= 0.20) return HitSeverity.Minor;
        if (percent <= 0.40) return HitSeverity.Middling;
        if (percent <= 0.60) return HitSeverity.Moderate;
        if (percent <= 0.80) return HitSeverity.Major;
        return HitSeverity.Critical;
    }

    /// <summary>Public so spell-damage messages (SpellCaster.BuildMessage) can reuse the same wording as melee combat instead of duplicating it.</summary>
    public static string SeverityWord(HitSeverity severity) => severity switch
    {
        HitSeverity.Minor => "minor",
        HitSeverity.Middling => "middling",
        HitSeverity.Moderate => "moderate",
        HitSeverity.Major => "major",
        HitSeverity.Critical => "critical",
        HitSeverity.Mortal => "mortal",
        _ => ""
    };

    /// <summary>Percentage is of the recipient's MISSING health (Max - pre-heal Current) -- a heal that tops off or exceeds what was missing is always Full regardless of percentage, mirroring how lethal damage is always Mortal.</summary>
    public static HealSeverity ClassifyHealSeverity(int healed, int missingHealth)
    {
        if (missingHealth <= 0 || healed >= missingHealth)
        {
            return HealSeverity.Full;
        }

        double percent = (double)healed / missingHealth;
        if (percent <= 0.20) return HealSeverity.Minor;
        if (percent <= 0.40) return HealSeverity.Middling;
        if (percent <= 0.60) return HealSeverity.Moderate;
        if (percent <= 0.80) return HealSeverity.Major;
        return HealSeverity.Critical;
    }

    /// <summary>"an orc" / "a goblin" -- naive vowel-letter check, good enough for this game's all-lowercase monster/object names. Used to build cause-of-death text (Actor.LastDamageSource); the player's own proper-noun Name never goes through this path since a death's "killer" is always something else.</summary>
    public static string WithArticle(string name) =>
        name.Length > 0 && "aeiouAEIOU".IndexOf(name[0]) >= 0 ? $"an {name}" : $"a {name}";

    /// <summary>Actor-aware overload -- a boss's DisplayName is a proper name (e.g. "Lormax Golden Wing") and must never take "a/an", unlike an ordinary monster's common-noun Name. Every call site that builds article text from an actor should use this instead of the plain string overload.</summary>
    public static string WithArticle(Actor actor) =>
        actor.UsesProperNounDisplayName ? actor.DisplayName : WithArticle(actor.DisplayName);

    /// <summary>
    /// Sentence-ready reference to an actor: "You"/"you" for the player, the bare
    /// DisplayName for a boss (a proper name never takes an article), or "The X"/"the X" for
    /// an ordinary monster's common-noun Name. Centralizes what several call sites (combat
    /// messages, AI status lines, spell/skill messages) used to build by hand.
    /// </summary>
    public static string Label(Actor actor, bool capitalized)
    {
        if (actor is Player)
        {
            return capitalized ? "You" : "you";
        }
        if (actor.UsesProperNounDisplayName)
        {
            return actor.DisplayName;
        }
        return capitalized ? $"The {actor.DisplayName}" : $"the {actor.DisplayName}";
    }

    public static string SeverityWord(HealSeverity severity) => severity switch
    {
        HealSeverity.Minor => "minor",
        HealSeverity.Middling => "middling",
        HealSeverity.Moderate => "moderate",
        HealSeverity.Major => "major",
        HealSeverity.Critical => "critical",
        HealSeverity.Full => "full",
        _ => ""
    };

    /// <summary>Base/second-person form -- "You {word} the goblin", and reused as a noun in cause-of-death text ("an orc's {word}"). Centralized here rather than derived by string rules (crush/slash need "-es", not "-s") so a future attack type just adds one switch arm.</summary>
    public static string AttackWord(AttackType attackType) => attackType switch
    {
        AttackType.Bite => "bite",
        AttackType.Sting => "sting",
        AttackType.Hit => "hit",
        AttackType.Crush => "crush",
        AttackType.Slash => "slash",
        AttackType.Pierce => "pierce",
        _ => "hit"
    };

    /// <summary>Third-person form -- "The orc {word} you".</summary>
    public static string AttackVerb(AttackType attackType) => attackType switch
    {
        AttackType.Bite => "bites",
        AttackType.Sting => "stings",
        AttackType.Hit => "hits",
        AttackType.Crush => "crushes",
        AttackType.Slash => "slashes",
        AttackType.Pierce => "pierces",
        _ => "hits"
    };

    /// <summary>
    /// Pet and Companion System: a combat exchange where NEITHER side is the viewing player (a
    /// pet fighting a monster) -- Format's own dual-branch phrasing ("You hit X"/"X hits you")
    /// has no sensible third-person form for this, so both sides go through Label/AttackVerb
    /// directly instead. Deliberately doesn't participate in Format's dark-room identity-masking
    /// (otherPartyVisible) -- that system exists to protect the PLAYER from an ambush spoiling a
    /// monster's name before they can react, a justification that doesn't extend to a skirmish
    /// between two other actors happening in front of them.
    /// </summary>
    public static string FormatThirdParty(AttackResult result)
    {
        if (!result.Hit)
        {
            return $"{Label(result.Attacker, capitalized: true)} misses {Label(result.Defender, capitalized: false)}.";
        }
        string severityWord = SeverityWord(result.Severity ?? HitSeverity.Minor);
        return $"{Label(result.Attacker, capitalized: true)} {AttackVerb(result.AttackType)} {Label(result.Defender, capitalized: false)} for {severityWord} damage.";
    }

    /// <summary>
    /// Pet and Companion System: auto-detects which of Format's two directions applies (or falls
    /// back to FormatThirdParty when neither combatant is the player) -- lets a call site that
    /// might now be resolving a monster-vs-pet fight (ChaseAI, ProjectileEngine) build the right
    /// message without duplicating the "does this involve the player" check itself.
    /// </summary>
    public static string FormatAttack(AttackResult result, bool otherPartyVisible = true)
    {
        if (result.Attacker is Player)
        {
            return Format(result, attackerIsPlayer: true, otherPartyVisible);
        }
        if (result.Defender is Player)
        {
            return Format(result, attackerIsPlayer: false, otherPartyVisible);
        }
        return FormatThirdParty(result);
    }

    /// <summary>
    /// Both directions report severity, not the raw damage number --
    /// "You pierce the X for minor damage." / "The X stings you for minor
    /// damage." -- so neither side leaks exact numbers the player would
    /// otherwise use to min-max instead of reading the fiction. The verb
    /// itself comes from the attacker's AttackType (a monster's innate
    /// type, or the player's equipped weapon), never from checking names.
    /// </summary>
    /// <param name="otherPartyVisible">
    /// Whether the non-player combatant (the monster, whichever side of the swing it's on) is
    /// currently visible to the player -- false substitutes "something"/"the darkness" for its
    /// name, so an unilluminated dark-room monster's attack never accidentally identifies it
    /// (design spec section 15). Defaults true, so every existing call site is unaffected; only
    /// ChaseAI's monster-attacks-player path currently passes anything else.
    /// </param>
    public static string Format(AttackResult result, bool attackerIsPlayer, bool otherPartyVisible = true)
    {
        if (!result.Hit)
        {
            if (attackerIsPlayer)
            {
                return otherPartyVisible
                    ? $"You miss {Label(result.Defender, capitalized: false)}."
                    : "You swing at something in the darkness and miss.";
            }
            return otherPartyVisible
                ? $"{Label(result.Attacker, capitalized: true)} misses you."
                : "Something misses you in the darkness.";
        }

        string severityWord = SeverityWord(result.Severity ?? HitSeverity.Minor);
        if (attackerIsPlayer)
        {
            return otherPartyVisible
                ? $"You {AttackWord(result.AttackType)} {Label(result.Defender, capitalized: false)} for {severityWord} damage."
                : $"You {AttackWord(result.AttackType)} something in the darkness for {severityWord} damage.";
        }
        return otherPartyVisible
            ? $"{Label(result.Attacker, capitalized: true)} {AttackVerb(result.AttackType)} you for {severityWord} damage."
            : $"Something strikes you from the darkness for {severityWord} damage.";
    }
}
