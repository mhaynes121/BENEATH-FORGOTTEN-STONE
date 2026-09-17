namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

/// <summary>
/// New Priest Skill Progression (Purify): removes every currently-active condition explicitly
/// marked CanBePurified in one use -- every hostile DoT/status (StatusEffect always sets this)
/// and every hostile secondary stat debuff (StatModifierEffect sets it equal to its own
/// IsHostileSecondaryEffect flag), plus the two Actor-field conditions that aren't ActiveEffect-
/// backed at all (Silenced, Frightened). Deliberately does NOT touch Stunned or Prone -- neither
/// is purifiable per the proposal ("Prone requires the Stand command. A Stunned Priest cannot act
/// to use Purify.", the latter already true by construction since GameLoop's stun check blocks
/// the player's whole turn before any command, Purify included, is ever read).
/// </summary>
public class PurifyEffect : SpellEffect
{
    public override void Apply(SpellCastingContext context, SpellCastResult result)
    {
        var caster = context.Caster;

        // Same revert-the-stat-delta step EffectProcessor.Tick already performs on natural
        // expiry -- Purify is just an early, on-demand expiry for anything CanBePurified.
        var purifiable = caster.ActiveEffects.Where(e => e.CanBePurified).ToList();
        foreach (var effect in purifiable)
        {
            if (effect.ModifiedStat.HasValue)
            {
                StatModifierEffect.ApplyStatDelta(caster, effect.ModifiedStat.Value, -effect.StatAmount);
            }
            caster.ActiveEffects.Remove(effect);
        }

        if (context.TurnNumber < caster.SilencedUntilTurn)
        {
            caster.SilencedUntilTurn = context.TurnNumber;
        }
        if (context.TurnNumber < caster.FrightenedUntilTurn)
        {
            caster.FrightenedUntilTurn = context.TurnNumber;
        }
    }
}
