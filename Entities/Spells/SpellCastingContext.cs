using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities.Components;
using BENEATH_FORGOTTEN_STONE.Entities.Proficiency;

namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

/// <summary>
/// One specific attempt to cast a spell. Never stored on Spell -- a Spell
/// definition is a reusable, stateless template shared by every caster
/// (player or monster) who knows it or holds an item that casts it.
/// </summary>
public class SpellCastingContext
{
    public Actor Caster { get; }
    public Level Level { get; }
    public int TurnNumber { get; }
    public Random Rng { get; }

    /// <summary>Set by SpellCaster.Cast (= spell.Name) or SkillCaster.Cast (= skill.Name) before effects run -- lets an effect (e.g. DamageEffect, StatModifierEffect) build cause-of-death/active-effect text without needing a reference back to whichever caster-type owns it. A plain string rather than the Spell object itself so shared effects stay agnostic between Spell and the physical Skill system.</summary>
    public string CastName { get; set; }

    /// <summary>Set by the caller before Cast() for SingleTarget spells (resolved via aim ray for the player, or directly for AI).</summary>
    public Actor TargetActor { get; set; }

    /// <summary>Set by the caller before Cast() for Tile spells.</summary>
    public (int X, int Y)? TargetTile { get; set; }

    /// <summary>Set by the caller before Cast() for Item spells (e.g. Identify) -- chosen from the caster's own inventory/equipment, never resolved by TargetResolver since it isn't an Actor or a tile.</summary>
    public Item TargetItem { get; set; }

    /// <summary>Resolved by SpellCaster from TargetType/TargetActor/TargetTile before effects run.</summary>
    public List<Actor> AffectedActors { get; set; } = new();

    /// <summary>
    /// Ability Proficiency System: the caster's current rank-derived scaling for whichever
    /// ability is being cast right now, set by GameLoop before calling SkillCaster.Cast/
    /// SpellCaster.Cast. Defaults to RankScaling.Neutral (every multiplier a no-op) so any
    /// existing or future caller that never sets this -- monster spellcasting, in particular --
    /// is completely unaffected and keeps producing today's exact numbers.
    /// </summary>
    public RankScaling RankScaling { get; set; } = RankScaling.Neutral;

    public SpellCastingContext(Actor caster, Level level, int turnNumber, Random rng)
    {
        Caster = caster;
        Level = level;
        TurnNumber = turnNumber;
        Rng = rng;
    }
}
