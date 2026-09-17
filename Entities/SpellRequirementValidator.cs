using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Centralizes class/level eligibility for casting a known spell -- mirrors
/// ItemRequirementValidator. Only applies to spells cast from KnownSpells;
/// item-triggered casts (a wand) bypass this entirely, the same way they
/// already bypass mana/cooldown in SpellCaster.Cast -- the item's own
/// MinimumLevel/RequiredClass (see ItemRequirementValidator) gates those
/// instead. Mana, cooldown, range, and line-of-sight stay inside
/// SpellCaster.Cast, which is shared by monster casters and has no notion
/// of a CharacterClass to check against.
/// </summary>
public static class SpellRequirementValidator
{
    public static bool CanCast(Player player, Spell spell, out string reason)
    {
        if (!player.KnownSpells.Contains(spell))
        {
            reason = $"You don't know {spell.Name}.";
            return false;
        }

        if (!spell.CanBeCastBy(player.Class))
        {
            reason = $"Your class cannot cast {spell.Name}.";
            return false;
        }

        if (player.Level < spell.Level)
        {
            reason = $"You must be level {spell.Level} to cast {spell.Name}.";
            return false;
        }

        reason = "";
        return true;
    }
}
