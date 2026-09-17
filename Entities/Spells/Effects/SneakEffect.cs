namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

/// <summary>Sneak: a self-targeted toggle-on, not a timed buff -- see Player.IsSneaking. Broken by attacking (GameLoop.HandleMove) or once a monster notices anyway (DetectionRules).</summary>
public class SneakEffect : SpellEffect
{
    public override void Apply(SpellCastingContext context, SpellCastResult result)
    {
        if (context.Caster is Player player)
        {
            player.IsSneaking = true;
        }
    }
}
