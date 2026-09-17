using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>Centralizes item eligibility so it's enforced once (at InventoryScreen's equip/use choke point), not re-checked per screen.</summary>
public static class ItemRequirementValidator
{
    public static bool CanUse(Player player, Item item, out string reason)
    {
        if (player.Level < item.MinimumLevel)
        {
            reason = $"You must be level {item.MinimumLevel} to use the {item.DisplayName}.";
            return false;
        }

        if (item.RequiredClass != null && item.RequiredClass != player.Class)
        {
            reason = $"Only a {item.RequiredClass.Name} can use the {item.DisplayName}.";
            return false;
        }

        if (item.RequiredRace != null && item.RequiredRace != player.Race)
        {
            reason = $"Only a {item.RequiredRace.Name} can use the {item.DisplayName}.";
            return false;
        }

        // A spellbook/scroll is dead weight to a class that could never learn what it teaches
        // (e.g. a Priest-only Spellbook of Smite in a Warrior's hands) -- SpellRequirementValidator
        // only ever runs this check once the player already has it, so the Trader buy screen
        // needs its own look here to color it correctly before that purchase happens.
        if (item.TeachesSpell != null && !item.TeachesSpell.CanBeCastBy(player.Class))
        {
            reason = $"Your class cannot learn {item.TeachesSpell.Name}.";
            return false;
        }

        // A potion that ONLY restores mana (no HealthRestore, no CarryCapacityBonus) is dead
        // weight to a class with no mana pool at all (Warrior/Thief, BaseMana 0) -- Restore
        // clamps to Max, so it would silently do nothing. A dual-purpose potion (health+mana)
        // still counts as usable via its health side.
        if (item.Type == ItemType.Consumable && item.ManaRestore > 0 && item.HealthRestore == 0
            && item.CarryCapacityBonus == 0 && player.Mana.Max == 0)
        {
            reason = $"You have no mana pool -- the {item.DisplayName} would do nothing for you.";
            return false;
        }

        // An item-triggered cast (a Wand of Fire) is usable from inventory via its own Charges
        // regardless of whether this class could ever equip it -- see SpellRequirementValidator's
        // own doc comment and GameLoop.HandleCastSpell's wand branch, neither of which check
        // class/equip eligibility for a CastsSpell item at all. Skipping straight past the
        // equip check below for these avoids a false "unusable" (e.g. a Warrior genuinely CAN
        // fire a Wand of Fire's charge even though they could never wear the wand itself) --
        // a plain stat-bonus wand with no CastsSpell (most of them) still needs the equip check,
        // since standing there holding it is the only thing it ever does.
        if (item.CastsSpell != null)
        {
            reason = "";
            return true;
        }

        // Equip eligibility (weapon type / armor weight / shield) and throw eligibility
        // (ThrowableCategory) are two INDEPENDENT avenues to actually using an item -- a class
        // can be trained to throw something it could never wield in melee (a Thief with a
        // Throwing Axe) and vice versa. Each defaults to "no objection" when the corresponding
        // concept doesn't apply to this item at all (a Ring has no WeaponType; a Sword isn't
        // CanBeThrown), so a plain non-equipment, non-throwable item (a potion, a scroll)
        // sails through both checks unrestricted, same as before this method existed.
        bool equipApplies = item.EquipmentType != EquipmentType.None;
        bool throwApplies = item.CanBeThrown && item.ThrowableCategory.HasValue;
        string equipReason = "";
        bool equipOk = !equipApplies || EquipmentCompatibility.IsAllowedForClass(player.Class, item, out equipReason);
        bool throwOk = !throwApplies || player.Class.AllowedThrowableCategories.Contains(item.ThrowableCategory.Value);

        bool usable = equipApplies && throwApplies ? equipOk || throwOk : equipOk && throwOk;
        if (!usable)
        {
            reason = equipApplies && throwApplies
                ? $"Your class can neither wield nor throw the {item.DisplayName}."
                : throwApplies
                    ? $"Your class has no training to throw the {item.DisplayName}."
                    : equipReason;
            return false;
        }

        reason = "";
        return true;
    }
}
