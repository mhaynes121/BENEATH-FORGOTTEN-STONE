namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

/// <summary>
/// Detect Traps (Thief skill / Mage spell): opens a timed window during which GameLoop rolls a
/// stat-scaled chance, once per move, to reveal any trap within GameLoop's TrapRevealRadius --
/// see GameLoop.RollDetectTraps. The window being open (this effect having fired) and a given
/// roll actually finding something are two different states, exactly as the design calls for.
/// Same self-targeted "set an UntilTurn field, checked elsewhere" shape as PoisonWeaponEffect.
/// </summary>
public class DetectTrapsEffect : SpellEffect
{
    public int Duration { get; }

    /// <summary>Which stat powers the per-turn roll while this window is open -- Agility for the Thief skill, Knowledge for the Mage spell (see GameLoop.DetectTrapsChanceForStat).</summary>
    public PrimaryAttribute Stat { get; }

    public DetectTrapsEffect(int duration, PrimaryAttribute stat)
    {
        Duration = duration;
        Stat = stat;
    }

    public override void Apply(SpellCastingContext context, SpellCastResult result)
    {
        if (context.Caster is Player player)
        {
            player.DetectTrapsUntilTurn = context.TurnNumber + context.RankScaling.ScaleDuration(Duration);
            player.DetectTrapsStat = Stat;
        }
    }
}
