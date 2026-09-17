using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Centralizes which EquipmentSlots an item's EquipmentType can occupy,
/// so this rule lives in exactly one place instead of being duplicated
/// across inventory UI, equip flow, and item classes. Adding a new slot
/// later (e.g. Belt) is one new enum value plus one new dictionary entry.
/// </summary>
public static class EquipmentCompatibility
{
    private static readonly Dictionary<EquipmentType, EquipmentSlot[]> CompatibleSlotsByType = new()
    {
        [EquipmentType.Hand] = new[] { EquipmentSlot.PrimaryHand, EquipmentSlot.OffHand },
        [EquipmentType.Ring] = new[] { EquipmentSlot.PrimaryRing, EquipmentSlot.OffHandRing },
        [EquipmentType.Gloves] = new[] { EquipmentSlot.Gloves },
        [EquipmentType.Wrists] = new[] { EquipmentSlot.Wrists },
        [EquipmentType.Arms] = new[] { EquipmentSlot.Arms },
        [EquipmentType.Body] = new[] { EquipmentSlot.Body },
        [EquipmentType.Neck] = new[] { EquipmentSlot.Neck },
        [EquipmentType.Head] = new[] { EquipmentSlot.Head },
        [EquipmentType.Legs] = new[] { EquipmentSlot.Legs },
        [EquipmentType.Feet] = new[] { EquipmentSlot.Feet },
        [EquipmentType.Ranged] = new[] { EquipmentSlot.RangedWeapon },
        [EquipmentType.AmmoThrown] = new[] { EquipmentSlot.Ammunition }
    };

    /// <summary>A hybrid item's slot list swaps order depending on how it's normally carried: a stackable one (Short Spear) prefers Ammo/Thrown first, an individually-carried one (Long Spear) prefers a hand first -- see the Offer to Equip design's own "prefer Ammo/Thrown for a stack, prefer a hand slot for an individual item" rule (spec section 11), applied here so it's consistent everywhere a hybrid gets auto-equipped, not just the pickup offer.</summary>
    private static readonly EquipmentSlot[] HandSlotsThenAmmo = { EquipmentSlot.PrimaryHand, EquipmentSlot.OffHand, EquipmentSlot.Ammunition };
    private static readonly EquipmentSlot[] AmmoThenHandSlots = { EquipmentSlot.Ammunition, EquipmentSlot.PrimaryHand, EquipmentSlot.OffHand };

    public static IReadOnlyList<EquipmentSlot> GetCompatibleSlots(EquipmentType type) =>
        CompatibleSlotsByType.TryGetValue(type, out var slots) ? slots : Array.Empty<EquipmentSlot>();

    /// <summary>The Item overload -- identical to the EquipmentType one for every ordinary item, except a ThrowableCapability.MeleeOrProjectile hybrid (a spear), whose compatible list is the hand slots plus Ammunition, ordered by whether it's normally carried as a stack (ItemStacking.IsStackable) or individually.</summary>
    public static IReadOnlyList<EquipmentSlot> GetCompatibleSlots(Item item)
    {
        if (item.ThrowableCapability == ThrowableCapability.MeleeOrProjectile)
        {
            return ItemStacking.IsStackable(item) ? AmmoThenHandSlots : HandSlotsThenAmmo;
        }
        return GetCompatibleSlots(item.EquipmentType);
    }

    public static bool IsCompatible(Item item, EquipmentSlot slot) => GetCompatibleSlots(item).Contains(slot);

    /// <summary>
    /// Combination-compatibility rules (distinct from slot compatibility
    /// above -- "can this occupy the slot" vs. "can this coexist with
    /// what's already equipped"). Declaring a conflict one-directionally
    /// is enough; ConflictsWith checks both directions, so a future
    /// asymmetric rule (e.g. a GreatShield conflicting with both Weapon
    /// and Shield) only needs one entry.
    /// </summary>
    private static readonly Dictionary<EquipmentCategory, HashSet<EquipmentCategory>> ConflictingCategories = new()
    {
        [EquipmentCategory.Weapon] = new HashSet<EquipmentCategory> { EquipmentCategory.Weapon },
        [EquipmentCategory.Shield] = new HashSet<EquipmentCategory> { EquipmentCategory.Shield },
        [EquipmentCategory.Wand] = new HashSet<EquipmentCategory> { EquipmentCategory.Wand }
    };

    public static bool ConflictsWith(EquipmentCategory a, EquipmentCategory b)
    {
        if (a == EquipmentCategory.None || b == EquipmentCategory.None)
        {
            return false;
        }
        return (ConflictingCategories.TryGetValue(a, out var setA) && setA.Contains(b))
            || (ConflictingCategories.TryGetValue(b, out var setB) && setB.Contains(a));
    }

    private static string CategoryNoun(EquipmentCategory category) => category switch
    {
        EquipmentCategory.Weapon => "weapon",
        EquipmentCategory.Shield => "shield",
        EquipmentCategory.Wand => "wand",
        _ => "item"
    };

    /// <summary>
    /// Class-based equipment restriction: does this class's weapon-type/
    /// weapon-size/armor-weight/shield rules permit this item at all, independent of
    /// level/race (ItemRequirementValidator) and slot/conflict rules. An
    /// item's own explicit RequiredClass (if set) is a separate, stricter
    /// override layered on top of this -- a class-appropriate weapon type
    /// can still be reserved for one specific class via that field (e.g.
    /// the Mace is Blunt, which both Warrior and Priest allow, but its
    /// RequiredClass further narrows it to Priest alone).
    /// </summary>
    public static bool IsAllowedForClass(CharacterClass characterClass, Item item, out string reason)
    {
        if (item.EquipmentCategory == EquipmentCategory.Shield && !characterClass.AllowedShieldSizes.Contains(item.Size))
        {
            reason = characterClass.AllowedShieldSizes.Count == 0
                ? $"{characterClass.Name}s cannot use shields."
                : $"{characterClass.Name}s cannot use a {SizeNoun(item.Size)} shield.";
            return false;
        }

        if (item.WeaponType.HasValue && !characterClass.AllowedWeaponTypes.Contains(item.WeaponType.Value))
        {
            reason = $"{characterClass.Name}s cannot use a {WeaponTypeNoun(item.WeaponType.Value)}.";
            return false;
        }

        if (item.EquipmentCategory == EquipmentCategory.Weapon && !characterClass.AllowedWeaponSizes.Contains(item.Size))
        {
            reason = $"{characterClass.Name}s cannot use a {SizeNoun(item.Size)} weapon.";
            return false;
        }

        if (item.ArmorWeight.HasValue && !characterClass.AllowedArmorWeights.Contains(item.ArmorWeight.Value))
        {
            reason = $"{characterClass.Name}s cannot wear {ArmorWeightNoun(item.ArmorWeight.Value)} armor.";
            return false;
        }

        reason = "";
        return true;
    }

    private static string WeaponTypeNoun(WeaponType type) => type switch
    {
        WeaponType.Dagger => "dagger",
        WeaponType.Sword => "sword",
        WeaponType.Axe => "axe",
        WeaponType.Blunt => "blunt weapon",
        WeaponType.Wand => "wand",
        _ => "weapon"
    };

    private static string ArmorWeightNoun(ArmorWeight weight) => weight switch
    {
        ArmorWeight.Light => "light",
        ArmorWeight.Medium => "medium",
        ArmorWeight.Heavy => "heavy",
        _ => ""
    };

    private static string SizeNoun(Size size) => size switch
    {
        Size.Small => "small",
        Size.Medium => "medium",
        Size.Large => "large",
        _ => ""
    };

    /// <summary>
    /// Agility penalty for wearing armor heavier than Light -- Thief-only.
    /// Warrior and Priest can wear Medium/Heavy without any cost; Thief is
    /// the one class for whom stepping past Light (into the Medium it's
    /// allowed) trades agility for the extra protection. Applied
    /// automatically by Player.ApplyStatModifiers/RemoveStatModifiers, and
    /// public so the inventory examine screen can preview it before the
    /// item is worn. Per-item and stacks across slots.
    /// </summary>
    public static int AgilityPenaltyForWeight(CharacterClass characterClass, ArmorWeight? weight)
    {
        if (characterClass != CharacterClass.Thief)
        {
            return 0;
        }

        return weight switch
        {
            ArmorWeight.Medium => 1,
            ArmorWeight.Heavy => 2,
            _ => 0
        };
    }

    /// <summary>
    /// The single centralized gate for equipping: slot compatibility, item
    /// requirements (level/class/race), class-based weapon/armor/shield
    /// restrictions, and combination conflicts against everything else
    /// currently equipped. Callers must not mutate equipment/inventory
    /// state unless this returns true -- nothing here has side effects, so
    /// a failed check leaves everything untouched.
    /// </summary>
    /// <param name="targetSlot">The slot being equipped into -- its current occupant (if any) is being replaced, so it's excluded from the conflict check.</param>
    public static bool CanEquip(Player player, Item item, EquipmentSlot targetSlot, out string reason)
    {
        if (!IsCompatible(item, targetSlot))
        {
            reason = $"{item.DisplayName} cannot be equipped in {SlotLabel(targetSlot)}.";
            return false;
        }

        if (!ItemRequirementValidator.CanUse(player, item, out reason))
        {
            return false;
        }

        if (!IsAllowedForClass(player.Class, item, out reason))
        {
            return false;
        }

        // RangedWeapon/Ammunition are physically separate from the hand slots (a bow slung on
        // the back, a quiver at the hip) -- they never compete with a held weapon/shield/wand for
        // the same hand space, so the Weapon/Shield/Wand self-conflict rule below is skipped
        // entirely whenever either side of the comparison is one of these two slots. Without
        // this, equipping a Bow while a melee weapon is held would incorrectly trip the
        // "two Weapons" conflict.
        bool IsRangedOrAmmoSlot(EquipmentSlot slot) => slot is EquipmentSlot.RangedWeapon or EquipmentSlot.Ammunition;

        foreach (var kvp in player.Equipment.AllEquipped)
        {
            if (kvp.Key == targetSlot || IsRangedOrAmmoSlot(targetSlot) || IsRangedOrAmmoSlot(kvp.Key))
            {
                continue;
            }

            var existingItem = kvp.Value;

            // Dual Wield lifts the one-weapon-at-a-time restriction specifically -- Shield
            // and Wand still self-conflict for everyone, so this only skips the Weapon case.
            bool bothWeapons = item.EquipmentCategory == EquipmentCategory.Weapon && existingItem.EquipmentCategory == EquipmentCategory.Weapon;
            if (bothWeapons && player.HasSkill(SkillCatalog.DualWield))
            {
                continue;
            }

            if (ConflictsWith(item.EquipmentCategory, existingItem.EquipmentCategory))
            {
                reason = item.EquipmentCategory == existingItem.EquipmentCategory
                    ? $"You cannot equip two {CategoryNoun(item.EquipmentCategory)}s."
                    : $"{item.DisplayName} cannot be equipped alongside your {existingItem.DisplayName}.";
                return false;
            }
        }

        reason = "";
        return true;
    }

    /// <summary>
    /// The empty slot to offer for an "equip on pickup" prompt (see GameLoop.OfferEquipAfterPickup)
    /// -- deliberately distinct from EquipmentComponent.FindAutoEquipSlot, which returns a
    /// single-slot type's slot even when it's already occupied (that method's caller, the manual
    /// inventory equip flow, treats "occupied" as an implicit replace). Here, occupied always
    /// means ineligible, for every slot shape: examines every compatible slot in preference order
    /// (primary before secondary, e.g. PrimaryHand before OffHand), skips any that already hold
    /// something, and returns the first empty one CanEquip actually approves -- never just the
    /// first empty one, since an empty slot can still be invalid (a conflicting second weapon
    /// without Dual Wield, a class/level/race restriction, etc.). Returns null for non-equipment
    /// or when no empty compatible slot both exists and passes CanEquip.
    /// </summary>
    public static EquipmentSlot? FindEligibleEmptyEquipSlot(Player player, Item item)
    {
        foreach (var slot in GetCompatibleSlots(item))
        {
            if (player.Equipment.Get(slot) != null)
            {
                continue;
            }
            if (CanEquip(player, item, slot, out _))
            {
                return slot;
            }
        }
        return null;
    }

    public static string SlotLabel(EquipmentSlot slot) => slot switch
    {
        EquipmentSlot.PrimaryHand => "Primary Hand",
        EquipmentSlot.OffHand => "Off-hand",
        EquipmentSlot.PrimaryRing => "Primary Ring",
        EquipmentSlot.OffHandRing => "Off-hand Ring",
        EquipmentSlot.Gloves => "Gloves",
        EquipmentSlot.Wrists => "Wrists",
        EquipmentSlot.Arms => "Arms",
        EquipmentSlot.Body => "Body",
        EquipmentSlot.Neck => "Neck",
        EquipmentSlot.Head => "Head",
        EquipmentSlot.Legs => "Legs",
        EquipmentSlot.Feet => "Feet",
        EquipmentSlot.RangedWeapon => "Ranged Weapon",
        EquipmentSlot.Ammunition => "Ammo/Thrown",
        _ => slot.ToString()
    };
}
