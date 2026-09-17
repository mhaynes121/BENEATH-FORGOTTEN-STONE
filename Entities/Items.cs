using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities.Components;
using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// The item catalog. To add a new item, add one static entry here (in the
/// style of CharacterClass/Race) and it to the `All` list -- the dungeon
/// generator picks spawns randomly from `All`, so nothing else needs to
/// change for it to start showing up in the world.
/// </summary>
public static class Items
{
    public static readonly Item HealthPotion = new(
        "Health Potion", '!', "Restores a modest amount of health.",
        ItemType.Consumable, charges: 1, healthRestore: 10,
        longDescription: "A small vial of red liquid, warm to the touch. Drinking it knits shallow wounds closed almost instantly.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.5);

    public static readonly Item GreaterHealthPotion = new(
        "Greater Health Potion", '!', "Restores a large amount of health.",
        ItemType.Consumable, charges: 1, healthRestore: 25,
        longDescription: "A stoppered flask of thick crimson liquid. Alchemists charge a premium for this potency, but it can pull you back from the brink.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.75);

    public static readonly Item ManaPotion = new(
        "Mana Potion", '!', "Restores a modest amount of mana.",
        ItemType.Consumable, charges: 1, manaRestore: 10,
        longDescription: "A vial of shimmering blue liquid that hums faintly. Drinking it clears the mind and rekindles the will to cast.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.5);

    public static readonly Item GreaterManaPotion = new(
        "Greater Mana Potion", '!', "Restores a large amount of mana.",
        ItemType.Consumable, charges: 1, manaRestore: 25,
        longDescription: "A deep blue draught, cold as winter and faintly luminous. Its power is enough to fully steady a spellcaster mid-battle.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.75);

    public static readonly Item Dagger = new(
        "Dagger", '/', "A light blade. Quick, but weak.",
        ItemType.Weapon, EquipmentType.Hand, EquipmentCategory.Weapon, physicalAttackBonus: 2,
        longDescription: "A short, single-edged blade meant for quick strikes rather than raw power. Easy to conceal, easier to swing.",
        size: Size.Small, itemSize: ItemSize.Small, attackType: AttackType.Pierce, weaponType: WeaponType.Dagger, weight: 1.0);

    public static readonly Item ShortSword = new(
        "Short Sword", '/', "A reliable sidearm.",
        ItemType.Weapon, EquipmentType.Hand, EquipmentCategory.Weapon, physicalAttackBonus: 4,
        longDescription: "A well-maintained short sword designed for close combat. Its simple construction makes it dependable and easy to handle.",
        size: Size.Medium, itemSize: ItemSize.Small, attackType: AttackType.Slash, weaponType: WeaponType.Sword, weight: 2.5);

    public static readonly Item LongSword = new(
        "Long Sword", '/', "A well-balanced blade.",
        ItemType.Weapon, EquipmentType.Hand, EquipmentCategory.Weapon, physicalAttackBonus: 6,
        longDescription: "A double-edged blade of even temper and honest steel. Neither the fastest nor the heaviest weapon, but it rarely lets its wielder down.",
        size: Size.Large, itemSize: ItemSize.Large, attackType: AttackType.Slash, weaponType: WeaponType.Sword, weight: 4.0);

    public static readonly Item BattleAxe = new(
        "Battle Axe", '/', "Heavy and devastating in the right hands.",
        ItemType.Weapon, EquipmentType.Hand, EquipmentCategory.Weapon, physicalAttackBonus: 9,
        longDescription: "A wide, single-bladed axe built to cleave through armor and bone alike. Its weight punishes a weak grip as much as it punishes enemies.",
        size: Size.Large, itemSize: ItemSize.Medium, attackType: AttackType.Slash, weaponType: WeaponType.Axe, weight: 6.0);

    public static readonly Item Warhammer = new(
        "Warhammer", '/', "Crushing force, favored by Warriors.",
        ItemType.Weapon, EquipmentType.Hand, EquipmentCategory.Weapon, physicalAttackBonus: 11,
        longDescription: "A brutal, iron-headed hammer that relies on sheer momentum rather than a keen edge. Bones break long before armor does.",
        size: Size.Large, itemSize: ItemSize.Medium, attackType: AttackType.Crush, weaponType: WeaponType.Blunt, weight: 6.0);

    public static readonly Item Shield = new(
        "Wooden Shield", ')', "Held in the off-hand to turn aside blows.",
        ItemType.Armor, EquipmentType.Hand, EquipmentCategory.Shield, defenseBonus: 2,
        longDescription: "A round shield of banded oak, scarred with old dents. Unglamorous, but it has stopped more blows than most swords have landed.",
        size: Size.Medium, itemSize: ItemSize.Medium, weight: 8.0);

    // Thief's signature armor -- Medium weight. EquipmentCompatibility.AgilityPenaltyForWeight
    // only charges a Thief agility for wearing this (Warrior/Priest wear it for free), so the
    // flavor text below deliberately doesn't claim a universal cost. Kept unrestricted
    // (no RequiredClass) rather than Thief-exclusive.
    public static readonly Item LeatherArmor = new(
        "Leather Armor", '[', "Sturdy protection, doesn't slow most people down.",
        ItemType.Armor, EquipmentType.Body, defenseBonus: 1,
        longDescription: "A boiled-leather cuirass -- the classic choice for anyone who needs to stay quick on their feet without going unarmored.",
        size: Size.Small, itemSize: ItemSize.Medium, armorWeight: ArmorWeight.Medium, weight: 12.0);

    public static readonly Item ChainMail = new(
        "Chain Mail", '[', "Solid protection for the frontline.",
        ItemType.Armor, EquipmentType.Body, defenseBonus: 3,
        longDescription: "A shirt of interlocking iron rings, heavy but dependable. Turns aside glancing blows that would open leather like paper.",
        size: Size.Medium, itemSize: ItemSize.Medium, armorWeight: ArmorWeight.Medium, weight: 35.0);

    public static readonly Item PlateArmor = new(
        "Plate Armor", '[', "Heavy and nearly impenetrable.",
        ItemType.Armor, EquipmentType.Body, defenseBonus: 5,
        longDescription: "A full suit of fitted steel plate, polished despite its dents. Slow to wear, slower to fight through.",
        size: Size.Large, itemSize: ItemSize.Large, armorWeight: ArmorWeight.Heavy, weight: 55.0);

    // Mage's signature armor -- Light weight, so it's never subject to the Thief-only
    // heavier-than-Light agility penalty regardless of who wears it. Kept unrestricted
    // (no RequiredClass) rather than Mage-exclusive; Archmage's Robe (below) is the
    // higher-level, explicitly Mage-only counterpart.
    public static readonly Item WizardRobe = new(
        "Wizard's Robe", '[', "Woven with faint protective wards.",
        ItemType.Armor, EquipmentType.Body, defenseBonus: 1,
        longDescription: "A dark, star-flecked robe stitched with faintly glowing thread. Offers little physical protection, but the wards woven into it are no accident.",
        size: Size.Small, itemSize: ItemSize.Medium, armorWeight: ArmorWeight.Light, weight: 4.0);

    public static readonly Item LeatherGloves = new(
        "Leather Gloves", '{', "Supple leather, doesn't hinder grip.",
        ItemType.Armor, EquipmentType.Gloves, defenseBonus: 1,
        longDescription: "Well-fitted gloves of soft leather, broken in through long use. They guard the knuckles without dulling the sense of touch.",
        size: Size.Small, itemSize: ItemSize.Small, armorWeight: ArmorWeight.Medium, weight: 1.5);

    public static readonly Item LeatherBoots = new(
        "Leather Boots", '{', "Quiet, comfortable, well-worn.",
        ItemType.Armor, EquipmentType.Feet, defenseBonus: 1,
        longDescription: "Sturdy boots with a soft tread, favored by those who'd rather not announce their approach. Comfortable enough for a long march.",
        size: Size.Small, itemSize: ItemSize.Small, armorWeight: ArmorWeight.Medium, weight: 2.0);

    public static readonly Item IronHelmet = new(
        "Iron Helmet", '^', "A dented but serviceable helm.",
        ItemType.Armor, EquipmentType.Head, defenseBonus: 1,
        longDescription: "A simple open-faced helm, its surface scarred with old dents that speak to blows it's already turned aside.",
        size: Size.Small, itemSize: ItemSize.Small, armorWeight: ArmorWeight.Heavy, weight: 6.0);

    public static readonly Item RingOfStrength = new(
        "Ring of Strength", '=', "A band of dull iron, heavier than it looks.",
        ItemType.Armor, EquipmentType.Ring,
        statModifiers: new Dictionary<PrimaryAttribute, int> { [PrimaryAttribute.Strength] = 2 },
        longDescription: "A plain iron band, unremarkable to look at, but wearing it seems to settle a quiet, unnatural strength into the arm.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.1);

    public static readonly Item BasicWand = new(
        "Wooden Wand", '|', "A small wooden wand.",
        ItemType.Wand, EquipmentType.Hand, EquipmentCategory.Wand, magicalAttackBonus: 1,
        longDescription: "A slender wand of pale, untreated wood. It channels only the faintest arcane charge, but even that is more than none.",
        size: Size.Small, itemSize: ItemSize.Small, weaponType: WeaponType.Wand, weight: 1.0);

    public static readonly Item WandOfFire = new(
        "Wand of Fire", '|', "A charred wand that hurls a burst of flame. 3 charges.",
        ItemType.Wand, EquipmentType.Hand, EquipmentCategory.Wand, castsSpell: SpellCatalog.Fireball, charges: 3,
        longDescription: "A wand of blackened, heat-cracked wood, still warm along its length. Whatever magic is bound inside it won't last forever.",
        size: Size.Small, itemSize: ItemSize.Small, weaponType: WeaponType.Wand, weight: 1.0);

    // --- Wrists, Arms, Neck, Legs: previously had zero items in the whole catalog ---

    public static readonly Item LeatherBracers = new(
        "Leather Bracers", '(', "Simple wrist guards.",
        ItemType.Armor, EquipmentType.Wrists, defenseBonus: 1,
        longDescription: "A pair of stiffened leather bands laced tight around the wrists. Nothing fancy, but they'll turn a glancing blow.",
        size: Size.Small, itemSize: ItemSize.Small, armorWeight: ArmorWeight.Medium, weight: 1.0);

    public static readonly Item IronVambraces = new(
        "Iron Vambraces", '(', "Heavy forearm guards favored by front-liners.",
        ItemType.Armor, EquipmentType.Wrists, defenseBonus: 2,
        minimumLevel: 3, requiredClass: CharacterClass.Warrior,
        longDescription: "Riveted iron plates strapped over thick leather. Warriors favor them for blocking blows that would otherwise shatter bone.",
        size: Size.Medium, itemSize: ItemSize.Small, armorWeight: ArmorWeight.Heavy, weight: 3.0);

    public static readonly Item MagesCuffs = new(
        "Mage's Cuffs", '(', "Cuffs inscribed with faint arcane sigils.",
        ItemType.Armor, EquipmentType.Wrists,
        statModifiers: new Dictionary<PrimaryAttribute, int> { [PrimaryAttribute.Knowledge] = 1 },
        minimumLevel: 2, requiredClass: CharacterClass.Mage,
        longDescription: "Slim silver cuffs etched with sigils that shift faintly when spells are cast nearby. They sharpen a caster's recall of the words that matter.",
        size: Size.Small, itemSize: ItemSize.Small, armorWeight: ArmorWeight.Light, weight: 0.25);

    public static readonly Item PaddedSleeves = new(
        "Padded Sleeves", '}', "Quilted sleeves that soften a blow.",
        ItemType.Armor, EquipmentType.Arms, defenseBonus: 1,
        longDescription: "Thick quilted fabric wrapped around the upper arms. Not much to look at, but it takes the edge off a solid hit.",
        size: Size.Small, itemSize: ItemSize.Medium, armorWeight: ArmorWeight.Medium, weight: 1.5);

    public static readonly Item PlatePauldrons = new(
        "Plate Pauldrons", '}', "Heavy shoulder plates.",
        ItemType.Armor, EquipmentType.Arms, defenseBonus: 3,
        minimumLevel: 4, requiredClass: CharacterClass.Warrior,
        longDescription: "Overlapping steel plates that guard the shoulders and upper arms. Their weight is nothing to a Warrior used to carrying real armor.",
        size: Size.Large, itemSize: ItemSize.Small, armorWeight: ArmorWeight.Heavy, weight: 5.0);

    public static readonly Item ScholarsArmbands = new(
        "Scholar's Armbands", '}', "Simple bands worn by the devout.",
        ItemType.Armor, EquipmentType.Arms,
        statModifiers: new Dictionary<PrimaryAttribute, int> { [PrimaryAttribute.Wisdom] = 1 },
        minimumLevel: 2, requiredClass: CharacterClass.Priest,
        longDescription: "Plain cloth bands worn above the elbow, marked with a small sigil of devotion. Priests wear them as a quiet reminder to stay measured under pressure.",
        size: Size.Small, itemSize: ItemSize.Small, armorWeight: ArmorWeight.Light, weight: 0.3);

    public static readonly Item SimpleAmulet = new(
        "Simple Amulet", '"', "A plain protective charm.",
        ItemType.Armor, EquipmentType.Neck, defenseBonus: 1,
        longDescription: "A small stone pendant on a leather cord, worn smooth by handling. Common enough to find on any battlefield, but it works.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.2);

    public static readonly Item AmuletOfVitality = new(
        "Amulet of Vitality", '"', "A pendant that steadies the wearer's pulse.",
        ItemType.Armor, EquipmentType.Neck,
        statModifiers: new Dictionary<PrimaryAttribute, int> { [PrimaryAttribute.Constitution] = 2 },
        minimumLevel: 3,
        longDescription: "A warm red stone set in plain silver. It beats faintly in time with the wearer's own heart, and seems to steady it when it falters.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.2);

    public static readonly Item HolySymbol = new(
        "Holy Symbol", '"', "A symbol of devotion, worn openly.",
        ItemType.Armor, EquipmentType.Neck,
        statModifiers: new Dictionary<PrimaryAttribute, int> { [PrimaryAttribute.Wisdom] = 2 },
        minimumLevel: 3, requiredClass: CharacterClass.Priest,
        longDescription: "A polished holy symbol worn openly rather than hidden. Its presence alone seems to settle the mind before a working.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.2);

    public static readonly Item LeatherLeggings = new(
        "Leather Leggings", ']', "Simple leg protection.",
        ItemType.Armor, EquipmentType.Legs, defenseBonus: 1,
        longDescription: "Boiled leather wrapped around the thighs and shins. Doesn't slow a stride, and stops most of what would otherwise draw blood.",
        size: Size.Small, itemSize: ItemSize.Medium, armorWeight: ArmorWeight.Medium, weight: 4.0);

    public static readonly Item ChainLeggings = new(
        "Chain Leggings", ']', "Interlocking rings guarding the legs.",
        ItemType.Armor, EquipmentType.Legs, defenseBonus: 2,
        minimumLevel: 3,
        longDescription: "A skirt of fine iron rings worn over padding. Heavier than leather, but it turns aside far more.",
        size: Size.Medium, itemSize: ItemSize.Medium, armorWeight: ArmorWeight.Medium, weight: 6.0);

    public static readonly Item PlateGreaves = new(
        "Plate Greaves", ']', "Heavy leg plates.",
        ItemType.Armor, EquipmentType.Legs, defenseBonus: 3,
        minimumLevel: 5, requiredClass: CharacterClass.Warrior,
        longDescription: "Fitted steel plate that guards the legs from knee to ankle. Slows a sprint, but a Warrior rarely needs to run.",
        size: Size.Large, itemSize: ItemSize.Medium, armorWeight: ArmorWeight.Heavy, weight: 12.0);

    // --- Weapons: some class/race-flavored, giving Player.StartingGear real variety to draw from ---

    public static readonly Item ThiefsShiv = new(
        "Thief's Shiv", '/', "A small, easily hidden blade.",
        ItemType.Weapon, EquipmentType.Hand, EquipmentCategory.Weapon, physicalAttackBonus: 3,
        requiredClass: CharacterClass.Thief,
        longDescription: "A slim, unadorned blade small enough to vanish up a sleeve. It won't win a fair fight, which is exactly why a Thief doesn't pick one.",
        size: Size.Small, itemSize: ItemSize.Small, attackType: AttackType.Pierce, weaponType: WeaponType.Dagger, weight: 0.75);

    public static readonly Item Rapier = new(
        "Rapier", '/', "A slender blade built for speed.",
        ItemType.Weapon, EquipmentType.Hand, EquipmentCategory.Weapon, physicalAttackBonus: 5,
        minimumLevel: 3, requiredClass: CharacterClass.Thief,
        longDescription: "A narrow, whip-fast blade meant for darting strikes rather than brute force. It rewards quick feet over a strong arm.",
        size: Size.Medium, itemSize: ItemSize.Small, attackType: AttackType.Pierce, weaponType: WeaponType.Sword, weight: 2.5);

    public static readonly Item WarPick = new(
        "War Pick", '/', "A spiked pick built to punch through armor.",
        ItemType.Weapon, EquipmentType.Hand, EquipmentCategory.Weapon, physicalAttackBonus: 7,
        minimumLevel: 4, requiredClass: CharacterClass.Warrior,
        longDescription: "A hooked steel spike mounted on a short haft, built to punch through plate that a blade would just skate off of.",
        size: Size.Medium, itemSize: ItemSize.Medium, attackType: AttackType.Pierce, weaponType: WeaponType.Blunt, weight: 4.5);

    public static readonly Item Mace = new(
        "Mace", '/', "A simple blunt weapon.",
        ItemType.Weapon, EquipmentType.Hand, EquipmentCategory.Weapon, physicalAttackBonus: 5,
        requiredClass: CharacterClass.Priest,
        longDescription: "A plain, flanged head on a wooden haft. Priests favor blunt weapons over an edge, and this one has seen its share of use.",
        size: Size.Medium, itemSize: ItemSize.Medium, attackType: AttackType.Crush, weaponType: WeaponType.Blunt, weight: 4.0);

    public static readonly Item HolyMace = new(
        "Holy Mace", '/', "A blessed mace radiating quiet warmth.",
        ItemType.Weapon, EquipmentType.Hand, EquipmentCategory.Weapon, physicalAttackBonus: 6,
        statModifiers: new Dictionary<PrimaryAttribute, int> { [PrimaryAttribute.Wisdom] = 1 },
        minimumLevel: 5, requiredClass: CharacterClass.Priest,
        longDescription: "A mace whose flanged head has been blessed and reblessed over generations of use. It's warm to the touch even in the coldest dungeon.",
        size: Size.Medium, itemSize: ItemSize.Medium, attackType: AttackType.Crush, weaponType: WeaponType.Blunt, weight: 4.25);

    public static readonly Item DwarvenWaraxe = new(
        "Dwarven Waraxe", '/', "A massive axe of dwarven make.",
        ItemType.Weapon, EquipmentType.Hand, EquipmentCategory.Weapon, physicalAttackBonus: 10,
        minimumLevel: 6, requiredRace: Race.Dwarf,
        longDescription: "A broad, double-bitted axe forged in a style no human smith has ever quite managed to copy. It's balanced for a grip stronger than it looks.",
        size: Size.Large, itemSize: ItemSize.Large, attackType: AttackType.Slash, weaponType: WeaponType.Axe, weight: 7.5);

    public static readonly Item ElvenBlade = new(
        "Elven Blade", '/', "A slender, impossibly light blade.",
        ItemType.Weapon, EquipmentType.Hand, EquipmentCategory.Weapon, physicalAttackBonus: 6,
        statModifiers: new Dictionary<PrimaryAttribute, int> { [PrimaryAttribute.Agility] = 1 },
        minimumLevel: 4, requiredRace: Race.Elf,
        longDescription: "A leaf-shaped blade so light it barely seems to weigh on the wrist, etched along its length with letters no human scholar has fully translated.",
        size: Size.Medium, itemSize: ItemSize.Medium, attackType: AttackType.Slash, weaponType: WeaponType.Sword, weight: 2.0);

    public static readonly Item ApprenticeWand = new(
        "Apprentice Wand", '|', "A wand given to those newly trained.",
        ItemType.Wand, EquipmentType.Hand, EquipmentCategory.Wand, magicalAttackBonus: 2,
        requiredClass: CharacterClass.Mage,
        longDescription: "A plain wand of polished ash, the kind handed to every apprentice on their first day. Unglamorous, but reliable.",
        size: Size.Small, itemSize: ItemSize.Small, weaponType: WeaponType.Wand, weight: 1.0);

    // --- More body/head armor, some class/race-flavored ---

    public static readonly Item PaddedArmor = new(
        "Padded Armor", '[', "Simple quilted armor.",
        ItemType.Armor, EquipmentType.Body, defenseBonus: 1,
        longDescription: "Layers of quilted cloth stitched into a serviceable vest. The cheapest protection worth wearing, and better than none.",
        size: Size.Small, itemSize: ItemSize.Medium, armorWeight: ArmorWeight.Medium, weight: 18.0);

    public static readonly Item ScaleMail = new(
        "Scale Mail", '[', "Overlapping metal scales.",
        ItemType.Armor, EquipmentType.Body, defenseBonus: 4,
        minimumLevel: 4,
        longDescription: "Rows of overlapping steel scales riveted to a leather backing. Flexible enough to move in, tough enough to matter.",
        size: Size.Medium, itemSize: ItemSize.Medium, armorWeight: ArmorWeight.Medium, weight: 32.0);

    public static readonly Item HalflingLeather = new(
        "Halfling Leather", '[', "Finely tailored leather armor.",
        ItemType.Armor, EquipmentType.Body, defenseBonus: 2,
        statModifiers: new Dictionary<PrimaryAttribute, int> { [PrimaryAttribute.Agility] = 1 },
        minimumLevel: 3, requiredRace: Race.Halfling,
        longDescription: "Supple leather tailored close to the body, cut in a style favored by Halfling scouts who'd rather not be seen at all.",
        size: Size.Small, itemSize: ItemSize.Medium, armorWeight: ArmorWeight.Medium, weight: 14.0);

    public static readonly Item MagesCap = new(
        "Mage's Cap", '^', "A simple cap worn by spellcasters.",
        ItemType.Armor, EquipmentType.Head, defenseBonus: 1,
        statModifiers: new Dictionary<PrimaryAttribute, int> { [PrimaryAttribute.Knowledge] = 1 },
        minimumLevel: 2, requiredClass: CharacterClass.Mage,
        longDescription: "A soft, unadorned cap that offers little physical protection, but seems to help keep a caster's thoughts from scattering mid-working.",
        size: Size.Small, itemSize: ItemSize.Small, armorWeight: ArmorWeight.Light, weight: 0.5);

    public static readonly Item WarriorsGreathelm = new(
        "Warrior's Greathelm", '^', "A heavy, enclosing helm.",
        ItemType.Armor, EquipmentType.Head, defenseBonus: 3,
        minimumLevel: 5, requiredClass: CharacterClass.Warrior,
        longDescription: "A full steel helm that covers the face down to the jaw. Vision suffers for it, but so does anything trying to land a solid hit.",
        size: Size.Large, itemSize: ItemSize.Small, armorWeight: ArmorWeight.Heavy, weight: 8.0);

    public static readonly Item ThiefsHood = new(
        "Thief's Hood", '^', "A dark, close-fitting hood.",
        ItemType.Armor, EquipmentType.Head, defenseBonus: 1,
        statModifiers: new Dictionary<PrimaryAttribute, int> { [PrimaryAttribute.Agility] = 1 },
        minimumLevel: 2, requiredClass: CharacterClass.Thief,
        longDescription: "A dark hood cut to keep both the face and footsteps quiet. Standard issue for anyone who prefers not to be noticed at all.",
        size: Size.Small, itemSize: ItemSize.Small, armorWeight: ArmorWeight.Light, weight: 0.75);

    // --- More rings, mirroring Ring of Strength for the other four primary attributes ---

    public static readonly Item RingOfAgility = new(
        "Ring of Agility", '=', "A band that seems to lighten the wearer's step.",
        ItemType.Armor, EquipmentType.Ring,
        statModifiers: new Dictionary<PrimaryAttribute, int> { [PrimaryAttribute.Agility] = 2 },
        longDescription: "A slender silver ring, cool to the touch. Wearing it seems to shave a fraction of a second off every reaction.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.1);

    public static readonly Item RingOfWisdom = new(
        "Ring of Wisdom", '=', "A band that quiets a restless mind.",
        ItemType.Armor, EquipmentType.Ring,
        statModifiers: new Dictionary<PrimaryAttribute, int> { [PrimaryAttribute.Wisdom] = 2 },
        longDescription: "A plain band set with a single dull stone. It doesn't glitter, but wearing it seems to make the right decision a little more obvious.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.1);

    public static readonly Item RingOfKnowledge = new(
        "Ring of Knowledge", '=', "A band engraved with tiny, precise script.",
        ItemType.Armor, EquipmentType.Ring,
        statModifiers: new Dictionary<PrimaryAttribute, int> { [PrimaryAttribute.Knowledge] = 2 },
        longDescription: "A copper ring engraved with script too small to read without squinting. Wearing it makes half-remembered facts easier to recall.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.1);

    // --- One more consumable ---

    public static readonly Item ElixirOfVitality = new(
        "Elixir of Vitality", '!', "Restores both health and mana.",
        ItemType.Consumable, charges: 1, healthRestore: 15, manaRestore: 15,
        longDescription: "A swirling two-toned draught, red and blue never quite mixing. Alchemists still argue over how it does both at once.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 1.0);

    // --- Higher-tier weapons per class -- the class-equipment restrictions mean each
    // class previously topped out around level 5-6 with nothing beyond it. Daggers stay
    // universal (every class allows Dagger), the rest lean class-restricted on purpose.

    public static readonly Item SilverDagger = new(
        "Silver Dagger", '/', "A dagger with a silvered edge.",
        ItemType.Weapon, EquipmentType.Hand, EquipmentCategory.Weapon, physicalAttackBonus: 3,
        statModifiers: new Dictionary<PrimaryAttribute, int> { [PrimaryAttribute.Wisdom] = 1 },
        minimumLevel: 2,
        longDescription: "A slim blade plated in silver, favored by those who hunt things that an ordinary edge won't stop. Every class finds a use for a good dagger.",
        size: Size.Small, itemSize: ItemSize.Small, attackType: AttackType.Pierce, weaponType: WeaponType.Dagger, weight: 1.0);

    public static readonly Item AssassinsDagger = new(
        "Assassin's Dagger", '/', "A wickedly balanced blade.",
        ItemType.Weapon, EquipmentType.Hand, EquipmentCategory.Weapon, physicalAttackBonus: 8,
        statModifiers: new Dictionary<PrimaryAttribute, int> { [PrimaryAttribute.Agility] = 1 },
        minimumLevel: 7, requiredClass: CharacterClass.Thief,
        longDescription: "Weighted for a throw or a thrust with equal ease. Its previous owner never saw it coming, which is rather the point.",
        size: Size.Small, itemSize: ItemSize.Small, attackType: AttackType.Pierce, weaponType: WeaponType.Dagger, weight: 1.0);

    public static readonly Item TwinFangDagger = new(
        "Twin Fang Dagger", '/', "A dagger forged with two parallel edges.",
        ItemType.Weapon, EquipmentType.Hand, EquipmentCategory.Weapon, physicalAttackBonus: 9,
        minimumLevel: 9,
        longDescription: "Two blades fused into one, said to bite twice for every thrust. A fine weapon for any class disciplined enough to wield it well.",
        size: Size.Small, itemSize: ItemSize.Small, attackType: AttackType.Pierce, weaponType: WeaponType.Dagger, weight: 1.25);

    public static readonly Item Broadsword = new(
        "Broadsword", '/', "A wide, versatile blade.",
        ItemType.Weapon, EquipmentType.Hand, EquipmentCategory.Weapon, physicalAttackBonus: 8,
        minimumLevel: 6,
        longDescription: "A broad, honest blade with no tricks to it -- just a solid edge and enough weight to make every swing count.",
        size: Size.Large, itemSize: ItemSize.Large, attackType: AttackType.Slash, weaponType: WeaponType.Sword, weight: 5.0);

    public static readonly Item BladeOfTheVanguard = new(
        "Blade of the Vanguard", '/', "A sword carried at the front of every charge.",
        ItemType.Weapon, EquipmentType.Hand, EquipmentCategory.Weapon, physicalAttackBonus: 13,
        statModifiers: new Dictionary<PrimaryAttribute, int> { [PrimaryAttribute.Strength] = 1 },
        minimumLevel: 10, requiredClass: CharacterClass.Warrior,
        longDescription: "A heavy longsword carried by whoever leads the charge, notched along its length by armor that didn't hold.",
        size: Size.Large, itemSize: ItemSize.Large, attackType: AttackType.Slash, weaponType: WeaponType.Sword, weight: 6.0);

    public static readonly Item ExecutionersAxe = new(
        "Executioner's Axe", '/', "A massive, single-bladed axe.",
        ItemType.Weapon, EquipmentType.Hand, EquipmentCategory.Weapon, physicalAttackBonus: 14,
        minimumLevel: 8, requiredClass: CharacterClass.Warrior,
        longDescription: "A broad-headed axe heavy enough that only sustained training keeps it from overbalancing its wielder. It ends fights quickly.",
        size: Size.Large, itemSize: ItemSize.VeryLarge, attackType: AttackType.Slash, weaponType: WeaponType.Axe, weight: 8.0);

    public static readonly Item GreatWarhammer = new(
        "Great Warhammer", '/', "An immense, two-handed hammer.",
        ItemType.Weapon, EquipmentType.Hand, EquipmentCategory.Weapon, physicalAttackBonus: 15,
        minimumLevel: 9, requiredClass: CharacterClass.Warrior,
        longDescription: "A hammer so large that most who lift it can barely swing it twice before their arms give out. A Warrior isn't most people.",
        size: Size.Large, itemSize: ItemSize.VeryLarge, attackType: AttackType.Crush, weaponType: WeaponType.Blunt, weight: 9.0);

    public static readonly Item TemplarsMace = new(
        "Templar's Mace", '/', "A mace carried by militant clergy.",
        ItemType.Weapon, EquipmentType.Hand, EquipmentCategory.Weapon, physicalAttackBonus: 9,
        statModifiers: new Dictionary<PrimaryAttribute, int> { [PrimaryAttribute.Wisdom] = 2 },
        minimumLevel: 8, requiredClass: CharacterClass.Priest,
        longDescription: "A flanged mace engraved with a militant order's sigil. Its wielders draw no blood, but they leave no doubt either.",
        size: Size.Medium, itemSize: ItemSize.Medium, attackType: AttackType.Crush, weaponType: WeaponType.Blunt, weight: 4.5);

    public static readonly Item JourneymansWand = new(
        "Journeyman's Wand", '|', "A wand carried past apprenticeship.",
        ItemType.Wand, EquipmentType.Hand, EquipmentCategory.Wand, magicalAttackBonus: 4,
        minimumLevel: 4, requiredClass: CharacterClass.Mage,
        longDescription: "A wand of seasoned yew, its core no longer raw and unstable. It responds faster than the apprentice model it replaced.",
        size: Size.Small, itemSize: ItemSize.Small, weaponType: WeaponType.Wand, weight: 1.25);

    public static readonly Item ArchmagesWand = new(
        "Archmage's Wand", '|', "A wand carried by a master of the craft.",
        ItemType.Wand, EquipmentType.Hand, EquipmentCategory.Wand, magicalAttackBonus: 8,
        statModifiers: new Dictionary<PrimaryAttribute, int> { [PrimaryAttribute.Knowledge] = 2 },
        minimumLevel: 10, requiredClass: CharacterClass.Mage,
        longDescription: "A wand of dense blackwood bound in silver wire, carried by those who no longer need to think about the words they're saying.",
        size: Size.Small, itemSize: ItemSize.Small, weaponType: WeaponType.Wand, weight: 1.5);

    // --- More body armor -- Heavy in particular only had one entry (Plate Armor) total ---

    public static readonly Item ClothTunic = new(
        "Cloth Tunic", '[', "A simple, unarmored tunic.",
        ItemType.Armor, EquipmentType.Body, defenseBonus: 1,
        longDescription: "Plain woven cloth, offering almost nothing in the way of protection. Better than fighting bare-chested, and not by much more.",
        size: Size.Small, itemSize: ItemSize.Medium, armorWeight: ArmorWeight.Light, weight: 3.0);

    public static readonly Item StuddedLeather = new(
        "Studded Leather", '[', "Leather reinforced with metal studs.",
        ItemType.Armor, EquipmentType.Body, defenseBonus: 2,
        minimumLevel: 2,
        longDescription: "Boiled leather set with rows of iron studs. Heavier than plain leather, but still light enough not to slow anyone down.",
        size: Size.Small, itemSize: ItemSize.Medium, armorWeight: ArmorWeight.Medium, weight: 16.0);

    public static readonly Item ReinforcedChainmail = new(
        "Reinforced Chainmail", '[', "Chainmail doubled over at the vitals.",
        ItemType.Armor, EquipmentType.Body, defenseBonus: 5,
        minimumLevel: 6,
        longDescription: "A shirt of fine rings doubled over the chest and back. Heavier than standard chainmail, and it shows in what it turns aside.",
        size: Size.Medium, itemSize: ItemSize.Medium, armorWeight: ArmorWeight.Medium, weight: 42.0);

    public static readonly Item FullPlateArmor = new(
        "Full Plate Armor", '[', "A complete suit of articulated plate.",
        ItemType.Armor, EquipmentType.Body, defenseBonus: 8,
        minimumLevel: 9, requiredClass: CharacterClass.Warrior,
        longDescription: "Fitted plate covering nearly every inch, articulated at every joint by a master armorer. Nothing short of it stops a killing blow this well.",
        size: Size.Large, itemSize: ItemSize.Large, armorWeight: ArmorWeight.Heavy, weight: 75.0);

    public static readonly Item ArchmagesRobe = new(
        "Archmage's Robe", '[', "A robe worn by masters of the arcane.",
        ItemType.Armor, EquipmentType.Body, defenseBonus: 2,
        statModifiers: new Dictionary<PrimaryAttribute, int> { [PrimaryAttribute.Knowledge] = 2 },
        minimumLevel: 7, requiredClass: CharacterClass.Mage,
        longDescription: "A midnight-blue robe embroidered with constellations that don't quite match the sky outside. Every fold of it hums faintly.",
        size: Size.Small, itemSize: ItemSize.Medium, armorWeight: ArmorWeight.Light, weight: 5.0);

    public static readonly Item ShadowweaveArmor = new(
        "Shadowweave Armor", '[', "Armor woven from unnaturally dark thread.",
        ItemType.Armor, EquipmentType.Body, defenseBonus: 3,
        statModifiers: new Dictionary<PrimaryAttribute, int> { [PrimaryAttribute.Agility] = 1 },
        minimumLevel: 6, requiredClass: CharacterClass.Thief,
        longDescription: "Dark, close-fitting armor woven from thread that seems to drink in the light around it. Silent, and built to stay that way.",
        size: Size.Small, itemSize: ItemSize.Medium, armorWeight: ArmorWeight.Light, weight: 6.0);

    public static readonly Item BlessedChainmail = new(
        "Blessed Chainmail", '[', "Chainmail consecrated by a temple.",
        ItemType.Armor, EquipmentType.Body, defenseBonus: 5,
        statModifiers: new Dictionary<PrimaryAttribute, int> { [PrimaryAttribute.Wisdom] = 1 },
        minimumLevel: 7, requiredClass: CharacterClass.Priest,
        longDescription: "A chainmail shirt consecrated link by link in a week-long rite. Its wearer feels steadier for it, in ways hard to put into words.",
        size: Size.Medium, itemSize: ItemSize.Medium, armorWeight: ArmorWeight.Medium, weight: 38.0);

    public static readonly Item DwarvenPlatemail = new(
        "Dwarven Platemail", '[', "Plate armor of dwarven make.",
        ItemType.Armor, EquipmentType.Body, defenseBonus: 7,
        statModifiers: new Dictionary<PrimaryAttribute, int> { [PrimaryAttribute.Constitution] = 1 },
        minimumLevel: 8, requiredRace: Race.Dwarf,
        longDescription: "Plate forged in the deep holds, fitted with a precision no surface smith has matched. It's heavier than it looks, and looks plenty heavy.",
        size: Size.Large, itemSize: ItemSize.Large, armorWeight: ArmorWeight.Heavy, weight: 68.0);

    // --- More shields -- there was exactly one in the entire catalog ---

    public static readonly Item RoundShield = new(
        "Round Shield", ')', "A small, maneuverable shield.",
        ItemType.Armor, EquipmentType.Hand, EquipmentCategory.Shield, defenseBonus: 2,
        minimumLevel: 1,
        longDescription: "A small round shield, light enough to swing into place quickly. It won't stop everything, but it stops enough.",
        size: Size.Small, itemSize: ItemSize.Medium, weight: 6.0);

    public static readonly Item IronShield = new(
        "Iron Shield", ')', "A solid iron-banded shield.",
        ItemType.Armor, EquipmentType.Hand, EquipmentCategory.Shield, defenseBonus: 3,
        minimumLevel: 4,
        longDescription: "A round shield banded in iron rather than merely faced with it. Heavier than wood, and considerably harder to split.",
        size: Size.Medium, itemSize: ItemSize.Medium, weight: 12.0);

    public static readonly Item TowerShield = new(
        "Tower Shield", ')', "A massive shield covering nearly the whole body.",
        ItemType.Armor, EquipmentType.Hand, EquipmentCategory.Shield, defenseBonus: 6,
        minimumLevel: 8, requiredClass: CharacterClass.Warrior,
        longDescription: "A shield tall enough to crouch behind entirely. Slow to maneuver, but nothing gets through it by accident.",
        size: Size.Large, itemSize: ItemSize.Large, weight: 20.0);

    // --- More accessories across several previously-thin slots ---

    public static readonly Item RingOfFortitude = new(
        "Ring of Fortitude", '=', "A thick band that steadies the body.",
        ItemType.Armor, EquipmentType.Ring,
        statModifiers: new Dictionary<PrimaryAttribute, int> { [PrimaryAttribute.Constitution] = 2 },
        minimumLevel: 5,
        longDescription: "A heavy iron band, plain but for a single word inscribed inside it in a language no one currently living can read.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.1);

    public static readonly Item BootsOfSwiftness = new(
        "Boots of Swiftness", '{', "Boots that seem to lighten every step.",
        ItemType.Armor, EquipmentType.Feet,
        statModifiers: new Dictionary<PrimaryAttribute, int> { [PrimaryAttribute.Agility] = 1 },
        minimumLevel: 4,
        longDescription: "Supple boots that seem to know where the ground is before your foot lands. Any class can appreciate not stumbling in the dark.",
        size: Size.Small, itemSize: ItemSize.Small, armorWeight: ArmorWeight.Light, weight: 1.0);

    public static readonly Item GauntletsOfPower = new(
        "Gauntlets of Power", '{', "Heavy gauntlets that steady the grip.",
        ItemType.Armor, EquipmentType.Gloves,
        statModifiers: new Dictionary<PrimaryAttribute, int> { [PrimaryAttribute.Strength] = 1 },
        minimumLevel: 6, requiredClass: CharacterClass.Warrior,
        longDescription: "Reinforced steel gauntlets that add real weight to every blow. They're not subtle, and they were never meant to be.",
        size: Size.Medium, itemSize: ItemSize.Small, armorWeight: ArmorWeight.Heavy, weight: 4.0);

    public static readonly Item CircletOfInsight = new(
        "Circlet of Insight", '^', "A thin circlet worn at the brow.",
        ItemType.Armor, EquipmentType.Head, defenseBonus: 1,
        statModifiers: new Dictionary<PrimaryAttribute, int> { [PrimaryAttribute.Knowledge] = 1 },
        minimumLevel: 6, requiredClass: CharacterClass.Mage,
        longDescription: "A slender silver circlet that sits cold against the brow. Thoughts seem to arrange themselves more easily while wearing it.",
        size: Size.Small, itemSize: ItemSize.Small, armorWeight: ArmorWeight.Light, weight: 0.5);

    public static readonly Item AmuletOfTheDevout = new(
        "Amulet of the Devout", '"', "An amulet worn by the faithful.",
        ItemType.Armor, EquipmentType.Neck, defenseBonus: 1,
        statModifiers: new Dictionary<PrimaryAttribute, int> { [PrimaryAttribute.Wisdom] = 1 },
        minimumLevel: 6, requiredClass: CharacterClass.Priest,
        longDescription: "A weathered amulet passed down through a long line of the faithful, each of whom wore it a little smoother than the last.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.2);

    // --- A few more consumables across the level range -- the original five were all level 0 ---

    public static readonly Item MinorElixir = new(
        "Minor Elixir", '!', "A weak but useful dual-restore draught.",
        ItemType.Consumable, charges: 1, healthRestore: 20, manaRestore: 5,
        minimumLevel: 1,
        longDescription: "A cheap, slightly bitter draught brewed in bulk for adventurers who can't yet afford anything finer. It works well enough.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.5);

    public static readonly Item SuperiorHealthPotion = new(
        "Superior Health Potion", '!', "Restores a great deal of health.",
        ItemType.Consumable, charges: 1, healthRestore: 40,
        minimumLevel: 5,
        longDescription: "A dense, near-black liquid that burns going down. Alchemists reserve the name 'superior' for potions that actually deserve it.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 1.0);

    public static readonly Item SuperiorManaPotion = new(
        "Superior Mana Potion", '!', "Restores a great deal of mana.",
        ItemType.Consumable, charges: 1, manaRestore: 40,
        minimumLevel: 5,
        longDescription: "A vial of mana so concentrated it seems to shimmer under its own light. A single dose can turn a losing fight around.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 1.0);

    public static readonly Item PotionOfRenewal = new(
        "Potion of Renewal", '!', "Fully restores both health and mana.",
        ItemType.Consumable, charges: 1, healthRestore: 30, manaRestore: 30,
        minimumLevel: 8,
        longDescription: "A rare draught brewed only by the most patient alchemists. It doesn't discriminate between what ails you -- it simply mends it.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 1.0);

    // --- Elixirs of Burden -- temporary max-carry-capacity boosts, mirroring Bull's Strength
    // (SpellCatalog.BullsStrength) for classes without it, or for stacking a bigger haul
    // than the spell alone allows. See EncumbranceCalculator/Stat.CarryCapacity.

    public static readonly Item MinorPotionOfStrength = new(
        "Minor Potion of Strength", '!', "Temporarily raises carry capacity by 10 lbs.",
        ItemType.Consumable, charges: 1,
        minimumLevel: 1, carryCapacityBonus: 10, carryCapacityBonusDuration: 50,
        longDescription: "A faintly metallic tonic that puts iron in your grip for a while. The effect fades well before the aftertaste does.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.5);

    public static readonly Item PotionOfStrength = new(
        "Potion of Strength", '!', "Temporarily raises carry capacity by 20 lbs.",
        ItemType.Consumable, charges: 1,
        minimumLevel: 3, carryCapacityBonus: 20, carryCapacityBonusDuration: 100,
        longDescription: "A thick, gritty draught that settles into the muscles like a second wind. Adventurers who loot too enthusiastically keep a few on hand.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.5);

    public static readonly Item MajorPotionOfStrength = new(
        "Major Potion of Strength", '!', "Temporarily raises carry capacity by 40 lbs.",
        ItemType.Consumable, charges: 1,
        minimumLevel: 6, carryCapacityBonus: 40, carryCapacityBonusDuration: 150,
        longDescription: "A potent, foul-smelling brew that alchemists warn against drinking on an empty stomach. For a while, nothing feels heavy.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.75);

    // --- Basic and improved cloth/silk pieces for every worn-armor slot -- Light weight,
    // universal (no class/race restriction). Needed once Leather/Chain/Padded/Iron/Plate
    // became Medium/Heavy across the board: without these, a Light-only class (Mage,
    // Thief) would have nothing at all to wear in most slots besides class-exclusive gear.

    public static readonly Item ClothCap = new(
        "Cloth Cap", '^', "A simple cloth cap.",
        ItemType.Armor, EquipmentType.Head, defenseBonus: 1,
        longDescription: "A soft cap of plain woven cloth. It won't turn a real blow, but it keeps the rain off.",
        size: Size.Small, itemSize: ItemSize.Small, armorWeight: ArmorWeight.Light, weight: 0.5);

    public static readonly Item SilkCap = new(
        "Silk Cap", '^', "A finely woven silk cap.",
        ItemType.Armor, EquipmentType.Head, defenseBonus: 2,
        minimumLevel: 4,
        longDescription: "A close-fitting cap of fine silk, stitched tighter and more carefully than its cloth cousin. Still won't stop a blade, but stops more than nothing.",
        size: Size.Small, itemSize: ItemSize.Small, armorWeight: ArmorWeight.Light, weight: 0.5);

    public static readonly Item ClothGloves = new(
        "Cloth Gloves", '{', "Simple cloth gloves.",
        ItemType.Armor, EquipmentType.Gloves, defenseBonus: 1,
        longDescription: "Thin woven gloves, more about keeping hands warm than safe. They don't get in the way of anything.",
        size: Size.Small, itemSize: ItemSize.Small, armorWeight: ArmorWeight.Light, weight: 0.25);

    public static readonly Item SilkGloves = new(
        "Silk Gloves", '{', "Finely woven silk gloves.",
        ItemType.Armor, EquipmentType.Gloves, defenseBonus: 2,
        minimumLevel: 4,
        longDescription: "Smooth silk gloves that don't dull the fingertips at all -- ideal for anyone whose work depends on a light touch.",
        size: Size.Small, itemSize: ItemSize.Small, armorWeight: ArmorWeight.Light, weight: 0.25);

    public static readonly Item ClothShoes = new(
        "Cloth Shoes", '{', "Simple cloth shoes.",
        ItemType.Armor, EquipmentType.Feet, defenseBonus: 1,
        longDescription: "Soft-soled shoes of plain cloth and thin leather trim. Quiet, comfortable, and offering almost no protection at all.",
        size: Size.Small, itemSize: ItemSize.Small, armorWeight: ArmorWeight.Light, weight: 0.5);

    public static readonly Item SilkSlippers = new(
        "Silk Slippers", '{', "Finely woven silk slippers.",
        ItemType.Armor, EquipmentType.Feet, defenseBonus: 2,
        minimumLevel: 4,
        longDescription: "Slippers of fine silk, better suited to a quiet study than a dungeon floor -- but they hold up better than they look like they should.",
        size: Size.Small, itemSize: ItemSize.Small, armorWeight: ArmorWeight.Light, weight: 0.4);

    public static readonly Item ClothWraps = new(
        "Cloth Wraps", '(', "Simple cloth wrist wraps.",
        ItemType.Armor, EquipmentType.Wrists, defenseBonus: 1,
        longDescription: "Long strips of cloth wound tight around the wrists. Common among anyone who works with their hands and can't afford better.",
        size: Size.Small, itemSize: ItemSize.Small, armorWeight: ArmorWeight.Light, weight: 0.2);

    public static readonly Item SilkWristwraps = new(
        "Silk Wristwraps", '(', "Finely woven silk wrist wraps.",
        ItemType.Armor, EquipmentType.Wrists, defenseBonus: 2,
        minimumLevel: 4,
        longDescription: "Silk wound in careful layers around the wrist, light enough that most forget they're wearing it at all.",
        size: Size.Small, itemSize: ItemSize.Small, armorWeight: ArmorWeight.Light, weight: 0.2);

    public static readonly Item ClothSleeves = new(
        "Cloth Sleeves", '}', "Simple cloth sleeves.",
        ItemType.Armor, EquipmentType.Arms, defenseBonus: 1,
        longDescription: "Loose cloth sleeves, worn more for warmth and modesty than protection. They don't restrict movement in the slightest.",
        size: Size.Small, itemSize: ItemSize.Medium, armorWeight: ArmorWeight.Light, weight: 0.3);

    public static readonly Item SilkSleeves = new(
        "Silk Sleeves", '}', "Finely woven silk sleeves.",
        ItemType.Armor, EquipmentType.Arms, defenseBonus: 2,
        minimumLevel: 4,
        longDescription: "Fine silk sleeves that catch the light strangely. Purely decorative to most eyes, though they hold up surprisingly well.",
        size: Size.Small, itemSize: ItemSize.Medium, armorWeight: ArmorWeight.Light, weight: 0.3);

    public static readonly Item ClothLeggings = new(
        "Cloth Leggings", ']', "Simple cloth leggings.",
        ItemType.Armor, EquipmentType.Legs, defenseBonus: 1,
        longDescription: "Plain cloth leggings, loose enough to move freely in. They wouldn't stop a determined blow, but most blows aren't that determined.",
        size: Size.Small, itemSize: ItemSize.Medium, armorWeight: ArmorWeight.Light, weight: 0.75);

    public static readonly Item SilkLeggings = new(
        "Silk Leggings", ']', "Finely woven silk leggings.",
        ItemType.Armor, EquipmentType.Legs, defenseBonus: 2,
        minimumLevel: 4,
        longDescription: "Silk leggings worn under (or instead of) heavier armor, prized for how little they weigh on a long march.",
        size: Size.Small, itemSize: ItemSize.Medium, armorWeight: ArmorWeight.Light, weight: 0.75);

    // --- Skeleton Key -- unlocks any Door (Dungeon/Door.cs) regardless of its
    // IsPickable/IsBashable flags, consuming one charge per use. Deliberately NOT added to
    // BaseItems/Items.All -- it should never show up via the general weighted floor-item/loot
    // draw, only via DungeonGenerator.SpawnSkeletonKeys, which guarantees one on every floor
    // that has at least one locked Door, so no class/skill combination is ever soft-locked.
    // Stackable via Charges -- the same mechanism Lockpicks already use -- so accumulating one
    // from several floors without using them shows as one growing count instead of several
    // identical inventory entries. See GameLoop.HandlePickUp/PickUpAll's stacking branch and
    // UseSkeletonKeyOnDoor's decrement-not-remove logic. isSkeletonKey (and, redundantly but
    // explicitly, canBeLost: false) both mark it as the game's one truly irreplaceable item --
    // see ItemLossRules.CanBeLost.

    public static readonly Item SkeletonKey = new(
        "Skeleton Key", '`', "An old iron key that seems to fit any lock.",
        ItemType.Key, charges: 1,
        longDescription: "A worn iron key, its teeth filed down to an approximation of every lock at once. It crumbles to dust the moment its last use is spent.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.2, isSkeletonKey: true, canBeLost: false);

    // --- Lockpicks -- required (alongside SkillCatalog.PickLock) to attempt picking any
    // locked Door or Chest; see GameLoop.PickDoorLock/HandleOpenChest. Charges start
    // at 6 ("a set of six"); a successful pick never costs a charge, only a failed attempt
    // has a chance to snap one (GameLoop.BreakLockpickOnFailure). Unlike SkeletonKey this IS
    // in BaseItems/Items.All -- it's an ordinary, repeatable floor/loot find, not a
    // guaranteed-once spawn, and HandlePickUp tops up an already-carried set by 6 instead of
    // adding a second separate inventory entry when another is found. Thief-only via
    // requiredClass -- redundant with PickLock already being a Thief-only skill for the
    // actual picking action, but this also makes InventoryScreen's "u" (use) flow correctly
    // explain why another class can't use a found set, instead of the generic
    // "isn't used from here" message.
    public static readonly Item Lockpicks = new(
        "Lockpicks", ';', "A set of six slender picks for working a lock's tumblers.",
        ItemType.Lockpick, charges: 6,
        longDescription: "A leather roll holding six slender iron picks, thin enough to slip past a lock's tumblers -- and thin enough to snap if you push your luck.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.3, requiredClass: CharacterClass.Thief);

    // --- Ranged weapons, ammunition, and thrown weapons -- see Core/ProjectileEngine.cs.
    // Ammo is modeled as an ItemType.Ammunition stack via the existing Charges field, the same
    // mechanism Lockpicks already use for "how many left" -- no new stacking system needed.

    public static readonly Item Bow = new(
        "Bow", ')', "A curved wooden bow. Fires Arrows.",
        ItemType.Weapon, EquipmentType.Ranged, EquipmentCategory.Weapon, physicalAttackBonus: 3,
        longDescription: "A supple longbow of well-seasoned yew, strung and ready. Useless without a steady supply of arrows.",
        size: Size.Medium, itemSize: ItemSize.Medium, attackType: AttackType.Pierce, weaponType: WeaponType.Bow, weight: 3.0,
        requiredAmmunitionType: AmmunitionType.Arrow, projectileRange: 8);

    public static readonly Item Crossbow = new(
        "Crossbow", ')', "A mechanical bow. Fires Bolts.",
        ItemType.Weapon, EquipmentType.Ranged, EquipmentCategory.Weapon, physicalAttackBonus: 5,
        minimumLevel: 2,
        longDescription: "A steel-limbed crossbow with a hand crank. Slower to nock, but its bolts hit far harder than any arrow.",
        size: Size.Medium, itemSize: ItemSize.Medium, attackType: AttackType.Pierce, weaponType: WeaponType.Crossbow, weight: 5.0,
        requiredAmmunitionType: AmmunitionType.Bolt, projectileRange: 10);

    public static readonly Item Sling = new(
        "Sling", ')', "A simple leather sling. Fires Sling Stones (or Rocks, with a penalty).",
        ItemType.Weapon, EquipmentType.Ranged, EquipmentCategory.Weapon, physicalAttackBonus: 1,
        longDescription: "A worn leather cradle on a pair of cords -- crude, but a well-practiced arm can put a stone through an eye socket at range.",
        size: Size.Small, itemSize: ItemSize.Small, attackType: AttackType.Crush, weaponType: WeaponType.Sling, weight: 0.5,
        requiredAmmunitionType: AmmunitionType.SlingStone, projectileRange: 6);

    /// <summary>Short, thrusting-and-throwing spear -- ThrowableCapability.MeleeOrProjectile, stackable while readied as a throwable (see EquipmentCompatibility.GetCompatibleSlots(Item), which prefers Ammo/Thrown by default for exactly this reason). Lower melee damage and a longer throw than LongSpear.</summary>
    public static readonly Item ShortSpear = new(
        "Short Spear", '/', "A light, well-balanced spear. Can be thrown or fought with in melee.",
        ItemType.Weapon, EquipmentType.Hand, EquipmentCategory.Weapon, physicalAttackBonus: 3,
        longDescription: "A short ashwood haft tipped with a narrow steel point -- light enough to carry several and throw one after another.",
        size: Size.Medium, itemSize: ItemSize.Medium, attackType: AttackType.Pierce, weaponType: WeaponType.Spear, weight: 2.5,
        canBeThrown: true, projectileRange: 6, throwableCategory: ThrowableCategory.Martial, charges: 1,
        throwableCapability: ThrowableCapability.MeleeOrProjectile,
        breakChanceOnCreatureHit: 0.06, breakChanceOnWallImpact: 0.15, breakChanceOnOrdinaryLanding: 0.02);

    /// <summary>Long, two-handed-feeling thrusting spear -- ThrowableCapability.MeleeOrProjectile, carried and equipped individually rather than stacked (see EquipmentCompatibility.GetCompatibleSlots(Item), which prefers a hand slot by default for exactly this reason). Higher melee damage and a shorter throw than ShortSpear.</summary>
    public static readonly Item LongSpear = new(
        "Long Spear", '/', "A long, heavy spear. Can be thrown or fought with in melee.",
        ItemType.Weapon, EquipmentType.Hand, EquipmentCategory.Weapon, physicalAttackBonus: 6,
        longDescription: "A stout ashwood haft half again as long as a Short Spear, tipped with a broad leaf-shaped head -- devastating planted in a foe's ribs, unwieldy to throw more than once.",
        size: Size.Large, itemSize: ItemSize.Large, attackType: AttackType.Pierce, weaponType: WeaponType.Spear, weight: 5.0,
        canBeThrown: true, projectileRange: 4, throwableCategory: ThrowableCategory.Martial,
        throwableCapability: ThrowableCapability.MeleeOrProjectile,
        breakChanceOnCreatureHit: 0.10, breakChanceOnWallImpact: 0.25, breakChanceOnOrdinaryLanding: 0.04);

    public static readonly Item Arrow = new(
        "Arrow", '\'', "Fletched ammunition for a Bow. 20 in this bundle.",
        ItemType.Ammunition, EquipmentType.AmmoThrown, charges: 20,
        longDescription: "A bundle of straight-grained shafts fletched with grey goose feathers, tipped in honest iron.",
        size: Size.Small, itemSize: ItemSize.Small, weight: 1.0, ammunitionType: AmmunitionType.Arrow,
        throwableCapability: ThrowableCapability.ProjectileOnly,
        breakChanceOnCreatureHit: 0.15, breakChanceOnWallImpact: 0.35, breakChanceOnOrdinaryLanding: 0.05);

    public static readonly Item FireArrow = new(
        "Fire Arrow", '\'', "An arrow whose head is wrapped in oil-soaked cloth.",
        ItemType.Ammunition, EquipmentType.AmmoThrown, charges: 12,
        statusEffects: new[] { new ItemStatusEffect(ItemEffectType.Fire, chance: 0.5, magnitude: 4) },
        longDescription: "The head of each shaft is bound in oil-soaked cloth, ready to catch light the instant it's loosed.",
        size: Size.Small, itemSize: ItemSize.Small, weight: 1.0, ammunitionType: AmmunitionType.Arrow,
        throwableCapability: ThrowableCapability.ProjectileOnly,
        breakChanceOnCreatureHit: 0.15, breakChanceOnWallImpact: 0.35, breakChanceOnOrdinaryLanding: 0.05);

    public static readonly Item FrostArrow = new(
        "Frost Arrow", '\'', "An arrow etched with frost-rimed runes.",
        ItemType.Ammunition, EquipmentType.AmmoThrown, charges: 12,
        statusEffects: new[] { new ItemStatusEffect(ItemEffectType.Frost, chance: 0.5, magnitude: 3, duration: 2) },
        longDescription: "Faint blue runes creep along each shaft, cold enough to numb the fingers that nock them.",
        size: Size.Small, itemSize: ItemSize.Small, weight: 1.0, ammunitionType: AmmunitionType.Arrow,
        throwableCapability: ThrowableCapability.ProjectileOnly,
        breakChanceOnCreatureHit: 0.15, breakChanceOnWallImpact: 0.35, breakChanceOnOrdinaryLanding: 0.05);

    public static readonly Item PoisonArrow = new(
        "Poison Arrow", '\'', "An arrow with a dark, oily residue on its head.",
        ItemType.Ammunition, EquipmentType.AmmoThrown, charges: 12,
        statusEffects: new[] { new ItemStatusEffect(ItemEffectType.Poison, chance: 0.35, magnitude: 3, duration: 3) },
        longDescription: "A sickly film clings to the head of each shaft. A shallow hit is rarely just a shallow hit.",
        size: Size.Small, itemSize: ItemSize.Small, weight: 1.0, ammunitionType: AmmunitionType.Arrow,
        throwableCapability: ThrowableCapability.ProjectileOnly,
        breakChanceOnCreatureHit: 0.15, breakChanceOnWallImpact: 0.35, breakChanceOnOrdinaryLanding: 0.05);

    public static readonly Item HolyArrow = new(
        "Holy Arrow", '\'', "An arrow inscribed with a faintly glowing ward.",
        ItemType.Ammunition, EquipmentType.AmmoThrown, charges: 10,
        longDescription: "Each shaft bears a single inscribed ward, warm to the touch. Undead flesh recoils from wounds it leaves behind.",
        size: Size.Small, itemSize: ItemSize.Small, weight: 1.0, ammunitionType: AmmunitionType.Arrow,
        isIdentified: false, isBlessed: true, blessedUndeadDamageBonus: 4,
        throwableCapability: ThrowableCapability.ProjectileOnly,
        breakChanceOnCreatureHit: 0.15, breakChanceOnWallImpact: 0.35, breakChanceOnOrdinaryLanding: 0.05);

    public static readonly Item Bolt = new(
        "Bolt", '\'', "Heavy ammunition for a Crossbow. 20 in this case.",
        ItemType.Ammunition, EquipmentType.AmmoThrown, charges: 20, physicalAttackBonus: 2,
        longDescription: "A case of short, heavy steel-tipped bolts, built for a crossbow's stronger draw.",
        size: Size.Small, itemSize: ItemSize.Small, weight: 1.5, ammunitionType: AmmunitionType.Bolt,
        throwableCapability: ThrowableCapability.ProjectileOnly,
        breakChanceOnCreatureHit: 0.12, breakChanceOnWallImpact: 0.30, breakChanceOnOrdinaryLanding: 0.04);

    public static readonly Item HeavyBolt = new(
        "Heavy Bolt", '\'', "An oversized bolt built for maximum penetration.",
        ItemType.Ammunition, EquipmentType.AmmoThrown, charges: 12, physicalAttackBonus: 4,
        longDescription: "Thicker and heavier than a standard bolt, sacrificing range for the kind of impact that punches through armor.",
        size: Size.Small, itemSize: ItemSize.Small, weight: 1.5, ammunitionType: AmmunitionType.Bolt,
        throwableCapability: ThrowableCapability.ProjectileOnly,
        breakChanceOnCreatureHit: 0.12, breakChanceOnWallImpact: 0.30, breakChanceOnOrdinaryLanding: 0.04);

    public static readonly Item SilverBolt = new(
        "Silver Bolt", '\'', "A bolt cast from solid silver.",
        ItemType.Ammunition, EquipmentType.AmmoThrown, charges: 10, physicalAttackBonus: 2,
        longDescription: "A case of bolts cast from pure silver rather than steel -- softer, but murder on anything that shouldn't still be walking.",
        size: Size.Small, itemSize: ItemSize.Small, weight: 1.5, ammunitionType: AmmunitionType.Bolt,
        isIdentified: false, isBlessed: true, blessedUndeadDamageBonus: 5,
        throwableCapability: ThrowableCapability.ProjectileOnly,
        breakChanceOnCreatureHit: 0.12, breakChanceOnWallImpact: 0.30, breakChanceOnOrdinaryLanding: 0.04);

    public static readonly Item SlingStone = new(
        "Sling Stone", '*', "A smooth, hand-shaped stone made for a sling.",
        ItemType.Ammunition, EquipmentType.AmmoThrown, charges: 15,
        longDescription: "A stone deliberately shaped and smoothed to fly true from a sling's cradle -- no penalty finding its mark, unlike an ordinary rock.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.3, ammunitionType: AmmunitionType.SlingStone,
        throwableCapability: ThrowableCapability.ProjectileOnly);

    public static readonly Item ThrowingKnife = new(
        "Throwing Knife", '/', "A balanced blade meant to be thrown, not swung.",
        ItemType.Weapon, EquipmentType.AmmoThrown, EquipmentCategory.Weapon, physicalAttackBonus: 2,
        longDescription: "A slim, perfectly balanced blade forged to fly true -- unlike a Short Spear, it was never meant to be fought with in the hand.",
        size: Size.Small, itemSize: ItemSize.Small, attackType: AttackType.Pierce, weaponType: WeaponType.Dagger, weight: 0.5,
        canBeThrown: true, projectileRange: 5, throwableCategory: ThrowableCategory.Martial, charges: 1,
        throwableCapability: ThrowableCapability.ProjectileOnly,
        breakChanceOnCreatureHit: 0.03, breakChanceOnWallImpact: 0.08, breakChanceOnOrdinaryLanding: 0.01);

    public static readonly Item ThrowingAxe = new(
        "Throwing Axe", '/', "A short-hafted axe weighted for throwing.",
        ItemType.Weapon, EquipmentType.AmmoThrown, EquipmentCategory.Weapon, physicalAttackBonus: 4,
        longDescription: "A compact, single-bladed axe, its haft deliberately short so it tumbles true end-over-end -- never meant to be swung in the hand.",
        size: Size.Medium, itemSize: ItemSize.Small, attackType: AttackType.Slash, weaponType: WeaponType.Axe, weight: 2.0,
        canBeThrown: true, projectileRange: 5, throwableCategory: ThrowableCategory.Martial, charges: 1,
        throwableCapability: ThrowableCapability.ProjectileOnly,
        breakChanceOnCreatureHit: 0.03, breakChanceOnWallImpact: 0.08, breakChanceOnOrdinaryLanding: 0.01);

    public static readonly Item Shuriken = new(
        "Shuriken", '*', "A flat, bladed star meant only to be thrown.",
        ItemType.Weapon, EquipmentType.AmmoThrown, physicalAttackBonus: 1,
        longDescription: "A small four-pointed star of sharpened steel, useless in the hand but deadly at a distance.",
        size: Size.Small, itemSize: ItemSize.VerySmall, attackType: AttackType.Pierce, weight: 0.1,
        canBeThrown: true, projectileRange: 6, throwableCategory: ThrowableCategory.Exotic, charges: 1,
        throwableCapability: ThrowableCapability.ProjectileOnly,
        breakChanceOnCreatureHit: 0.02, breakChanceOnWallImpact: 0.06, breakChanceOnOrdinaryLanding: 0.01);

    public static readonly Item Dart = new(
        "Dart", '`', "A small weighted dart.",
        ItemType.Weapon, EquipmentType.AmmoThrown, physicalAttackBonus: 1,
        longDescription: "A short, weighted dart with a tuft of fletching. Light enough to carry a dozen without noticing.",
        size: Size.Small, itemSize: ItemSize.VerySmall, attackType: AttackType.Pierce, weight: 0.2,
        canBeThrown: true, projectileRange: 5, throwableCategory: ThrowableCategory.Light, charges: 1,
        throwableCapability: ThrowableCapability.ProjectileOnly,
        breakChanceOnCreatureHit: 0.02, breakChanceOnWallImpact: 0.06, breakChanceOnOrdinaryLanding: 0.01);

    /// <summary>Dual-natured: an ordinary Light throwable on its own, AND fireable from a Sling (AmmunitionType.SlingStone) with a centralized range penalty for lacking a proper Sling Stone's shape -- see Entities.Projectiles.ProjectileFactory.RockInSlingRangePenalty.</summary>
    public static readonly Item Rock = new(
        "Rock", '*', "A fist-sized rock. Anyone can throw one -- or sling one, with a penalty.",
        ItemType.Weapon, EquipmentType.AmmoThrown, physicalAttackBonus: 0,
        longDescription: "A plain, fist-sized stone worn smooth. It takes no training at all to pick one up and throw it -- just a good arm -- though it flies less true from a sling than a proper Sling Stone.",
        size: Size.Small, itemSize: ItemSize.VerySmall, attackType: AttackType.Crush, weight: 0.5,
        canBeThrown: true, projectileRange: 4, throwableCategory: ThrowableCategory.Light, charges: 1,
        ammunitionType: AmmunitionType.SlingStone, throwableCapability: ThrowableCapability.ProjectileOnly);

    // --- Blessed/Cursed/on-hit-effect items -- a representative handful demonstrating the
    // expanded item-properties system (Item.IsBlessed/IsCursed/StatusEffects/EncumbranceModifier,
    // Item.IsIdentified), not an exhaustive authoring pass. All start unidentified
    // (isIdentified: false) except the scroll -- there's nothing to hide about a scroll being
    // a scroll. Reaching normal loot tables via BaseItems/Items.All is what makes them
    // "uncommon but not rare": they're just a few more entries in their existing ItemType pool.

    public static readonly Item BlessedLongSword = new(
        "Blessed Long Sword", '/', "A long sword with a faint, warm glow.",
        ItemType.Weapon, EquipmentType.Hand, EquipmentCategory.Weapon, physicalAttackBonus: 6,
        longDescription: "A long sword consecrated at its forging. The glow is easy to miss in daylight, but undead flesh recoils from its edge.",
        size: Size.Large, itemSize: ItemSize.Large, attackType: AttackType.Slash, weaponType: WeaponType.Sword, weight: 4.0,
        isIdentified: false, isBlessed: true, blessedUndeadDamageBonus: 5);

    public static readonly Item CursedDagger = new(
        "Cursed Dagger", '/', "A dagger that feels wrong to hold.",
        ItemType.Weapon, EquipmentType.Hand, EquipmentCategory.Weapon, physicalAttackBonus: 2,
        statModifiers: new Dictionary<PrimaryAttribute, int> { [PrimaryAttribute.Strength] = -2 },
        longDescription: "A plain dagger, its grip cold no matter how long it's held. Something in the blade doesn't want to be put down.",
        size: Size.Small, itemSize: ItemSize.Small, attackType: AttackType.Pierce, weaponType: WeaponType.Dagger, weight: 1.0,
        isIdentified: false, isCursed: true);

    public static readonly Item FlamingLongSword = new(
        "Flaming Long Sword", '/', "A long sword wreathed in guttering fire.",
        ItemType.Weapon, EquipmentType.Hand, EquipmentCategory.Weapon, physicalAttackBonus: 6,
        statusEffects: new[] { new ItemStatusEffect(ItemEffectType.Fire, chance: 1.0, magnitude: 5) },
        longDescription: "A long sword whose edge burns without consuming itself. Every cut leaves a scorch mark behind.",
        size: Size.Large, itemSize: ItemSize.Large, attackType: AttackType.Slash, weaponType: WeaponType.Sword, weight: 4.0,
        isIdentified: false);

    public static readonly Item VenomfangDagger = new(
        "Venomfang Dagger", '/', "A dagger with a faintly discolored edge.",
        ItemType.Weapon, EquipmentType.Hand, EquipmentCategory.Weapon, physicalAttackBonus: 2,
        statusEffects: new[] { new ItemStatusEffect(ItemEffectType.Poison, chance: 0.25, magnitude: 3, duration: 3) },
        longDescription: "A slim dagger, its fuller stained a permanent sickly green. A cut that draws blood sometimes draws more than that.",
        size: Size.Small, itemSize: ItemSize.Small, attackType: AttackType.Pierce, weaponType: WeaponType.Dagger, weight: 1.0,
        isIdentified: false);

    public static readonly Item CursedRing = new(
        "Tarnished Ring", '=', "A ring gone black with tarnish.",
        ItemType.Armor, EquipmentType.Ring,
        statModifiers: new Dictionary<PrimaryAttribute, int> { [PrimaryAttribute.Agility] = -1 },
        encumbranceModifier: -20,
        longDescription: "A once-fine ring, its metal blackened beyond any polish. Wearing it leaves the hand clumsy and the pack somehow heavier.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.1,
        isIdentified: false, isCursed: true);

    // --- Resistance System: positive/mixed equipment (Resistance System spec section 65) ---

    public static readonly Item RingOfEmbers = new(
        "Ring of Embers", '=', "A band warm to the touch, even in a cold room.",
        ItemType.Armor, EquipmentType.Ring, resistanceModifiers: new ResistanceSet(fire: 15),
        longDescription: "A copper ring that never quite cools, as if it remembers a flame it was forged in. Fire seems reluctant to bite the wearer.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.1, minimumLevel: 3, isIdentified: false);

    public static readonly Item RingOfTides = new(
        "Ring of Tides", '=', "A band that always feels faintly damp.",
        ItemType.Armor, EquipmentType.Ring, resistanceModifiers: new ResistanceSet(water: 15),
        longDescription: "A pale ring etched with wave-like grooves. Water beads and rolls off the wearer's skin rather than soaking in.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.1, minimumLevel: 3, isIdentified: false);

    public static readonly Item Frostband = new(
        "Frostband", '=', "A band that stays cold no matter the room.",
        ItemType.Armor, EquipmentType.Ring, resistanceModifiers: new ResistanceSet(ice: 15),
        longDescription: "A ring of pale, faintly frosted metal. Wearing it dulls the bite of cold to a distant, manageable chill.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.1, minimumLevel: 3, isIdentified: false);

    public static readonly Item StormRing = new(
        "Storm Ring", '=', "A band that crackles faintly if you listen close.",
        ItemType.Armor, EquipmentType.Ring, resistanceModifiers: new ResistanceSet(shock: 15),
        longDescription: "A dark ring inlaid with a thread of silver that never stops humming. Lightning seems to slide around the wearer more than through them.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.1, minimumLevel: 3, isIdentified: false);

    public static readonly Item SerpentCharm = new(
        "Serpent Charm", '"', "A small carved fang on a leather cord.",
        ItemType.Armor, EquipmentType.Neck, resistanceModifiers: new ResistanceSet(poison: 20),
        longDescription: "A yellowed fang, carved with a coiling serpent motif, strung on a simple cord. Venom seems to lose its conviction near it.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.2, minimumLevel: 4, isIdentified: false);

    public static readonly Item AmuletOfWarding = new(
        "Amulet of Warding", '"', "A pendant etched with layered protective sigils.",
        ItemType.Armor, EquipmentType.Neck, resistanceModifiers: new ResistanceSet(magic: 15),
        longDescription: "A heavy pendant covered in overlapping wards, worn smooth by whoever carried it before. Hostile magic seems to slide off rather than take hold.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.3, minimumLevel: 4, isIdentified: false);

    public static readonly Item ElementalWardRing = new(
        "Elemental Ward Ring", '=', "A band set with four small, differently colored stones.",
        ItemType.Armor, EquipmentType.Ring, resistanceModifiers: new ResistanceSet(fire: 5, water: 5, ice: 5, shock: 5),
        longDescription: "A plain band set with four tiny stones -- red, blue, white, and yellow -- each offering a modest ward against its own element.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.1, minimumLevel: 3, isIdentified: false);

    public static readonly Item SalamanderBoots = new(
        "Salamander Boots", '{', "Boots with a faint, leathery sheen.",
        ItemType.Armor, EquipmentType.Feet, resistanceModifiers: new ResistanceSet(fire: 8),
        longDescription: "Boots stitched from salamander hide, still faintly warm. The soles never seem to scorch, however hot the ground gets.",
        size: Size.Small, itemSize: ItemSize.Small, armorWeight: ArmorWeight.Light, weight: 2.0, minimumLevel: 2, isIdentified: false);

    public static readonly Item FrostWolfGloves = new(
        "Frost Wolf Gloves", '{', "Thick gloves lined with pale fur.",
        ItemType.Armor, EquipmentType.Gloves, resistanceModifiers: new ResistanceSet(ice: 8),
        longDescription: "Gloves cut from a frost wolf's pelt, the fur still faintly cold to the touch. The wearer's own hands never seem to feel it.",
        size: Size.Small, itemSize: ItemSize.Small, armorWeight: ArmorWeight.Light, weight: 1.5, minimumLevel: 2, isIdentified: false);

    public static readonly Item StormhideBoots = new(
        "Stormhide Boots", '{', "Boots with a faint metallic sheen.",
        ItemType.Armor, EquipmentType.Feet, resistanceModifiers: new ResistanceSet(shock: 8),
        longDescription: "Boots of an odd, faintly iridescent hide, said to be cured in the skin of something that once weathered a storm and won.",
        size: Size.Small, itemSize: ItemSize.Small, armorWeight: ArmorWeight.Light, weight: 2.0, minimumLevel: 2, isIdentified: false);

    public static readonly Item SerpentSkinBracers = new(
        "Serpent-Skin Bracers", '{', "Bracers of tough, faintly scaled leather.",
        ItemType.Armor, EquipmentType.Wrists, resistanceModifiers: new ResistanceSet(poison: 10),
        longDescription: "Bracers cut from serpent hide, the scale pattern still faintly visible beneath the tanning. Venom seems to lose potency against them.",
        size: Size.Small, itemSize: ItemSize.Small, weight: 1.0, minimumLevel: 3, isIdentified: false);

    public static readonly Item RunedSilverAmulet = new(
        "Runed Silver Amulet", '"', "A silver pendant etched with fine runes.",
        ItemType.Armor, EquipmentType.Neck, resistanceModifiers: new ResistanceSet(magic: 10),
        longDescription: "A disc of polished silver, its surface covered edge to edge in tiny protective runes. It hums faintly in the presence of magic.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.2, minimumLevel: 3, isIdentified: false);

    // --- Resistance System: cursed equipment (Resistance System spec section 66) ---

    public static readonly Item AshenCrown = new(
        "Ashen Crown", '^', "A crown of blackened, ash-dusted metal.",
        ItemType.Armor, EquipmentType.Head, magicalAttackBonus: 4, resistanceModifiers: new ResistanceSet(fire: -20),
        longDescription: "A crown cast from metal that never fully cooled, still dusted with ash that never brushes away. It sharpens the mind's fire at the cost of the body's own defense against it.",
        size: Size.Small, itemSize: ItemSize.Small, weight: 1.0, isIdentified: false, isCursed: true);

    public static readonly Item FrozenHeart = new(
        "Frozen Heart", '"', "A pendant shaped like a heart of solid ice.",
        ItemType.Armor, EquipmentType.Neck, resistanceModifiers: new ResistanceSet(fire: -20, ice: 20),
        longDescription: "A pendant carved into the shape of a heart, permanently rimed with frost that never melts. It guards fiercely against cold, and just as fiercely resents the opposite.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.3, isIdentified: false, isCursed: true);

    public static readonly Item RingOfTheDrowned = new(
        "Ring of the Drowned", '=', "A waterlogged ring that never quite dries.",
        ItemType.Armor, EquipmentType.Ring, resistanceModifiers: new ResistanceSet(water: 20, shock: -15),
        longDescription: "A corroded band pulled from something that didn't survive deep water. It wards off the water it came from, but lightning finds the wearer all too easily.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.1, isIdentified: false, isCursed: true);

    public static readonly Item SerpentIdol = new(
        "Serpent Idol", '"', "A small carved idol shaped like a coiled snake.",
        ItemType.Armor, EquipmentType.Neck, resistanceModifiers: new ResistanceSet(poison: 20, magic: -15),
        longDescription: "A blackened idol of a coiled serpent, its carved eyes unsettling to look at directly. It shrugs off venom while leaving the wearer's mind unusually exposed to magic.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.4, isIdentified: false, isCursed: true);

    public static readonly Item CharredBand = new(
        "Charred Band", '=', "A ring scorched black along its entire surface.",
        ItemType.Armor, EquipmentType.Ring, resistanceModifiers: new ResistanceSet(fire: -20),
        longDescription: "A ring burned black by some fire it should never have survived. It never stopped burning, in a way -- fire finds the wearer more easily now.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.1, isIdentified: false, isCursed: true);

    public static readonly Item CrackedFrostRing = new(
        "Cracked Frost Ring", '=', "A ring of clouded ice, fractured through the middle.",
        ItemType.Armor, EquipmentType.Ring, resistanceModifiers: new ResistanceSet(ice: -15),
        longDescription: "A band of what looks like solid ice, a hairline crack running its whole circumference. Whatever cold ward it once held has leaked out through the crack.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.1, isIdentified: false, isCursed: true);

    public static readonly Item StormCursedCrown = new(
        "Storm-Cursed Crown", '^', "A crown that crackles unpleasantly when touched.",
        ItemType.Armor, EquipmentType.Head, resistanceModifiers: new ResistanceSet(shock: -25),
        longDescription: "A jagged metal crown that draws a faint static crackle from anything nearby. It doesn't ward off lightning so much as invite it.",
        size: Size.Small, itemSize: ItemSize.Small, weight: 1.0, isIdentified: false, isCursed: true);

    public static readonly Item FlameguardArmor = new(
        "Flameguard Armor", '[', "Armor plated with heat-blackened scales.",
        ItemType.Armor, EquipmentType.Body, defenseBonus: 2,
        resistanceModifiers: new ResistanceSet(fire: 10),
        longDescription: "Overlapping scales of fire-hardened metal, warm to the touch even in a cold room. Flame slides off it more than it bites in.",
        size: Size.Medium, itemSize: ItemSize.Large, armorWeight: ArmorWeight.Medium, weight: 20.0,
        isIdentified: false);

    public static readonly Item RingOfRegeneration = new(
        "Ring of Regeneration", '=', "A ring set with a faintly pulsing stone.",
        ItemType.Armor, EquipmentType.Ring,
        statusEffects: new[] { new ItemStatusEffect(ItemEffectType.Regeneration, magnitude: 2) },
        longDescription: "A slim band set with a stone that pulses like a slow heartbeat. Wounds seem to knit a little faster while it's worn.",
        size: Size.Small, itemSize: ItemSize.VerySmall, weight: 0.1,
        isIdentified: false);

    public static readonly Item SpikedArmor = new(
        "Spiked Armor", '[', "Armor studded with short iron spikes.",
        ItemType.Armor, EquipmentType.Body, defenseBonus: 2,
        statusEffects: new[] { new ItemStatusEffect(ItemEffectType.Thorns, magnitude: 3) },
        longDescription: "Boiled leather studded with rows of short iron spikes. Anyone careless enough to land a solid hit comes away bleeding too.",
        size: Size.Medium, itemSize: ItemSize.Medium, armorWeight: ArmorWeight.Medium, weight: 15.0,
        isIdentified: false);

    // --- Scroll of Revealing -- a direct-use consumable (not a spell-teaching scroll like
    // SpellScrollCatalog's), see InventoryScreen.ApplyItem's IsIdentifyScroll branch. Named
    // "Revealing" rather than "Identify" specifically to avoid colliding with the auto-generated
    // "Scroll of Identify" SpellScrollCatalog now builds for the Mage-castable Identify spell
    // (see SpellCatalog.Identify) -- two different items with the same display name would be
    // genuinely confusing (one teaches a spell permanently, this one is consumed on the spot).
    public static readonly Item ScrollOfIdentify = new(
        "Scroll of Revealing", '?', "A scroll that reveals an item's true nature.",
        ItemType.Scroll, charges: 1,
        longDescription: "A short scroll bearing a single line of revealing script. Reading it over an item strips away whatever's hidden about it.",
        size: Size.Small, itemSize: ItemSize.Small, weight: 0.25, isIdentifyScroll: true);

    // --- Physical light sources -- see InventoryScreen.ApplyItem's light/extinguish toggle and
    // Core/LightingSystem.cs. Radius/duration/extinguish numbers all come from LightingConfig,
    // never hard-coded here, so balancing never means touching this file (design spec section 5).
    // Wax/wood Candle and Torch are explicitly Flammable (dropped in Fire/Lava, they burn up);
    // the metal Lantern is left at its ItemType.LightSource default of non-flammable.

    public static readonly Item Candle = new(
        "Candle", '!', "A short wax candle -- a dim, fragile light for a few dozen turns.",
        ItemType.LightSource, longDescription: "A stub of tallow candle. Its small flame barely reaches past an arm's length, and a stiff breeze could snuff it out entirely.",
        size: Size.Small, itemSize: ItemSize.Small, weight: 0.2, flammable: true,
        emitsLight: true, lightRadius: LightingConfig.CandleLightRadius, maxLightDuration: LightingConfig.CandleMaxDuration,
        extinguishChancePerTurn: LightingConfig.CandleExtinguishChancePerTurn, extinguishedByWater: true, destroyedWhenLightExhausted: true);

    public static readonly Item Torch = new(
        "Torch", '!', "A pitch-soaked torch -- a reliable, moderate light.",
        ItemType.LightSource, longDescription: "A length of wood wrapped in pitch-soaked cloth. Burns steadily and casts a decent circle of light, though it won't last forever.",
        size: Size.Small, itemSize: ItemSize.Medium, weight: 1.5, flammable: true,
        emitsLight: true, lightRadius: LightingConfig.TorchLightRadius, maxLightDuration: LightingConfig.TorchMaxDuration,
        extinguishChancePerTurn: LightingConfig.TorchExtinguishChancePerTurn, extinguishedByWater: true, destroyedWhenLightExhausted: true);

    public static readonly Item Lantern = new(
        "Lantern", '!', "An oil lantern -- the brightest and most reliable physical light.",
        ItemType.LightSource, longDescription: "A metal-cased oil lantern with a glass window. Casts a wide, steady light and rarely fails on its own -- worth preserving even once its oil finally runs dry.",
        size: Size.Small, itemSize: ItemSize.Medium, weight: 2.5,
        emitsLight: true, lightRadius: LightingConfig.LanternLightRadius, maxLightDuration: LightingConfig.LanternMaxDuration,
        extinguishChancePerTurn: LightingConfig.LanternExtinguishChancePerTurn, extinguishedByWater: true, destroyedWhenLightExhausted: false);

    private static readonly Item[] BaseItems =
    {
        HealthPotion, GreaterHealthPotion, ManaPotion, GreaterManaPotion, ElixirOfVitality,
        MinorElixir, SuperiorHealthPotion, SuperiorManaPotion, PotionOfRenewal,
        MinorPotionOfStrength, PotionOfStrength, MajorPotionOfStrength,
        Dagger, ShortSword, LongSword, BattleAxe, Warhammer, Shield,
        ThiefsShiv, Rapier, WarPick, Mace, HolyMace, DwarvenWaraxe, ElvenBlade, ApprenticeWand,
        SilverDagger, AssassinsDagger, TwinFangDagger, Broadsword, BladeOfTheVanguard,
        ExecutionersAxe, GreatWarhammer, TemplarsMace, JourneymansWand, ArchmagesWand,
        LeatherArmor, ChainMail, PlateArmor, WizardRobe, PaddedArmor, ScaleMail, HalflingLeather,
        ClothTunic, StuddedLeather, ReinforcedChainmail, FullPlateArmor,
        ArchmagesRobe, ShadowweaveArmor, BlessedChainmail, DwarvenPlatemail,
        RoundShield, IronShield, TowerShield,
        LeatherGloves, LeatherBoots, IronHelmet, MagesCap, WarriorsGreathelm, ThiefsHood,
        ClothCap, SilkCap, ClothGloves, SilkGloves, ClothShoes, SilkSlippers,
        LeatherBracers, IronVambraces, MagesCuffs, ClothWraps, SilkWristwraps,
        PaddedSleeves, PlatePauldrons, ScholarsArmbands, ClothSleeves, SilkSleeves,
        SimpleAmulet, AmuletOfVitality, HolySymbol, AmuletOfTheDevout,
        LeatherLeggings, ChainLeggings, PlateGreaves, ClothLeggings, SilkLeggings,
        RingOfStrength, RingOfAgility, RingOfWisdom, RingOfKnowledge, RingOfFortitude,
        BootsOfSwiftness, GauntletsOfPower, CircletOfInsight,
        BasicWand, WandOfFire, Lockpicks,
        Bow, Crossbow, Sling, ShortSpear, LongSpear, Arrow, FireArrow, FrostArrow, PoisonArrow, HolyArrow,
        Bolt, HeavyBolt, SilverBolt, SlingStone, ThrowingKnife, ThrowingAxe, Shuriken, Dart, Rock,
        BlessedLongSword, CursedDagger, FlamingLongSword, VenomfangDagger, CursedRing,
        FlameguardArmor, RingOfRegeneration, SpikedArmor, ScrollOfIdentify, Candle, Torch, Lantern,
        RingOfEmbers, RingOfTides, Frostband, StormRing, SerpentCharm, AmuletOfWarding, ElementalWardRing,
        SalamanderBoots, FrostWolfGloves, StormhideBoots, SerpentSkinBracers, RunedSilverAmulet,
        AshenCrown, FrozenHeart, RingOfTheDrowned, SerpentIdol, CharredBand, CrackedFrostRing, StormCursedCrown
    };

    /// <summary>BaseItems plus one scroll per Mage-castable spell (SpellScrollCatalog), one spellbook per Priest-castable spell (SpellbookCatalog), and the full portable-bag matrix (ContainerCatalog) -- the dungeon's item spawner, LootGenerator, and Trader all draw from this list, so every category becomes naturally findable without a separate spawn system.</summary>
    public static readonly IReadOnlyList<Item> All = BaseItems.Concat(SpellScrollCatalog.All).Concat(SpellbookCatalog.All).Concat(ContainerCatalog.All).ToList();
}
