namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

/// <summary>
/// New Priest Skill Progression (Intercession): opens the protection window -- the actual lethal-
/// damage prevention lives centrally in CombatStatsTracker.ApplyDamage, the single choke point
/// every damage source in the game already routes through (per the proposal's own "avoid placing
/// separate Intercession checks throughout individual damage callers"). This effect only ever
/// flips the flag and stamps the expiry turn; Skill.PreconditionCheck rejects a recast while
/// already active before this ever runs.
/// </summary>
public class IntercessionEffect : SpellEffect
{
    public int BaseDuration { get; }

    public IntercessionEffect(int baseDuration)
    {
        BaseDuration = baseDuration;
    }

    public override void Apply(SpellCastingContext context, SpellCastResult result)
    {
        var player = (Player)context.Caster;
        player.IntercessionActive = true;
        player.IntercessionExpiresOnTurn = context.TurnNumber + context.RankScaling.ScaleDuration(BaseDuration);
    }
}
