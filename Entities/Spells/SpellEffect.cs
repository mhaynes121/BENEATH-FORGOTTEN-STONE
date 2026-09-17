namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

/// <summary>
/// One independently-responsible piece of a spell's behavior. A Spell
/// carries a list of these instead of being a monolithic class with a
/// field per possible behavior -- adding a new effect later is a new
/// file, not a change to Spell or SpellCaster.
/// </summary>
public abstract class SpellEffect
{
    public abstract void Apply(SpellCastingContext context, SpellCastResult result);
}
