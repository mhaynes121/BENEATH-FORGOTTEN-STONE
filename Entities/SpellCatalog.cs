using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// The spell catalog. Same pattern as Items.cs/CharacterClass.cs/Race.cs:
/// add one static entry + register it in `All`, then make it reachable by
/// granting it to a class in Player.cs, a monster archetype in Monster.cs,
/// or an item's CastsSpell in Items.cs. Not every catalog spell is granted
/// anywhere yet (Poison Cloud, Drain Life) -- same as Items.All already
/// holding more items than any run is guaranteed to find.
///
/// Named SpellCatalog rather than the more obvious "Spells" because a
/// class can't share a fully-qualified name with the BENEATH_FORGOTTEN_STONE.
/// Entities.Spells namespace (Spell, SpellCaster, etc. live there) --
/// C# treats that as a hard conflict, not just an ambiguity.
/// </summary>
public static class SpellCatalog
{
    public static readonly Spell MagicMissile = new(
        "Magic Missile", "A bolt of raw arcane force.", '*',
        new SpellCastingConfiguration(manaCost: 8),
        new SpellTargetingConfiguration(SpellTargetType.SingleTarget, range: 10),
        new SpellEffect[] { new DamageEffect(new DiceRoll(2, 6), DamageType.Arcane) },
        level: 1, allowedClasses: new[] { CharacterClass.Mage },
        proficiencyId: "spell.magic_missile", governingAttribute: PrimaryAttribute.Knowledge);

    public static readonly Spell Fireball = new(
        "Fireball", "Hurls an explosive ball of flame.", 'o',
        new SpellCastingConfiguration(manaCost: 15, cooldown: 3),
        new SpellTargetingConfiguration(SpellTargetType.Tile, range: 8, areaOfEffectRadius: 2, requiresLineOfSight: true),
        new SpellEffect[]
        {
            new DamageEffect(new DiceRoll(4, 6), DamageType.Fire),
            new StatusEffect("Burning", duration: 3, tickDamage: 2, tickDamageType: DamageType.Fire)
        },
        level: 5, allowedClasses: new[] { CharacterClass.Mage },
        proficiencyId: "spell.fireball", governingAttribute: PrimaryAttribute.Knowledge);

    public static readonly Spell ArcaneShield = new(
        "Arcane Shield", "Wraps the caster in a shimmering ward.", '#',
        new SpellCastingConfiguration(manaCost: 12, cooldown: 10),
        new SpellTargetingConfiguration(SpellTargetType.Self, range: 0),
        new SpellEffect[]
        {
            new StatModifierEffect(Stat.Armor, amount: 8, duration: 10),
            new StatModifierEffect(Stat.MagicResistance, amount: 3, duration: 10)
        },
        level: 3, allowedClasses: new[] { CharacterClass.Mage },
        proficiencyId: "spell.arcane_shield", governingAttribute: PrimaryAttribute.Knowledge);

    public static readonly Spell Haste = new(
        "Haste", "Quickens the caster's step.", '+',
        new SpellCastingConfiguration(manaCost: 10, cooldown: 8),
        new SpellTargetingConfiguration(SpellTargetType.Self, range: 0),
        new SpellEffect[] { new StatModifierEffect(Stat.MovementSpeed, amount: 3, duration: 8) },
        level: 2, allowedClasses: new[] { CharacterClass.Priest },
        proficiencyId: "spell.haste", governingAttribute: PrimaryAttribute.Wisdom);

    public static readonly Spell PoisonCloud = new(
        "Poison Cloud", "Conjures a roiling cloud of caustic vapor.", '~',
        new SpellCastingConfiguration(manaCost: 18, cooldown: 5),
        new SpellTargetingConfiguration(SpellTargetType.Tile, range: 6, areaOfEffectRadius: 3, requiresLineOfSight: true),
        new SpellEffect[] { new StatusEffect("Poisoned", duration: 6, tickDamage: 2, tickDamageType: DamageType.Poison) },
        level: 6, allowedClasses: new[] { CharacterClass.Mage },
        proficiencyId: "spell.poison_cloud", governingAttribute: PrimaryAttribute.Knowledge);

    /// <summary>
    /// Prone/Knockdown System: deals no damage at all -- the knockdown IS the spell, so its
    /// earthquake flavor lives in KnockdownEffect's own success/failure message templates rather
    /// than a separate FlavorCastMessage override (which can't reference the target by name). No
    /// DamageType classification needed since there's no DamageEffect here to carry one.
    /// </summary>
    public static readonly Spell Tremor = new(
        "Tremor", "Shakes the earth beneath the target in a sudden, localized quake.", '~',
        new SpellCastingConfiguration(manaCost: 5, cooldown: 6),
        new SpellTargetingConfiguration(SpellTargetType.SingleTarget, range: 5),
        new SpellEffect[]
        {
            new KnockdownEffect(
                successMessageTemplate: "The ground bucks and shudders beneath {target}, who is thrown from their feet!",
                failureMessageTemplate: "The ground shudders, but {target} keeps its footing.")
        },
        level: 3, allowedClasses: new[] { CharacterClass.Mage },
        proficiencyId: "spell.tremor", governingAttribute: PrimaryAttribute.Knowledge);

    public static readonly Spell Teleport = new(
        "Teleport", "Blinks the caster through space.", '?',
        new SpellCastingConfiguration(manaCost: 20, cooldown: 6),
        new SpellTargetingConfiguration(SpellTargetType.Tile, range: 12, scalesRangeWithProficiency: true),
        new SpellEffect[] { new TeleportEffect() },
        level: 4, allowedClasses: new[] { CharacterClass.Mage },
        proficiencyId: "spell.teleport", governingAttribute: PrimaryAttribute.Knowledge);

    public static readonly Spell DrainLife = new(
        "Drain Life", "Siphons the target's vitality into the caster.", '&',
        new SpellCastingConfiguration(manaCost: 12, cooldown: 4),
        new SpellTargetingConfiguration(SpellTargetType.SingleTarget, range: 5),
        new SpellEffect[]
        {
            new DamageEffect(new DiceRoll(3, 8), DamageType.Shadow),
            new HealEffect(percentOfDamageDealt: 0.5, target: HealTarget.Caster)
        },
        level: 4, allowedClasses: new[] { CharacterClass.Mage },
        proficiencyId: "spell.drain_life", governingAttribute: PrimaryAttribute.Knowledge);

    public static readonly Spell Heal = new(
        "Heal", "Mends the caster's wounds.", '+',
        new SpellCastingConfiguration(manaCost: 10, cooldown: 3),
        new SpellTargetingConfiguration(SpellTargetType.Self, range: 0),
        new SpellEffect[] { new HealEffect(fixedAmount: 12) },
        level: 1, allowedClasses: new[] { CharacterClass.Priest },
        proficiencyId: "spell.heal", governingAttribute: PrimaryAttribute.Wisdom);

    // --- Mage-exclusive ---

    public static readonly Spell ArcaneBolt = new(
        "Arcane Bolt", "A weak bolt of raw magic -- every mage learns this first.", '*',
        new SpellCastingConfiguration(manaCost: 4),
        new SpellTargetingConfiguration(SpellTargetType.SingleTarget, range: 6),
        new SpellEffect[] { new DamageEffect(new DiceRoll(1, 6, 1), DamageType.Arcane) },
        level: 1, allowedClasses: new[] { CharacterClass.Mage },
        proficiencyId: "spell.arcane_bolt", governingAttribute: PrimaryAttribute.Knowledge);

    public static readonly Spell Spark = new(
        "Spark", "A weak crackle of lightning -- cheap and always ready.", '`',
        new SpellCastingConfiguration(manaCost: 5),
        new SpellTargetingConfiguration(SpellTargetType.SingleTarget, range: 8),
        new SpellEffect[] { new DamageEffect(new DiceRoll(1, 6, 2), DamageType.Lightning) },
        level: 1, allowedClasses: new[] { CharacterClass.Mage },
        proficiencyId: "spell.spark", governingAttribute: PrimaryAttribute.Knowledge);

    public static readonly Spell FrostBolt = new(
        "Frost Bolt", "A shard of freezing ice hurled at the target.", '*',
        new SpellCastingConfiguration(manaCost: 8),
        new SpellTargetingConfiguration(SpellTargetType.SingleTarget, range: 10),
        new SpellEffect[] { new DamageEffect(new DiceRoll(2, 6), DamageType.Ice) },
        level: 1, allowedClasses: new[] { CharacterClass.Mage },
        proficiencyId: "spell.frost_bolt", governingAttribute: PrimaryAttribute.Knowledge);

    public static readonly Spell FireBolt = new(
        "Fire Bolt", "A jet of flame that sears and lingers.", 'o',
        new SpellCastingConfiguration(manaCost: 10),
        new SpellTargetingConfiguration(SpellTargetType.SingleTarget, range: 9),
        new SpellEffect[]
        {
            new DamageEffect(new DiceRoll(2, 6), DamageType.Fire),
            new StatusEffect("Burning", duration: 2, tickDamage: 2, tickDamageType: DamageType.Fire)
        },
        level: 2, allowedClasses: new[] { CharacterClass.Mage },
        proficiencyId: "spell.fire_bolt", governingAttribute: PrimaryAttribute.Knowledge);

    public static readonly Spell FrostLance = new(
        "Frost Lance", "A piercing lance of ice that slows what it strikes.", '/',
        new SpellCastingConfiguration(manaCost: 12, cooldown: 2),
        new SpellTargetingConfiguration(SpellTargetType.SingleTarget, range: 8),
        new SpellEffect[]
        {
            new DamageEffect(new DiceRoll(3, 6), DamageType.Ice),
            new StatModifierEffect(Stat.MovementSpeed, amount: -2, duration: 4, isHostileSecondaryEffect: true)
        },
        level: 3, allowedClasses: new[] { CharacterClass.Mage },
        proficiencyId: "spell.frost_lance", governingAttribute: PrimaryAttribute.Knowledge);

    public static readonly Spell Blink = new(
        "Blink", "A short, instinctive step through space.", '?',
        new SpellCastingConfiguration(manaCost: 8, cooldown: 3),
        new SpellTargetingConfiguration(SpellTargetType.Tile, range: 5, scalesRangeWithProficiency: true),
        new SpellEffect[] { new TeleportEffect() },
        level: 3, allowedClasses: new[] { CharacterClass.Mage },
        proficiencyId: "spell.blink", governingAttribute: PrimaryAttribute.Knowledge);

    public static readonly Spell ManaBurn = new(
        "Mana Burn", "Sears the target's mind, damaging them and draining their magic.", '%',
        new SpellCastingConfiguration(manaCost: 10, cooldown: 4),
        new SpellTargetingConfiguration(SpellTargetType.SingleTarget, range: 6),
        new SpellEffect[]
        {
            new DamageEffect(new DiceRoll(1, 6), DamageType.Arcane),
            new ManaEffect(-15, HealTarget.AffectedActors)
        },
        level: 4, allowedClasses: new[] { CharacterClass.Mage },
        proficiencyId: "spell.mana_burn", governingAttribute: PrimaryAttribute.Knowledge);

    public static readonly Spell ManaShield = new(
        "Mana Shield", "Wraps the caster's mind in a ward against hostile magic.", '#',
        new SpellCastingConfiguration(manaCost: 14, cooldown: 10),
        new SpellTargetingConfiguration(SpellTargetType.Self, range: 0),
        new SpellEffect[] { new StatModifierEffect(Stat.MagicResistance, amount: 6, duration: 10) },
        level: 5, allowedClasses: new[] { CharacterClass.Mage },
        proficiencyId: "spell.mana_shield", governingAttribute: PrimaryAttribute.Knowledge);

    public static readonly Spell FrostNova = new(
        "Frost Nova", "A burst of freezing air that damages and slows everything nearby.", 'O',
        new SpellCastingConfiguration(manaCost: 18, cooldown: 5),
        new SpellTargetingConfiguration(SpellTargetType.Tile, range: 3, areaOfEffectRadius: 2, requiresLineOfSight: true),
        new SpellEffect[]
        {
            new DamageEffect(new DiceRoll(3, 6), DamageType.Ice),
            new StatModifierEffect(Stat.MovementSpeed, amount: -2, duration: 4, isHostileSecondaryEffect: true)
        },
        level: 6, allowedClasses: new[] { CharacterClass.Mage },
        proficiencyId: "spell.frost_nova", governingAttribute: PrimaryAttribute.Knowledge);

    public static readonly Spell ChainLightning = new(
        "Chain Lightning", "Arcing bolts of lightning that leap between nearby foes.", '%',
        new SpellCastingConfiguration(manaCost: 22, cooldown: 4),
        new SpellTargetingConfiguration(SpellTargetType.Tile, range: 8, areaOfEffectRadius: 2, requiresLineOfSight: true),
        new SpellEffect[] { new DamageEffect(new DiceRoll(5, 6), DamageType.Lightning) },
        level: 7, allowedClasses: new[] { CharacterClass.Mage },
        proficiencyId: "spell.chain_lightning", governingAttribute: PrimaryAttribute.Knowledge);

    /// <summary>Mage counterpart to the Thief's Detect Traps skill (SkillCatalog.DetectTraps) -- same timed, Knowledge-scaled-chance window instead of Agility, via the same shared DetectTrapsEffect. See GameLoop.RollDetectTraps.</summary>
    public static readonly Spell DetectTraps = new(
        "Detect Traps", "Opens the caster's senses to hidden danger nearby for a short time.", '^',
        new SpellCastingConfiguration(manaCost: 14, cooldown: 12),
        new SpellTargetingConfiguration(SpellTargetType.Self, range: 0),
        new SpellEffect[] { new DetectTrapsEffect(duration: 10, PrimaryAttribute.Knowledge) },
        level: 4, allowedClasses: new[] { CharacterClass.Mage },
        proficiencyId: "spell.detect_traps", governingAttribute: PrimaryAttribute.Knowledge);

    public static readonly Spell Meteor = new(
        "Meteor", "Calls down a blazing meteor to devastate the area.", 'O',
        new SpellCastingConfiguration(manaCost: 35, cooldown: 8),
        new SpellTargetingConfiguration(SpellTargetType.Tile, range: 9, areaOfEffectRadius: 3, requiresLineOfSight: true),
        new SpellEffect[]
        {
            new DamageEffect(new DiceRoll(8, 6), DamageType.Fire),
            new StatusEffect("Burning", duration: 3, tickDamage: 3, tickDamageType: DamageType.Fire)
        },
        level: 10, allowedClasses: new[] { CharacterClass.Mage },
        proficiencyId: "spell.meteor", governingAttribute: PrimaryAttribute.Knowledge);

    // --- Priest-exclusive ---

    public static readonly Spell Smite = new(
        "Smite", "Calls down a bolt of holy judgment.", '+',
        new SpellCastingConfiguration(manaCost: 8),
        new SpellTargetingConfiguration(SpellTargetType.SingleTarget, range: 8),
        new SpellEffect[] { new DamageEffect(new DiceRoll(2, 6), DamageType.Holy) },
        level: 1, allowedClasses: new[] { CharacterClass.Priest },
        proficiencyId: "spell.smite", governingAttribute: PrimaryAttribute.Wisdom);

    public static readonly Spell MinorHeal = new(
        "Minor Heal", "A quick, modest mending of the caster's wounds.", '+',
        new SpellCastingConfiguration(manaCost: 6, cooldown: 2),
        new SpellTargetingConfiguration(SpellTargetType.Self, range: 0),
        new SpellEffect[] { new HealEffect(fixedAmount: 8) },
        level: 1, allowedClasses: new[] { CharacterClass.Priest },
        proficiencyId: "spell.minor_heal", governingAttribute: PrimaryAttribute.Wisdom);

    public static readonly Spell Bless = new(
        "Bless", "Fills the caster with divine favor, sharpening their strikes.", '^',
        new SpellCastingConfiguration(manaCost: 10, cooldown: 6),
        new SpellTargetingConfiguration(SpellTargetType.Self, range: 0),
        new SpellEffect[] { new StatModifierEffect(Stat.PhysicalAttack, amount: 3, duration: 8) },
        level: 2, allowedClasses: new[] { CharacterClass.Priest },
        proficiencyId: "spell.bless", governingAttribute: PrimaryAttribute.Wisdom);

    /// <summary>Dark Rooms/Light Sources spec section 16 -- a Mage-exclusive light source that follows the caster; see Core/LightingSystem.cs. Radius/duration come from LightingConfig, not hard-coded here.</summary>
    public static readonly Spell ArcaneOrb = new(
        "Arcane Orb", "Conjures a pale orb of arcane light that follows the caster.", 'o',
        new SpellCastingConfiguration(manaCost: 12, cooldown: 15),
        new SpellTargetingConfiguration(SpellTargetType.Self, range: 0),
        new SpellEffect[] { new LightEffect(LightingConfig.ArcaneOrbLightRadius, LightingConfig.ArcaneOrbDuration) },
        level: 2, allowedClasses: new[] { CharacterClass.Mage },
        flavorCastMessage: "A pale orb of arcane light flickers into existence.",
        proficiencyId: "spell.arcane_orb", governingAttribute: PrimaryAttribute.Knowledge);

    /// <summary>Dark Rooms/Light Sources spec section 17 -- a Priest-exclusive light source radiating from the caster's own body, rather than a separate orb (see ArcaneOrb's doc comment for the Mage version this is deliberately distinct from).</summary>
    public static readonly Spell DivineRadiance = new(
        "Divine Radiance", "Radiates a warm, divine light directly from the caster's body.", '*',
        new SpellCastingConfiguration(manaCost: 12, cooldown: 15),
        new SpellTargetingConfiguration(SpellTargetType.Self, range: 0),
        new SpellEffect[] { new LightEffect(LightingConfig.DivineRadianceLightRadius, LightingConfig.DivineRadianceDuration) },
        level: 2, allowedClasses: new[] { CharacterClass.Priest },
        flavorCastMessage: "A warm radiance surrounds you.",
        proficiencyId: "spell.divine_radiance", governingAttribute: PrimaryAttribute.Wisdom);

    // Shared by both spellcasting classes -- Priest gets it via SpellbookCatalog's
    // auto-generated "Spellbook of Identify", Mage via SpellScrollCatalog's auto-generated
    // "Scroll of Identify". The latter is why Items.ScrollOfIdentify (a bespoke, direct-use
    // consumable, unrelated to this spell) was renamed to "Scroll of Revealing" -- see its
    // own doc comment in Items.cs.
    public static readonly Spell Identify = new(
        "Identify", "Reveals an item's true nature. Always succeeds.", '?',
        new SpellCastingConfiguration(manaCost: 10, cooldown: 2),
        new SpellTargetingConfiguration(SpellTargetType.Item, range: 0),
        new SpellEffect[] { new IdentifyEffect() },
        level: 2, allSpellcasters: true);

    public static readonly Spell HolyBolt = new(
        "Holy Bolt", "A focused lance of holy light.", '*',
        new SpellCastingConfiguration(manaCost: 12, cooldown: 2),
        new SpellTargetingConfiguration(SpellTargetType.SingleTarget, range: 9),
        new SpellEffect[] { new DamageEffect(new DiceRoll(3, 6), DamageType.Holy) },
        level: 3, allowedClasses: new[] { CharacterClass.Priest },
        proficiencyId: "spell.holy_bolt", governingAttribute: PrimaryAttribute.Wisdom);

    public static readonly Spell Fortify = new(
        "Fortify", "Hardens the caster's body against harm.", '#',
        new SpellCastingConfiguration(manaCost: 14, cooldown: 8),
        new SpellTargetingConfiguration(SpellTargetType.Self, range: 0),
        new SpellEffect[] { new StatModifierEffect(Stat.Armor, amount: 6, duration: 10) },
        level: 4, allowedClasses: new[] { CharacterClass.Priest },
        proficiencyId: "spell.fortify", governingAttribute: PrimaryAttribute.Wisdom);

    public static readonly Spell DivineShield = new(
        "Divine Shield", "Wraps the caster in a shimmering field of divine light.", '#',
        new SpellCastingConfiguration(manaCost: 14, cooldown: 10),
        new SpellTargetingConfiguration(SpellTargetType.Self, range: 0),
        new SpellEffect[] { new StatModifierEffect(Stat.MagicResistance, amount: 6, duration: 10) },
        level: 5, allowedClasses: new[] { CharacterClass.Priest },
        proficiencyId: "spell.divine_shield", governingAttribute: PrimaryAttribute.Wisdom);

    public static readonly Spell GreaterHeal = new(
        "Greater Heal", "A powerful mending of the caster's wounds.", '+',
        new SpellCastingConfiguration(manaCost: 22, cooldown: 5),
        new SpellTargetingConfiguration(SpellTargetType.Self, range: 0),
        new SpellEffect[] { new HealEffect(fixedAmount: 30) },
        level: 6, allowedClasses: new[] { CharacterClass.Priest },
        proficiencyId: "spell.greater_heal", governingAttribute: PrimaryAttribute.Wisdom);

    public static readonly Spell Consecration = new(
        "Consecration", "Sanctifies the ground, burning anything unholy that stands on it.", 'o',
        new SpellCastingConfiguration(manaCost: 20, cooldown: 5),
        new SpellTargetingConfiguration(SpellTargetType.Tile, range: 6, areaOfEffectRadius: 2, requiresLineOfSight: true),
        new SpellEffect[] { new DamageEffect(new DiceRoll(4, 6), DamageType.Holy) },
        level: 7, allowedClasses: new[] { CharacterClass.Priest },
        proficiencyId: "spell.consecration", governingAttribute: PrimaryAttribute.Wisdom);

    public static readonly Spell Renewal = new(
        "Renewal", "Restores both body and spirit in a single working.", '+',
        new SpellCastingConfiguration(manaCost: 10, cooldown: 8),
        new SpellTargetingConfiguration(SpellTargetType.Self, range: 0),
        new SpellEffect[]
        {
            new HealEffect(fixedAmount: 20),
            new ManaEffect(10, HealTarget.Caster)
        },
        level: 8, allowedClasses: new[] { CharacterClass.Priest },
        proficiencyId: "spell.renewal", governingAttribute: PrimaryAttribute.Wisdom);

    public static readonly Spell RighteousFury = new(
        "Righteous Fury", "Channels holy wrath into speed and strength.", '^',
        new SpellCastingConfiguration(manaCost: 25, cooldown: 12),
        new SpellTargetingConfiguration(SpellTargetType.Self, range: 0),
        new SpellEffect[]
        {
            new StatModifierEffect(Stat.PhysicalAttack, amount: 8, duration: 6),
            new StatModifierEffect(Stat.MovementSpeed, amount: 2, duration: 6)
        },
        level: 9, allowedClasses: new[] { CharacterClass.Priest },
        proficiencyId: "spell.righteous_fury", governingAttribute: PrimaryAttribute.Wisdom);

    // --- Shared: any spellcasting class, present or future ---

    public static readonly Spell MinorWard = new(
        "Minor Ward", "A thin protective field, cheap to weave.", '#',
        new SpellCastingConfiguration(manaCost: 5, cooldown: 3),
        new SpellTargetingConfiguration(SpellTargetType.Self, range: 0),
        new SpellEffect[] { new StatModifierEffect(Stat.Armor, amount: 2, duration: 5) },
        level: 1, allSpellcasters: true,
        proficiencyId: "spell.minor_ward");

    public static readonly Spell Swiftness = new(
        "Swiftness", "Quickens the caster's step for a short while.", '^',
        new SpellCastingConfiguration(manaCost: 8, cooldown: 5),
        new SpellTargetingConfiguration(SpellTargetType.Self, range: 0),
        new SpellEffect[] { new StatModifierEffect(Stat.MovementSpeed, amount: 2, duration: 6) },
        level: 2, allSpellcasters: true,
        proficiencyId: "spell.swiftness");

    public static readonly Spell DrainMana = new(
        "Drain Mana", "Siphons magical energy from the target into the caster.", '&',
        new SpellCastingConfiguration(manaCost: 8, cooldown: 4),
        new SpellTargetingConfiguration(SpellTargetType.SingleTarget, range: 6),
        new SpellEffect[]
        {
            new ManaEffect(-10, HealTarget.AffectedActors),
            new ManaEffect(5, HealTarget.Caster)
        },
        level: 3, allSpellcasters: true,
        proficiencyId: "spell.drain_mana");

    public static readonly Spell Regenerate = new(
        "Regenerate", "A steady working that knits minor wounds shut.", '+',
        new SpellCastingConfiguration(manaCost: 12, cooldown: 6),
        new SpellTargetingConfiguration(SpellTargetType.Self, range: 0),
        new SpellEffect[] { new HealEffect(fixedAmount: 15) },
        level: 4, allSpellcasters: true,
        proficiencyId: "spell.regenerate");

    public static readonly Spell SapStrength = new(
        "Sap Strength", "Weakens the target's body as it wounds them.", '&',
        new SpellCastingConfiguration(manaCost: 14, cooldown: 6),
        new SpellTargetingConfiguration(SpellTargetType.SingleTarget, range: 7),
        new SpellEffect[]
        {
            new DamageEffect(new DiceRoll(2, 6), DamageType.Arcane),
            new StatModifierEffect(Stat.PhysicalAttack, amount: -3, duration: 5, isHostileSecondaryEffect: true)
        },
        level: 5, allSpellcasters: true,
        proficiencyId: "spell.sap_strength");

    public static readonly Spell MysticArmor = new(
        "Mystic Armor", "A balanced ward against both blade and spell.", '#',
        new SpellCastingConfiguration(manaCost: 15, cooldown: 9),
        new SpellTargetingConfiguration(SpellTargetType.Self, range: 0),
        new SpellEffect[]
        {
            new StatModifierEffect(Stat.Armor, amount: 4, duration: 8),
            new StatModifierEffect(Stat.MagicResistance, amount: 4, duration: 8)
        },
        level: 5, allSpellcasters: true,
        proficiencyId: "spell.mystic_armor");

    public static readonly Spell BlinkStep = new(
        "Blink Step", "A middling working of short-range translocation.", '?',
        new SpellCastingConfiguration(manaCost: 16, cooldown: 7),
        new SpellTargetingConfiguration(SpellTargetType.Tile, range: 8, scalesRangeWithProficiency: true),
        new SpellEffect[] { new TeleportEffect() },
        level: 6, allSpellcasters: true,
        proficiencyId: "spell.blink_step");

    public static readonly Spell StaticCharge = new(
        "Static Charge", "Charges the target with lightning that continues to arc.", '%',
        new SpellCastingConfiguration(manaCost: 16, cooldown: 5),
        new SpellTargetingConfiguration(SpellTargetType.SingleTarget, range: 8),
        new SpellEffect[]
        {
            new DamageEffect(new DiceRoll(2, 6), DamageType.Lightning),
            new StatusEffect("Shocked", duration: 3, tickDamage: 1, tickDamageType: DamageType.Lightning)
        },
        level: 7, allSpellcasters: true,
        proficiencyId: "spell.static_charge");

    public static readonly Spell Empower = new(
        "Empower", "A capstone working that sharpens both blade and spell alike.", '^',
        new SpellCastingConfiguration(manaCost: 20, cooldown: 12),
        new SpellTargetingConfiguration(SpellTargetType.Self, range: 0),
        new SpellEffect[]
        {
            new StatModifierEffect(Stat.PhysicalAttack, amount: 5, duration: 10),
            new StatModifierEffect(Stat.MagicalAttack, amount: 5, duration: 10)
        },
        level: 8, allSpellcasters: true,
        proficiencyId: "spell.empower");

    /// <summary>+30 lbs carry capacity for 200 turns -- see EncumbranceCalculator/Stat.CarryCapacity. Shared rather than class-exclusive since encumbrance affects Mage and Priest equally.</summary>
    public static readonly Spell BullsStrength = new(
        "Bull's Strength", "Swells the caster's frame, letting them bear far more before buckling.", '^',
        new SpellCastingConfiguration(manaCost: 16, cooldown: 20),
        new SpellTargetingConfiguration(SpellTargetType.Self, range: 0),
        new SpellEffect[] { new StatModifierEffect(Stat.CarryCapacity, amount: 30, duration: 200) },
        level: 3, allSpellcasters: true,
        proficiencyId: "spell.bulls_strength");

    public static readonly IReadOnlyList<Spell> All = new[]
    {
        MagicMissile, Fireball, ArcaneShield, Haste, PoisonCloud, Teleport, DrainLife, Heal,
        ArcaneBolt, Spark, FrostBolt, FireBolt, FrostLance, Blink, ManaBurn, ManaShield, DetectTraps, FrostNova, ChainLightning, Meteor,
        Smite, MinorHeal, Bless, ArcaneOrb, DivineRadiance, Identify, HolyBolt, Fortify, DivineShield, GreaterHeal, Consecration, Renewal, RighteousFury,
        MinorWard, Swiftness, DrainMana, Regenerate, SapStrength, MysticArmor, BlinkStep, StaticCharge, Empower, BullsStrength, Tremor
    };
}
