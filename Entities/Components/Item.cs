using BENEATH_FORGOTTEN_STONE.Entities;
using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Entities.Components;

/// <summary>
/// An item template (a potion, a weapon, a suit of armor, ...). Instances
/// are immutable and shared -- picking one up adds the same reference to
/// the finder's inventory, mirroring how CharacterClass/Race are shared
/// templates rather than per-instance state. The one exception is
/// Charges: a charged item (e.g. a wand) needs its own independent count,
/// so the dungeon generator clones charged items at spawn time instead of
/// handing out the shared catalog reference -- see Clone() and
/// DungeonGenerator.SpawnItems.
/// </summary>
public class Item : IInspectable
{
    public string Name { get; }
    public char Symbol { get; }
    public string Description { get; }
    public ItemType Type { get; }

    /// <summary>IInspectable: aliases the existing Description field rather than duplicating it -- Description already serves as the brief, distant-glance blurb shown throughout the item's history in this codebase.</summary>
    public string ShortDescription => Description;

    /// <summary>Shown by Look when the item is explicitly examined (inventory/equipment/on the ground). Falls back to Description if not given.</summary>
    public string LongDescription { get; }

    /// <summary>What kind of equipment this is, if any -- drives which EquipmentSlot(s) it can occupy. See EquipmentCompatibility.</summary>
    public EquipmentType EquipmentType { get; }

    /// <summary>Finer classification used only for combination conflicts (e.g. can't wear two weapons) -- None means no conflict rules apply. See EquipmentCompatibility.CanEquip.</summary>
    public EquipmentCategory EquipmentCategory { get; }

    /// <summary>Consumable only: HP restored on use.</summary>
    public int HealthRestore { get; }

    /// <summary>Consumable only: MP restored on use.</summary>
    public int ManaRestore { get; }

    /// <summary>AttackPower bonus while equipped.</summary>
    public int PhysicalAttackBonus { get; }

    public int MagicalAttackBonus { get; }

    /// <summary>DefensePower bonus while equipped.</summary>
    public int DefenseBonus { get; }

    /// <summary>Primary-attribute bonuses (e.g. a Ring of Strength) applied to StatBlock.EquipmentModifier while equipped, removed on unequip. Empty for items with none.</summary>
    public IReadOnlyDictionary<PrimaryAttribute, int> StatModifiers { get; }

    /// <summary>Non-null for items that cast a spell when used (e.g. a wand). Bypasses the wielder's mana/cooldown -- limited by Charges instead.</summary>
    public Spell CastsSpell { get; }

    /// <summary>Non-null for a spell scroll or spellbook: reading it (the Learn Spell command) permanently adds this spell to KnownSpells and consumes the item, rather than casting it. See SpellScrollCatalog and SpellbookCatalog.</summary>
    public Spell TeachesSpell { get; }

    /// <summary>Remaining uses. Null means the item isn't charge-limited. Used both by a CastsSpell item (a wand) and by a set of Lockpicks (ItemType.Lockpick) -- see GameLoop's lock-picking flow, which decrements this only when a failed attempt happens to snap a pick.</summary>
    public int? Charges { get; set; }

    /// <summary>0 = no level requirement.</summary>
    public int MinimumLevel { get; }

    /// <summary>Null = any class may use it.</summary>
    public CharacterClass RequiredClass { get; }

    /// <summary>Null = any race may use it.</summary>
    public Race RequiredRace { get; }

    /// <summary>
    /// Sale/loot value -- drives trader buy/sell pricing (see ItemPricingCalculator). Defaults
    /// to a value computed from this item's own Type/MinimumLevel/bonuses/magical
    /// properties/current Charges (see ComputeDefaultGoldValue) rather than being hand-authored
    /// per catalog entry; pass a positive goldValue explicitly to override for a special/unique
    /// item. Recomputed from the *current* Charges on every access rather than frozen at
    /// construction, so a stack that grows or shrinks after spawning (see ItemStacking) is
    /// always priced for what it actually holds right now, not what it started with.
    /// </summary>
    public int GoldValue => explicitGoldValue > 0
        ? explicitGoldValue
        : ComputeDefaultGoldValue(Type, MinimumLevel, PhysicalAttackBonus, MagicalAttackBonus, DefenseBonus,
            StatModifiers, IsBlessed, IsCursed, StatusEffects, Charges, ResistanceModifiers);

    private readonly int explicitGoldValue;

    /// <summary>Physical size tier. A monster can only drop this item if its own Size is >= this. See LootGenerator.</summary>
    public Size Size { get; }

    /// <summary>
    /// Five-tier physical size for the Lost Items system (see ItemLossRules/ItemLossConfig) and
    /// the item-inspection "Size:" display -- deliberately independent of Size above, which keeps
    /// its existing, unrelated jobs (monster-loot eligibility, EquipmentCompatibility's shield/
    /// weapon class-size gating) untouched. LootGenerator bridges the two via
    /// ItemSizeMapping.MinimumMonsterSizeForLoot rather than comparing this directly against
    /// Monster.Size.
    /// </summary>
    public ItemSize ItemSize { get; }

    /// <summary>
    /// Whether this item is even eligible to be randomly lost when dropped or thrown --
    /// independent of (and checked alongside) ItemLossRules' other automatic protections
    /// (IsSkeletonKey, ThrownWeaponBehavior.ReturnsToThrower, GoldValue at or above
    /// ItemLossConfig.ProtectionValueThreshold). Defaults true; set false on a specific catalog
    /// entry to protect a small, cheap item that would otherwise qualify for loss but shouldn't
    /// (a hypothetical unique quest trinket, for instance) -- the same "flag defaults to the
    /// common case, a specific template opts out" shape IsSkeletonKey/IsIdentifyScroll already use.
    /// </summary>
    public bool CanBeLost { get; }

    /// <summary>Whether LootGenerator may ever select this item as a monster drop -- separate from Size, which governs which monsters can drop it, not whether it's eligible at all.</summary>
    public bool CanDropAsLoot { get; }

    /// <summary>Only meaningful for weapons (Crush/Slash/Pierce) -- null for everything else (armor, consumables, wands, rings, ...). Player.AttackType reads this off whatever's equipped in a hand slot, falling back to Hit when unarmed or holding something non-physical (a wand, an empty hand).</summary>
    public AttackType? AttackType { get; }

    /// <summary>Broad weapon category (Dagger/Sword/Axe/Blunt/Wand) for class-equipment restrictions -- null for non-weapons. See CharacterClass.AllowedWeaponTypes, EquipmentCompatibility.IsAllowedForClass.</summary>
    public WeaponType? WeaponType { get; }

    /// <summary>Weight class for worn armor material, for class-equipment restrictions -- null for rings/amulets (jewelry) and everything non-armor. See CharacterClass.AllowedArmorWeights, EquipmentCompatibility.IsAllowedForClass.</summary>
    public ArmorWeight? ArmorWeight { get; }

    /// <summary>Carry weight in lbs, counted whether the item is equipped or just sitting in Inventory.Items -- see EncumbranceCalculator.</summary>
    public double Weight { get; }

    /// <summary>Consumable only: temporary bonus to max carry capacity applied for CarryCapacityBonusDuration turns when drunk -- see InventoryScreen.ApplyItem and EncumbranceCalculator. 0 means this consumable doesn't affect capacity.</summary>
    public int CarryCapacityBonus { get; }

    /// <summary>Turns CarryCapacityBonus lasts once applied. Meaningless when CarryCapacityBonus is 0.</summary>
    public int CarryCapacityBonusDuration { get; }

    /// <summary>ItemType.Key only: consumed to instantly unlock any locked Door, bypassing its IsPickable/IsBashable restrictions entirely -- see GameLoop's door-unlock flow. The fallback for classes/doors where picking or bashing isn't an option.</summary>
    public bool IsSkeletonKey { get; }

    /// <summary>
    /// Whether the player knows this item's hidden properties yet -- stats, StatModifiers,
    /// IsBlessed/IsCursed, and StatusEffects are all concealed from display while false (see
    /// InventoryScreen.FormatItemDetails/DisplayName). The only genuinely per-instance mutable
    /// property on Item; defaults true so every existing item is unaffected, and only items
    /// explicitly constructed with isIdentified: false start hidden. Like Charges, a non-default
    /// value requires cloning at spawn instead of handing out the shared catalog reference --
    /// see Clone() and every "Charges != null" spawn-time clone check.
    /// </summary>
    public bool IsIdentified { get; set; }

    /// <summary>A blessed weapon deals BlessedUndeadDamageBonus extra damage against CreatureType.Undead targets -- see Actor.PhysicalAttack. Hidden while unidentified.</summary>
    public bool IsBlessed { get; }

    /// <summary>An equipped cursed item cannot be unequipped (see InventoryScreen.UnequipFlow) until identified reveals the fact -- the block itself still applies either way, only the message's wording depends on IsIdentified.</summary>
    public bool IsCursed { get; }

    /// <summary>Bonus physical damage against undead when this is an equipped IsBlessed weapon. Meaningless when IsBlessed is false.</summary>
    public int BlessedUndeadDamageBonus { get; }

    /// <summary>On-hit procs (weapons) or passive defensive effects (armor) -- see ItemStatusEffect and ItemEffectApplier. Empty for items with none.</summary>
    public IReadOnlyList<ItemStatusEffect> StatusEffects { get; }

    /// <summary>Passive bonus/penalty to carry capacity while equipped -- added to Actor.CarryCapacityBonus on equip, removed on unequip (see Player.ApplyStatModifiers/Monster.ApplyEquipmentBonus). Distinct from CarryCapacityBonus above, which is a temporary effect from drinking a consumable, not a passive property of gear.</summary>
    public int EncumbranceModifier { get; }

    /// <summary>A direct-use consumable (not a spell-teaching scroll like SpellScrollCatalog's) that identifies one chosen unidentified item and is consumed -- see InventoryScreen.ApplyItem. Mirrors IsSkeletonKey's single-purpose-flag pattern.</summary>
    public bool IsIdentifyScroll { get; }

    /// <summary>ItemType.Ammunition only: which family this ammo belongs to -- see RequiredAmmunitionType, Core/ProjectileEngine.cs. Stock count is Charges, the same stacking mechanism Lockpicks already use.</summary>
    public AmmunitionType? AmmunitionType { get; }

    /// <summary>Bow/Crossbow weapons only: which AmmunitionType this weapon consumes -- see GameLoop.HandleFireProjectile, which matches this against inventory items' own AmmunitionType.</summary>
    public AmmunitionType? RequiredAmmunitionType { get; }

    /// <summary>Whether GameLoop.HandleFireProjectile may throw this item directly from inventory -- independent of whether it's also a melee weapon (a Spear is both). See ThrownWeaponBehavior for what happens to it afterward.</summary>
    public bool CanBeThrown { get; }

    /// <summary>What happens to this item once a throw resolves -- meaningless when CanBeThrown is false. Defaults to DropsAtImpactPoint (see the constructor) so nothing here prevents a future recovery feature.</summary>
    public ThrownWeaponBehavior? ThrownWeaponBehavior { get; }

    /// <summary>How far this travels as a projectile -- meaningful for a Bow/Crossbow (fired) or a CanBeThrown item (thrown); 0 for everything else. See Core/ProjectileEngine.cs.</summary>
    public int ProjectileRange { get; }

    /// <summary>Meaningful only when CanBeThrown: which training tier throwing this requires -- see ThrowableCategory, CharacterClass.AllowedThrowableCategories. Independent of WeaponType/melee equip eligibility.</summary>
    public ThrowableCategory? ThrowableCategory { get; }

    /// <summary>MeleeOnly (the default -- true for every non-hybrid item, weapon or not), ProjectileOnly (ammunition and dedicated throwables), or MeleeOrProjectile (Short/Long Spear) -- see EquipmentCompatibility.GetCompatibleSlots(Item), the only place this is consulted.</summary>
    public ThrowableCapability ThrowableCapability { get; }

    /// <summary>Chance this breaks (removed permanently, never placed, never rolled for loss/concealment) on hitting a creature -- see ItemBreakageRules. 0 for non-projectiles and for projectiles that simply don't break (Rocks, Sling Stones).</summary>
    public double BreakChanceOnCreatureHit { get; }

    /// <summary>Chance this breaks on impacting a wall -- usually the highest of the three break chances. See ItemBreakageRules.</summary>
    public double BreakChanceOnWallImpact { get; }

    /// <summary>Chance this breaks on an ordinary (non-wall, non-creature) landing -- usually the lowest of the three. See ItemBreakageRules.</summary>
    public double BreakChanceOnOrdinaryLanding { get; }

    /// <summary>Resistance bonuses/penalties this item grants while equipped -- see the Resistance System spec, ResistanceCalculator.SumEquipmentResistance, and InventoryScreen.FormatItemDetails. Defaults to ResistanceSet.Zero so every existing item (and any old save's item, resolved by name against this catalog) behaves as "no effect" until explicitly given one.</summary>
    public ResistanceSet ResistanceModifiers { get; }

    /// <summary>
    /// Whether Fire/Lava can destroy this item when it lands on the ground there (dropped,
    /// thrown, spilled from a chest, or dropped as loot) -- see ItemDestructionRules. Defaults to
    /// a type-based rule (paper/parchment Scrolls and Spellbooks, wooden Ammunition shafts all
    /// burn; everything else -- weapons, armor, potions in sealed vials, wands, jewelry -- doesn't)
    /// rather than a hand-authored flag on all ~140 catalog entries, the same "derive from what
    /// the item already declares" approach GoldValue's own default already uses. Pass flammable
    /// explicitly only for the rare exception that needs to diverge from its type's default (e.g.
    /// a metal-bound spellbook that shouldn't burn).
    /// </summary>
    public bool Flammable => explicitFlammable ?? ComputeDefaultFlammable(Type);

    private readonly bool? explicitFlammable;

    /// <summary>
    /// Whether this item can ever provide illumination -- ItemType.LightSource (Candle/Torch/
    /// Lantern) items set this true; everything else defaults false. Existing purely to gate
    /// LightRadius/MaxLightDuration/etc. from applying to items that were never meant to
    /// light anything, the same role IsSkeletonKey/IsIdentifyScroll already play for their
    /// own single-purpose flags.
    /// </summary>
    public bool EmitsLight { get; }

    /// <summary>How many tiles this item illuminates outward while lit -- see LightingSystem.ComputeVisibleCells. Meaningless when EmitsLight is false.</summary>
    public int LightRadius { get; }

    /// <summary>Total turns of burn time this item starts with -- RemainingLightDuration is initialized to this at construction (and again on Clone, so every spawned instance starts full). Meaningless when EmitsLight is false.</summary>
    public int MaxLightDuration { get; }

    /// <summary>Chance, checked once per completed player turn while lit, that this light randomly goes out (design spec section 9) -- does NOT consume RemainingLightDuration when it fires. Meaningless when EmitsLight is false.</summary>
    public double ExtinguishChancePerTurn { get; }

    /// <summary>Whether walking onto Water while this is lit immediately extinguishes it (design spec section 10) -- true for every current physical light source. Meaningless when EmitsLight is false.</summary>
    public bool ExtinguishedByWater { get; }

    /// <summary>Whether this item is removed from inventory once its RemainingLightDuration naturally reaches 0 (Candle/Torch -- "consumed") versus preserved dark and relightable-if-ever-refueled (Lantern) -- design spec section 8. Meaningless when EmitsLight is false, and irrelevant to random extinguishing (section 9), which never destroys anything.</summary>
    public bool DestroyedWhenLightExhausted { get; }

    /// <summary>
    /// Whether this light source is currently burning -- the ONLY thing that makes EmitsLight
    /// actually illuminate anything (design spec section 6: merely carrying an unlit torch does
    /// nothing). Per-instance mutable state, like Charges/IsIdentified -- see the class-level doc
    /// comment on why that means a light-emitting item must always be cloned at spawn time
    /// rather than handed out as the shared catalog reference (ComputeDefaultFlammable-style
    /// type checks don't apply here since EmitsLight is a plain catalog-level bool, not derived).
    /// </summary>
    public bool IsLit { get; set; }

    /// <summary>Turns of burn time left -- decremented once per completed player turn while IsLit (see LightingSystem.ProcessTurn). Initialized to MaxLightDuration at construction; never reset back to full by extinguishing (intentional or random), only by... nothing yet -- refueling is future work (design spec section 20).</summary>
    public int RemainingLightDuration { get; set; }

    /// <summary>A Flammable item that's also Fireproof survives a Fire tile (but not Lava, unless also LavaProof) -- see ItemDestructionRules. Meaningless when Flammable is false.</summary>
    public bool Fireproof { get; }

    /// <summary>A Flammable item that's also LavaProof survives a Lava tile (but not plain Fire, unless also Fireproof) -- see ItemDestructionRules. Meaningless when Flammable is false.</summary>
    public bool LavaProof { get; }

    /// <summary>Non-null makes this item a portable container (a bag) -- see ContainerComponent/ContainerRules/ContainerTransferService. Null for every ordinary item.</summary>
    public ContainerComponent Container { get; }

    /// <summary>Corpse System: non-null only for a portable corpse item (ItemType.Corpse) -- see CorpseItemFactory. Null for every ordinary item.</summary>
    public CorpseMetadata CorpseMetadata { get; }

    /// <summary>
    /// Centralizes the "does a spawned copy of this item need its own independent instance
    /// instead of the shared catalog reference" decision -- Charges, IsIdentified, EmitsLight/
    /// IsLit/RemainingLightDuration, and now Container.Contents are all per-instance mutable
    /// state that must never be shared across two spawned copies of the same catalog entry.
    /// Every spawn site (DungeonGenerator.SpawnItems/SpawnChests, LootGenerator, Trader,
    /// StartingGearGenerator, SaveManager.ResolveItem) should check this instead of duplicating
    /// the condition -- see Clone().
    /// </summary>
    public bool RequiresUniqueInstance => Charges != null || !IsIdentified || EmitsLight || Container != null;

    /// <summary>What the player sees in place of Name while unidentified -- the mundane item type, never the true (possibly magical) name. Description/LongDescription stay visible either way; only the name and mechanical details are hidden.</summary>
    public string DisplayName => IsIdentified ? Name : $"Unidentified {Type}";

    /// <summary>
    /// Derives a sale value from attributes every item already declares, rather than hand-authoring
    /// ~100 individual numbers across the catalog -- mirrors how GoldDropCalculator already derives
    /// monster gold from Level instead of a stored per-monster figure. Coefficients are a tuning
    /// detail, easy to rebalance; the shape (type baseline, scaled up by level/bonuses/magic/charges)
    /// is the actual design.
    /// </summary>
    private static int ComputeDefaultGoldValue(ItemType type, int minimumLevel, int physicalAttackBonus, int magicalAttackBonus,
        int defenseBonus, IReadOnlyDictionary<PrimaryAttribute, int> statModifiers, bool isBlessed, bool isCursed,
        IReadOnlyList<ItemStatusEffect> statusEffects, int? charges, ResistanceSet resistanceModifiers)
    {
        double basePrice = type switch
        {
            ItemType.Weapon or ItemType.Armor => 25,
            ItemType.Wand => 40,
            ItemType.Spellbook => 30,
            ItemType.Scroll => 15,
            ItemType.Consumable => 8,
            ItemType.Key or ItemType.Lockpick => 5,
            ItemType.Ammunition => 2,
            ItemType.Container => 20,   // every catalog bag sets an explicit goldValue scaling with size/capacity/reduction instead -- see ContainerCatalog.cs; this is just a defensive fallback
            _ => 10
        };

        double levelFactor = 1 + minimumLevel * 0.5;
        double statBonusFactor = 1 + (physicalAttackBonus + magicalAttackBonus + defenseBonus) * 0.08;
        bool hasResistance = resistanceModifiers != null &&
            (resistanceModifiers.Fire != 0 || resistanceModifiers.Water != 0 || resistanceModifiers.Ice != 0 ||
             resistanceModifiers.Shock != 0 || resistanceModifiers.Poison != 0 || resistanceModifiers.Magic != 0);
        bool isMagical = (statModifiers?.Count ?? 0) > 0 || (statusEffects?.Count ?? 0) > 0 || isBlessed || isCursed || hasResistance;
        double magicalFactor = isMagical ? 1.5 : 1.0;
        double chargesFactor = charges.HasValue ? 1 + charges.Value * 0.1 : 1.0;

        return (int)Math.Round(basePrice * levelFactor * statBonusFactor * magicalFactor * chargesFactor);
    }

    private static bool ComputeDefaultFlammable(ItemType type) => type switch
    {
        ItemType.Scroll or ItemType.Spellbook or ItemType.Ammunition => true,
        _ => false
    };

    public Item(
        string name, char symbol, string description, ItemType type,
        EquipmentType equipmentType = EquipmentType.None,
        EquipmentCategory equipmentCategory = EquipmentCategory.None,
        int healthRestore = 0, int manaRestore = 0, int physicalAttackBonus = 0, int magicalAttackBonus = 0, int defenseBonus = 0,
        IReadOnlyDictionary<PrimaryAttribute, int> statModifiers = null,
        Spell castsSpell = null, int? charges = null,
        int minimumLevel = 0, CharacterClass requiredClass = null, Race requiredRace = null, int goldValue = 0,
        string longDescription = null, Size size = Size.Small, ItemSize itemSize = ItemSize.Medium, bool canDropAsLoot = true, Spell teachesSpell = null,
        AttackType? attackType = null, WeaponType? weaponType = null, ArmorWeight? armorWeight = null,
        double weight = 0, int carryCapacityBonus = 0, int carryCapacityBonusDuration = 0,
        bool isSkeletonKey = false, bool canBeLost = true,
        bool isIdentified = true, bool isBlessed = false, bool isCursed = false, int blessedUndeadDamageBonus = 0,
        IReadOnlyList<ItemStatusEffect> statusEffects = null, int encumbranceModifier = 0, bool isIdentifyScroll = false,
        AmmunitionType? ammunitionType = null, AmmunitionType? requiredAmmunitionType = null,
        bool canBeThrown = false, ThrownWeaponBehavior? thrownWeaponBehavior = null, int projectileRange = 0,
        ThrowableCategory? throwableCategory = null, ResistanceSet resistanceModifiers = null,
        bool? flammable = null, bool fireproof = false, bool lavaProof = false,
        bool emitsLight = false, int lightRadius = 0, int maxLightDuration = 0,
        double extinguishChancePerTurn = 0, bool extinguishedByWater = false, bool destroyedWhenLightExhausted = false,
        bool isLit = false, int? remainingLightDuration = null,
        ThrowableCapability throwableCapability = ThrowableCapability.MeleeOnly,
        double breakChanceOnCreatureHit = 0, double breakChanceOnWallImpact = 0, double breakChanceOnOrdinaryLanding = 0,
        ContainerComponent container = null, CorpseMetadata corpseMetadata = null)
    {
        Name = name;
        Symbol = symbol;
        Description = description;
        LongDescription = longDescription ?? description;
        Type = type;
        EquipmentType = equipmentType;
        EquipmentCategory = equipmentCategory;
        HealthRestore = healthRestore;
        ManaRestore = manaRestore;
        PhysicalAttackBonus = physicalAttackBonus;
        MagicalAttackBonus = magicalAttackBonus;
        DefenseBonus = defenseBonus;
        StatModifiers = statModifiers ?? new Dictionary<PrimaryAttribute, int>();
        CastsSpell = castsSpell;
        Charges = charges;
        MinimumLevel = minimumLevel;
        RequiredClass = requiredClass;
        RequiredRace = requiredRace;
        explicitGoldValue = goldValue;
        Size = size;
        ItemSize = itemSize;
        CanDropAsLoot = canDropAsLoot;
        TeachesSpell = teachesSpell;
        AttackType = attackType;
        WeaponType = weaponType;
        ArmorWeight = armorWeight;
        Weight = weight;
        CarryCapacityBonus = carryCapacityBonus;
        CarryCapacityBonusDuration = carryCapacityBonusDuration;
        IsSkeletonKey = isSkeletonKey;
        CanBeLost = canBeLost;
        IsIdentified = isIdentified;
        IsBlessed = isBlessed;
        IsCursed = isCursed;
        BlessedUndeadDamageBonus = blessedUndeadDamageBonus;
        StatusEffects = statusEffects ?? Array.Empty<ItemStatusEffect>();
        EncumbranceModifier = encumbranceModifier;
        IsIdentifyScroll = isIdentifyScroll;
        AmmunitionType = ammunitionType;
        RequiredAmmunitionType = requiredAmmunitionType;
        CanBeThrown = canBeThrown;
        ThrownWeaponBehavior = canBeThrown ? (thrownWeaponBehavior ?? Entities.ThrownWeaponBehavior.DropsAtImpactPoint) : null;
        ProjectileRange = projectileRange;
        ThrowableCategory = canBeThrown ? throwableCategory : null;
        ResistanceModifiers = resistanceModifiers ?? ResistanceSet.Zero;
        explicitFlammable = flammable;
        Fireproof = fireproof;
        LavaProof = lavaProof;
        EmitsLight = emitsLight;
        LightRadius = lightRadius;
        MaxLightDuration = maxLightDuration;
        ExtinguishChancePerTurn = extinguishChancePerTurn;
        ExtinguishedByWater = extinguishedByWater;
        DestroyedWhenLightExhausted = destroyedWhenLightExhausted;
        IsLit = isLit;
        RemainingLightDuration = remainingLightDuration ?? maxLightDuration;
        ThrowableCapability = throwableCapability;
        BreakChanceOnCreatureHit = breakChanceOnCreatureHit;
        BreakChanceOnWallImpact = breakChanceOnWallImpact;
        BreakChanceOnOrdinaryLanding = breakChanceOnOrdinaryLanding;
        Container = container;
        CorpseMetadata = corpseMetadata;
    }

    /// <summary>
    /// A fresh copy with its own independent Charges/IsIdentified state -- see the type-level
    /// note above and IsIdentified's own doc comment. Passes explicitGoldValue through (not the
    /// computed GoldValue) so a clone with a genuine hand-authored price keeps it, while a
    /// clone of a default-priced item keeps recomputing its own GoldValue off its own (possibly
    /// later-changing) Charges instead of freezing at whatever the source's Charges happened to
    /// be at clone time.
    /// </summary>
    public Item Clone() => new(
        Name, Symbol, Description, Type, EquipmentType, EquipmentCategory,
        HealthRestore, ManaRestore, PhysicalAttackBonus, MagicalAttackBonus, DefenseBonus,
        StatModifiers,
        CastsSpell, Charges,
        MinimumLevel, RequiredClass, RequiredRace, explicitGoldValue,
        LongDescription, Size, ItemSize, CanDropAsLoot, TeachesSpell, AttackType, WeaponType, ArmorWeight,
        Weight, CarryCapacityBonus, CarryCapacityBonusDuration, IsSkeletonKey, CanBeLost,
        IsIdentified, IsBlessed, IsCursed, BlessedUndeadDamageBonus, StatusEffects, EncumbranceModifier, IsIdentifyScroll,
        AmmunitionType, RequiredAmmunitionType, CanBeThrown, ThrownWeaponBehavior, ProjectileRange, ThrowableCategory,
        ResistanceModifiers, explicitFlammable, Fireproof, LavaProof,
        EmitsLight, LightRadius, MaxLightDuration, ExtinguishChancePerTurn, ExtinguishedByWater, DestroyedWhenLightExhausted,
        IsLit, RemainingLightDuration,
        ThrowableCapability, BreakChanceOnCreatureHit, BreakChanceOnWallImpact, BreakChanceOnOrdinaryLanding,
        Container?.CloneEmpty(), CorpseMetadata);
}
