namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

/// <summary>
/// Identifies context.TargetItem (set by GameLoop.HandleCastSpell's item-picker branch before
/// Cast() runs) -- the Item-targeted equivalent of DamageEffect/HealEffect acting on
/// context.AffectedActors, since an item isn't an Actor. Sets SpellCastResult.IdentifiedItemName
/// rather than Message directly, since SpellCaster.Cast overwrites Message with BuildMessage's
/// output right after every effect runs.
/// </summary>
public class IdentifyEffect : SpellEffect
{
    public override void Apply(SpellCastingContext context, SpellCastResult result)
    {
        if (context.TargetItem == null)
        {
            return;
        }

        context.TargetItem.IsIdentified = true;
        result.IdentifiedItemName = context.TargetItem.Name;
    }
}
