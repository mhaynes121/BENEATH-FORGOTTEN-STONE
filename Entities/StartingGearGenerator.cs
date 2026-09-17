using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Rolls and equips a random level-1 weapon and a random level-1 armor
/// piece for a freshly created character -- class/race appropriate because
/// it filters through the exact same validators every other equip path
/// uses (ItemRequirementValidator for level/class/race, and
/// EquipmentCompatibility.IsAllowedForClass for the class-based weapon-type/
/// armor-weight/shield rules), so a Mage can never roll a Battle Axe here
/// any more than they could equip one later. Called once, after the player
/// has already accepted the character (see CharacterCreationScreen) so the
/// result stays a surprise rather than something rolled/rerolled during
/// creation.
/// </summary>
public static class StartingGearGenerator
{
    public static (Item Weapon, Item Armor, Item RangedCompanion) EquipStartingGear(Player player, Random rng)
    {
        // Capped by weight too -- a very low-STR roll has a small MaxCapacity (see
        // EncumbranceCalculator), and without this a starting grant could hand out combined
        // gear heavier than the character can actually carry before ever taking a turn.
        double maxCapacity = EncumbranceCalculator.MaxCapacity(player);

        var weapon = PickRandom(player, rng, IsEligibleWeapon, maxCapacity);
        var armor = PickRandom(player, rng, i => i.Type == ItemType.Armor, maxCapacity - (weapon?.Weight ?? 0));

        Equip(player, weapon);
        Equip(player, armor);

        double remainingCapacity = maxCapacity - (weapon?.Weight ?? 0) - (armor?.Weight ?? 0);
        var companion = EquipMatchingAmmoOrLauncher(player, weapon, rng, remainingCapacity);

        return (weapon, armor, companion);
    }

    /// <summary>
    /// A launcher (Bow/Crossbow/Sling) rolled as the starting weapon is useless without a
    /// matching ammo grant, and (symmetrically, though no current catalog entry can actually
    /// trigger it -- see IsEligibleWeapon) ammo that itself requires a launcher would be
    /// equally useless alone. Either half landing on its own at character creation would be a
    /// dead weapon slot for the entire game unless the player happens to buy/find the other
    /// half, so both are guaranteed together instead of left to chance.
    /// </summary>
    private static Item EquipMatchingAmmoOrLauncher(Player player, Item weapon, Random rng, double maxWeight)
    {
        if (weapon == null)
        {
            return null;
        }

        Item companion = null;
        if (weapon.RequiredAmmunitionType.HasValue)
        {
            companion = PickRandom(player, rng, i => i.AmmunitionType == weapon.RequiredAmmunitionType.Value, maxWeight);
        }
        else if (weapon.AmmunitionType.HasValue && !weapon.CanBeThrown)
        {
            companion = PickRandom(player, rng, i => i.RequiredAmmunitionType == weapon.AmmunitionType.Value, maxWeight);
        }

        Equip(player, companion);
        return companion;
    }

    private static bool IsEligibleWeapon(Item item) =>
        item.Type == ItemType.Weapon || item.Type == ItemType.Wand;

    private static Item PickRandom(Player player, Random rng, Func<Item, bool> typeFilter, double maxWeight)
    {
        var candidates = Items.All
            .Where(typeFilter)
            .Where(i => i.MinimumLevel <= 1)
            .Where(i => i.Weight <= maxWeight)
            // A fresh character shouldn't open the game already stuck with a curse they
            // never chose to risk -- cursed gear should only ever come from something the
            // player found and equipped themselves (see CursedItemBlocksUnequip).
            .Where(i => !i.IsCursed)
            .Where(i => ItemRequirementValidator.CanUse(player, i, out _))
            // NOT redundant with CanUse above, despite checking the same underlying rule for
            // most items: CanUse treats a CastsSpell item (a Wand of Fire) as always usable
            // via its own charges regardless of class, since that's genuinely true for
            // reading it from inventory -- but Equip specifically (which is what this method
            // is picking a candidate FOR) still needs the class's real weapon-type permission,
            // or a Warrior could spawn already equipped with a Fireball-casting wand.
            .Where(i => EquipmentCompatibility.IsAllowedForClass(player.Class, i, out _))
            .ToList();

        return candidates.Count == 0 ? null : candidates[rng.Next(candidates.Count)];
    }

    private static void Equip(Player player, Item item)
    {
        if (item == null)
        {
            return;
        }

        // Same "don't hand out the shared catalog reference" rule as DungeonGenerator/SaveManager/LootGenerator.
        var instance = item.RequiresUniqueInstance ? item.Clone() : item;

        // Starting gear is a known quantity, not a mystery find -- a fresh character should
        // never open their inventory to see "Unidentified Ring" for something they were
        // handed at creation. Only meaningful when the clone above actually happened.
        instance.IsIdentified = true;

        var slot = player.Equipment.FindAutoEquipSlot(instance);
        if (slot == null)
        {
            return;
        }

        player.EquipInSlot(slot.Value, instance);
    }
}
