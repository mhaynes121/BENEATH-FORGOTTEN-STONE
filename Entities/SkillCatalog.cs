using BENEATH_FORGOTTEN_STONE.Entities.Skills;
using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// The physical skill catalog -- same pattern as SpellCatalog.cs: add one
/// static entry + register it in `All`. Unlike spells (found as loot,
/// learned by reading a scroll/spellbook), every skill here is granted
/// automatically the moment a character reaches its Level for its class --
/// see Player.GrantSkillsForLevel. Covers the 26-skill subset (12 Warrior,
/// 14 Thief) chosen from rpg_skill_system_specification.md's full 60-skill
/// list; the rest assume systems this solo, single-player game doesn't have
/// (a party/aggro table, NPCs, etc.) and were deliberately left out.
/// </summary>
public static class SkillCatalog
{
    // --- Warrior ---

    /// <summary>Bash's base (Warrior) unlock level -- Priest's own override below is derived from this (double, floored at 5) rather than a second hardcoded number, so the two can never drift apart if this ever changes.</summary>
    private const int BashWarriorLevel = 1;

    /// <summary>
    /// Also available to Priest -- a later, secondary pickup for them (double the Warrior's
    /// unlock level, never earlier than 5) rather than a core kit piece, per the Shield-Based
    /// Bash proposal. Thief and Mage deliberately do not get it. ProficiencyId is kept as
    /// "skill.warrior.bash" regardless -- it's a stable persistence key, not a display label, and
    /// renaming it would orphan any Priest's (or Warrior's) already-earned proficiency on load.
    /// Prone/Knockdown System: the old StunEffect is replaced by KnockdownEffect -- Stun and Prone
    /// both deny actions, so applying both would be redundant punishment for the same hit.
    /// requireAttackHit: true closes a latent gap the old StunEffect had (it never checked
    /// SpellCastResult.AttackHit, so a missed Bash could still stun) -- KnockdownEffect insists
    /// the shield strike actually connected first.
    /// </summary>
    public static readonly Skill Bash = new(
        "Bash", "Strikes with an equipped shield, knocking the target from its feet.", '!',
        level: BashWarriorLevel, allowedClasses: new[] { CharacterClass.Warrior, CharacterClass.Priest },
        levelOverrides: new Dictionary<CharacterClass, int> { [CharacterClass.Priest] = Math.Max(5, BashWarriorLevel * 2) },
        proficiencyId: "skill.warrior.bash", governingAttribute: PrimaryAttribute.Strength,
        casting: new SkillCastingConfiguration(cooldown: 4),
        targeting: new SpellTargetingConfiguration(SpellTargetType.SingleTarget, range: 1),
        effects: new SpellEffect[] { new WeaponDamageEffect(addShieldDefenseBonus: true), new KnockdownEffect(requireAttackHit: true) },
        requiresShield: true);

    public static readonly Skill Kick = new(
        "Kick", "A raw strike that rattles the target's concentration.", '*',
        level: 2, allowedClasses: new[] { CharacterClass.Warrior },
        proficiencyId: "skill.warrior.kick", governingAttribute: PrimaryAttribute.Strength,
        casting: new SkillCastingConfiguration(cooldown: 5),
        targeting: new SpellTargetingConfiguration(SpellTargetType.SingleTarget, range: 1),
        // "Interrupt spellcasting" reinterpreted as a Silence -- every cast in this engine
        // resolves instantly, so there's no cast-in-progress state to interrupt.
        effects: new SpellEffect[] { new WeaponDamageEffect(), new SilenceEffect(duration: 2) });

    public static readonly Skill HardyConstitution = new(
        "Hardy Constitution", "Passive. A frame built to take punishment.", '+',
        level: 3, allowedClasses: new[] { CharacterClass.Warrior },
        isPassive: true,
        onGrant: p => p.Health.IncreaseMax((int)(p.Stats.Adjusted(PrimaryAttribute.Constitution) * 0.5)));

    public static readonly Skill ShieldBlock = new(
        "Shield Block", "Braces behind a shield, sharply reducing incoming damage.", ')',
        level: 6, allowedClasses: new[] { CharacterClass.Warrior },
        proficiencyId: "skill.warrior.shield_block", governingAttribute: PrimaryAttribute.Constitution,
        casting: new SkillCastingConfiguration(cooldown: 6),
        targeting: new SpellTargetingConfiguration(SpellTargetType.Self, range: 0),
        effects: new SpellEffect[] { new StatModifierEffect(Stat.Armor, amount: 8, duration: 2) },
        requiresShield: true);

    public static readonly Skill ShatterGuard = new(
        "Shatter Guard", "A heavy blow that permanently cracks the target's armor.", '#',
        level: 13, allowedClasses: new[] { CharacterClass.Warrior },
        proficiencyId: "skill.warrior.shatter_guard", governingAttribute: PrimaryAttribute.Strength,
        casting: new SkillCastingConfiguration(cooldown: 8),
        targeting: new SpellTargetingConfiguration(SpellTargetType.SingleTarget, range: 1),
        effects: new SpellEffect[] { new PermanentStatModifierEffect(Stat.Armor, amount: -3) });

    public static readonly Skill ShieldSlam = new(
        "Shield Slam", "Slams a shield into the enemy, damage driven by your own Armor.", ')',
        level: 15, allowedClasses: new[] { CharacterClass.Warrior },
        proficiencyId: "skill.warrior.shield_slam", governingAttribute: PrimaryAttribute.Constitution,
        casting: new SkillCastingConfiguration(cooldown: 6),
        targeting: new SpellTargetingConfiguration(SpellTargetType.SingleTarget, range: 1),
        effects: new SpellEffect[] { new WeaponDamageEffect(scaleOffArmor: true) },
        requiresShield: true);

    public static readonly Skill BerserkerRage = new(
        "Berserker Rage", "Shrugs off every stun and silence, at the cost of your own defense.", '^',
        level: 16, allowedClasses: new[] { CharacterClass.Warrior },
        proficiencyId: "skill.warrior.berserker_rage", governingAttribute: PrimaryAttribute.Constitution,
        casting: new SkillCastingConfiguration(cooldown: 15),
        targeting: new SpellTargetingConfiguration(SpellTargetType.Self, range: 0),
        effects: new SpellEffect[]
        {
            new CleanseAndImmunityEffect(immunityDuration: 3),
            // "+10% damage taken" reinterpreted as a flat Armor cut -- no damage-taken
            // multiplier field exists on Actor, and this reads the same in practice.
            new StatModifierEffect(Stat.Armor, amount: -4, duration: 3)
        });

    public static readonly Skill CripplingStrike = new(
        "Crippling Strike", "A targeted blow to the legs that slows the enemy.", '%',
        level: 17, allowedClasses: new[] { CharacterClass.Warrior },
        proficiencyId: "skill.warrior.crippling_strike", governingAttribute: PrimaryAttribute.Strength,
        casting: new SkillCastingConfiguration(cooldown: 6),
        targeting: new SpellTargetingConfiguration(SpellTargetType.SingleTarget, range: 1),
        effects: new SpellEffect[] { new StatModifierEffect(Stat.MovementSpeed, amount: -4, duration: 4, isHostileSecondaryEffect: true) });

    public static readonly Skill Bloodthirst = new(
        "Bloodthirst", "A vicious attack that mends your own wounds with the damage dealt.", '&',
        level: 18, allowedClasses: new[] { CharacterClass.Warrior },
        proficiencyId: "skill.warrior.bloodthirst", governingAttribute: PrimaryAttribute.Strength,
        casting: new SkillCastingConfiguration(cooldown: 6),
        targeting: new SpellTargetingConfiguration(SpellTargetType.SingleTarget, range: 1),
        effects: new SpellEffect[]
        {
            new WeaponDamageEffect(),
            new HealEffect(percentOfDamageDealt: 0.5, target: HealTarget.Caster)
        });

    public static readonly Skill StalwartDefender = new(
        "Stalwart Defender", "Passive. A body too disciplined for magic to easily unravel.", '+',
        level: 19, allowedClasses: new[] { CharacterClass.Warrior },
        isPassive: true,
        onGrant: p => StatModifierEffect.ApplyStatDelta(p, Stat.MagicResistance, (int)(p.Stats.Adjusted(PrimaryAttribute.Constitution) * 0.3)));

    public static readonly Skill Recklessness = new(
        "Recklessness", "Abandons all caution for a burst of raw offense.", '^',
        level: 22, allowedClasses: new[] { CharacterClass.Warrior },
        proficiencyId: "skill.warrior.recklessness", governingAttribute: PrimaryAttribute.Strength,
        casting: new SkillCastingConfiguration(cooldown: 10),
        targeting: new SpellTargetingConfiguration(SpellTargetType.Self, range: 0),
        // "+50% crit chance" reinterpreted as a flat physical-attack buff -- no crit-hit
        // concept exists in this combat model (only hit/miss + severity tiers).
        effects: new SpellEffect[]
        {
            new StatModifierEffect(Stat.PhysicalAttack, amount: 6, duration: 2),
            new ZeroArmorEffect(duration: 2)
        });

    public static readonly Skill Executioner = new(
        "Executioner", "A devastating finisher usable only against a badly wounded foe.", 'X',
        level: 25, allowedClasses: new[] { CharacterClass.Warrior },
        proficiencyId: "skill.warrior.executioner", governingAttribute: PrimaryAttribute.Strength,
        casting: new SkillCastingConfiguration(cooldown: 10),
        targeting: new SpellTargetingConfiguration(SpellTargetType.SingleTarget, range: 1),
        effects: new SpellEffect[] { new WeaponDamageEffect(multiplier: 3.0) },
        targetHealthPercentBelow: 0.20);

    // --- Thief ---

    public static readonly Skill Evade = new(
        "Evade", "A burst of footwork that makes you far harder to hit.", '~',
        level: 4, allowedClasses: new[] { CharacterClass.Thief },
        proficiencyId: "skill.thief.evade", governingAttribute: PrimaryAttribute.Agility,
        casting: new SkillCastingConfiguration(cooldown: 6),
        targeting: new SpellTargetingConfiguration(SpellTargetType.Self, range: 0),
        effects: new SpellEffect[] { new StatModifierEffect(Stat.Dodge, amount: 15, duration: 2) });

    public static readonly Skill PoisonWeapon = new(
        "Poison Weapon", "Coats your blade with a toxin that can sicken whatever it cuts.", '&',
        level: 5, allowedClasses: new[] { CharacterClass.Thief },
        proficiencyId: "skill.thief.poison_weapon", governingAttribute: PrimaryAttribute.Agility,
        casting: new SkillCastingConfiguration(cooldown: 12),
        targeting: new SpellTargetingConfiguration(SpellTargetType.Self, range: 0),
        effects: new SpellEffect[] { new PoisonWeaponEffect(duration: 10) });

    public static readonly Skill Blind = new(
        "Blind", "Throws blinding powder, ruining the target's own accuracy.", '%',
        level: 10, allowedClasses: new[] { CharacterClass.Thief },
        proficiencyId: "skill.thief.blind", governingAttribute: PrimaryAttribute.Agility,
        casting: new SkillCastingConfiguration(cooldown: 8),
        targeting: new SpellTargetingConfiguration(SpellTargetType.SingleTarget, range: 3),
        effects: new SpellEffect[] { new StatModifierEffect(Stat.Accuracy, amount: -15, duration: 3, isHostileSecondaryEffect: true) });

    /// <summary>
    /// Prone/Knockdown System: a pure success-roll knockdown, deliberately NOT built on
    /// WeaponDamageEffect -- "should not use a normal weapon-damage attack simply to determine
    /// whether it succeeds" (Prone and Knockdown System proposal). requireAttackHit: false, so
    /// KnockdownEffect's own SecondaryEffectChance roll (Ability Proficiency System's rank-scaled
    /// application chance) IS Trip's entire success check. Still consumes its cooldown and turn on
    /// any valid attempt regardless of outcome -- SkillCaster.Cast reserves the cooldown before any
    /// effect runs, and HandleUseSkill treats a resolved cast as a spent turn either way.
    /// </summary>
    public static readonly Skill Trip = new(
        "Trip", "Sweeps the target's footing out from under them.", '%',
        level: 7, allowedClasses: new[] { CharacterClass.Thief },
        proficiencyId: "skill.thief.trip", governingAttribute: PrimaryAttribute.Agility,
        casting: new SkillCastingConfiguration(cooldown: 6),
        targeting: new SpellTargetingConfiguration(SpellTargetType.SingleTarget, range: 1),
        effects: new SpellEffect[] { new KnockdownEffect() });

    public static readonly Skill FleetFooted = new(
        "Fleet Footed", "Passive. Permanently quicker on your feet than most.", '+',
        level: 13, allowedClasses: new[] { CharacterClass.Thief },
        isPassive: true,
        onGrant: p => StatModifierEffect.ApplyStatDelta(p, Stat.MovementSpeed, 2));

    public static readonly Skill Assassinate = new(
        "Assassinate", "A lethal strike that punishes a foe who hasn't yet taken a scratch.", 'X',
        level: 26, allowedClasses: new[] { CharacterClass.Thief },
        proficiencyId: "skill.thief.assassinate", governingAttribute: PrimaryAttribute.Agility,
        casting: new SkillCastingConfiguration(cooldown: 15),
        targeting: new SpellTargetingConfiguration(SpellTargetType.SingleTarget, range: 1),
        effects: new SpellEffect[] { new WeaponDamageEffect(multiplier: 5.0) },
        targetHealthPercentAtLeast: 1.0);

    public static readonly Skill ExecutionCall = new(
        "Execution Call", "Strikes a fatal pressure point -- instantly fatal if it lands true.", 'X',
        level: 30, allowedClasses: new[] { CharacterClass.Thief },
        proficiencyId: "skill.thief.execution_call", governingAttribute: PrimaryAttribute.Agility,
        casting: new SkillCastingConfiguration(cooldown: 20),
        targeting: new SpellTargetingConfiguration(SpellTargetType.SingleTarget, range: 1),
        effects: new SpellEffect[] { new ExecuteInstakillEffect() });

    public static readonly Skill Sneak = new(
        "Sneak", "Slips into a stealthy stance, going unnoticed at a distance.", '?',
        level: 2, allowedClasses: new[] { CharacterClass.Thief },
        proficiencyId: "skill.thief.sneak", governingAttribute: PrimaryAttribute.Agility,
        casting: new SkillCastingConfiguration(cooldown: 0),
        targeting: new SpellTargetingConfiguration(SpellTargetType.Self, range: 0),
        effects: new SpellEffect[] { new SneakEffect() });

    public static readonly Skill Backstab = new(
        "Backstab", "A brutal strike against a foe who hasn't noticed you.", '/',
        level: 3, allowedClasses: new[] { CharacterClass.Thief },
        proficiencyId: "skill.thief.backstab", governingAttribute: PrimaryAttribute.Agility,
        casting: new SkillCastingConfiguration(cooldown: 5),
        targeting: new SpellTargetingConfiguration(SpellTargetType.SingleTarget, range: 1),
        effects: new SpellEffect[] { new WeaponDamageEffect(multiplier: 3.0) },
        requiresUnalertedSneakTarget: true);

    /// <summary>Passive, no OnGrant -- purely a flag other systems check via player.HasSkill. See GameLoop.HandleOpenChest.</summary>
    public static readonly Skill PickLock = new(
        "Pick Lock", "Passive. Opens locked chests without a key.", '=',
        level: 1, allowedClasses: new[] { CharacterClass.Thief },
        proficiencyId: "skill.thief.pick_lock", governingAttribute: PrimaryAttribute.Knowledge,
        isPassive: true);

    /// <summary>Passive, no OnGrant -- see GameLoop.AwardDeathRewards' bonus-item roll.</summary>
    public static readonly Skill PickPocket = new(
        "Pick Pocket", "Passive. A chance to slip an extra item free of whatever you kill.", '=',
        level: 2, allowedClasses: new[] { CharacterClass.Thief },
        proficiencyId: "skill.thief.pick_pocket", governingAttribute: PrimaryAttribute.Agility,
        isPassive: true);

    /// <summary>Opens a timed window during which each move only has a chance (scaled off Agility) to reveal nearby traps -- see DetectTrapsEffect/GameLoop.RollDetectTraps.</summary>
    public static readonly Skill DetectTraps = new(
        "Detect Traps", "Opens your senses to hidden danger nearby for a short time.", '^',
        level: 6, allowedClasses: new[] { CharacterClass.Thief },
        proficiencyId: "skill.thief.detect_traps", governingAttribute: PrimaryAttribute.Agility,
        casting: new SkillCastingConfiguration(cooldown: 12),
        targeting: new SpellTargetingConfiguration(SpellTargetType.Self, range: 0),
        effects: new SpellEffect[] { new DetectTrapsEffect(duration: 10, PrimaryAttribute.Agility) });

    /// <summary>A Thief's alternative to a Mage/Priest's always-succeeding Identify spell (SpellCatalog.Identify) -- a stat-scaled roll instead (StatBasedIdentifyEffect), off Knowledge, the same "appraisal" stat lockpicking already scales off (see GameLoop.PickDoorLock). Can fail; still costs its cooldown either way.</summary>
    public static readonly Skill Identify = new(
        "Identify", "A practiced eye for appraising an item's true nature -- not foolproof.", '?',
        level: 4, allowedClasses: new[] { CharacterClass.Thief },
        proficiencyId: "skill.thief.identify", governingAttribute: PrimaryAttribute.Knowledge,
        casting: new SkillCastingConfiguration(cooldown: 6),
        targeting: new SpellTargetingConfiguration(SpellTargetType.Item, range: 0),
        effects: new SpellEffect[] { new StatBasedIdentifyEffect(PrimaryAttribute.Knowledge) });

    public static readonly Skill Shadowstep = new(
        "Shadowstep", "Slips through the shadows to a foe's side.", '?',
        level: 21, allowedClasses: new[] { CharacterClass.Thief },
        proficiencyId: "skill.thief.shadowstep", governingAttribute: PrimaryAttribute.Agility,
        casting: new SkillCastingConfiguration(cooldown: 12),
        targeting: new SpellTargetingConfiguration(SpellTargetType.SingleTarget, range: 5, scalesRangeWithProficiency: true),
        effects: new SpellEffect[] { new TeleportAdjacentEffect() });

    /// <summary>Passive, no OnGrant -- see EquipmentCompatibility's Weapon-vs-Weapon conflict bypass and GameLoop.HandleMove's off-hand attack.</summary>
    public static readonly Skill DualWield = new(
        "Dual Wield", "Passive. Trained to fight with a blade in each hand.", '/',
        level: 1, allowedClasses: new[] { CharacterClass.Thief },
        isPassive: true);

    /// <summary>Passive, no OnGrant -- see ChaseAI's free counter-attack on a monster's miss.</summary>
    public static readonly Skill Riposte = new(
        "Riposte", "Passive. Answers a missed attack with one of your own.", '/',
        level: 18, allowedClasses: new[] { CharacterClass.Thief },
        isPassive: true);

    // --- Priest ---
    // New Priest Skill Progression: six skills centered on emergency healing, sacred authority,
    // cleansing, resilience, and anti-undead combat. Priest's existing Bash (level 5, above) is
    // unchanged and outside this proposal's scope. None of these cost mana -- see each skill's
    // own casting config, which never sets ManaCost (a field SkillCastingConfiguration doesn't
    // even have; skills never cost mana in this engine).

    /// <summary>Emergency heal for when the Priest can't afford or cast a healing spell. "It works while Silenced" and "cannot be used while Stunned or Prone" both hold automatically: skills never check SilencedUntilTurn at all (only spells do), and GameLoop already blocks the player's ENTIRE turn while Stunned, and blocks UseSkill specifically while Prone (see GameLoop.IsAllowedWhileProne) -- no extra gating needed here for either.</summary>
    public static readonly Skill LayOnHands = new(
        "Lay on Hands", "Channels practiced faith through the hands, closing wounds without drawing upon mana.", '+',
        level: 3, allowedClasses: new[] { CharacterClass.Priest },
        proficiencyId: "skill.priest.lay_on_hands", governingAttribute: PrimaryAttribute.Wisdom,
        casting: new SkillCastingConfiguration(cooldown: 12),
        targeting: new SpellTargetingConfiguration(SpellTargetType.Self, range: 0),
        effects: new SpellEffect[] { new HealEffect(fixedAmount: 10, scalesWithAttribute: PrimaryAttribute.Wisdom) },
        flavorCastMessage: "You lay a healing hand upon your wounds.",
        preconditionCheck: context => ((Player)context.Caster).Health.Current >= ((Player)context.Caster).Health.Max
            ? "You have no wounds to mend."
            : null);

    /// <summary>The Priest's signature crowd-control skill -- Charisma-governed (force of presence, not magical knowledge). PreconditionCheck reads the SelfCenteredArea targeting's own already-resolved AffectedActors (visible undead within radius 4), so the "no eligible undead" rejection can never drift from what the effect itself would actually hit.</summary>
    public static readonly Skill TurnUndead = new(
        "Turn Undead", "Invokes sacred authority, driving nearby undead back in terror.", '!',
        level: 6, allowedClasses: new[] { CharacterClass.Priest },
        proficiencyId: "skill.priest.turn_undead", governingAttribute: PrimaryAttribute.Charisma,
        casting: new SkillCastingConfiguration(cooldown: 10),
        targeting: new SpellTargetingConfiguration(SpellTargetType.SelfCenteredArea, range: 0,
            areaOfEffectRadius: 4, requiresLineOfSight: true, creatureTypeFilter: CreatureType.Undead),
        effects: new SpellEffect[]
        {
            new FrightenedEffect(duration: 4,
                successMessageTemplate: "{target} recoils in terror!",
                failureMessageTemplate: "{target} defies your command.",
                bossResistancePenalty: 0.30)
        },
        flavorCastMessage: "You raise your holy symbol and invoke sacred authority!",
        preconditionCheck: context => context.AffectedActors.Count == 0 ? "There are no undead nearby to turn." : null);

    /// <summary>Broad condition removal without mana -- see PurifyEffect for exactly what's eligible. The precondition check mirrors PurifyEffect's own eligibility logic exactly (ActiveEffects.Any(CanBePurified), or Silenced/Frightened still pending) so the free-rejection message and the actual cleanse can never disagree about whether there's anything to remove.</summary>
    public static readonly Skill Purify = new(
        "Purify", "Cleanses the body and spirit of poison, flame, fear, and corrupting influence.", '+',
        level: 10, allowedClasses: new[] { CharacterClass.Priest },
        proficiencyId: "skill.priest.purify", governingAttribute: PrimaryAttribute.Wisdom,
        casting: new SkillCastingConfiguration(cooldown: 10),
        targeting: new SpellTargetingConfiguration(SpellTargetType.Self, range: 0),
        effects: new SpellEffect[] { new PurifyEffect() },
        flavorCastMessage: "A cleansing warmth drives the affliction from your body.",
        preconditionCheck: context =>
        {
            var caster = (Player)context.Caster;
            bool hasPurifiable = caster.ActiveEffects.Any(e => e.CanBePurified)
                || context.TurnNumber < caster.SilencedUntilTurn
                || context.TurnNumber < caster.FrightenedUntilTurn;
            return hasPurifiable ? null : "You have no affliction that Purify can remove.";
        });

    /// <summary>Passive, no OnGrant -- purely reactive. Its 25% resistance chance against Stun/Frightened/ability-induced Knockdown is checked by CrowdControlResolver/KnockdownResolver directly (via player.HasSkill(Steadfast)), never here; there's no one-shot effect to apply at grant time the way Hardy Constitution's HP bonus needs one.</summary>
    public static readonly Skill Steadfast = new(
        "Steadfast", "Passive. Unwavering faith makes you difficult to stun, frighten, or knock from your feet.", '+',
        level: 14, allowedClasses: new[] { CharacterClass.Priest },
        isPassive: true);

    /// <summary>Focused anti-undead melee -- RequiresMeleeWeapon rejects bare hands/a Wand the same way RequiresShield rejects no shield. The Holy bonus damage explicitly depends on the physical attack having hit first (see WeaponDamageEffect: the bonus roll only happens inside the already-hit branch).</summary>
    public static readonly Skill Exorcism = new(
        "Exorcism", "Condemns a nearby undead creature with a weapon strike charged by holy judgment.", '!',
        level: 19, allowedClasses: new[] { CharacterClass.Priest },
        proficiencyId: "skill.priest.exorcism", governingAttribute: PrimaryAttribute.Wisdom,
        casting: new SkillCastingConfiguration(cooldown: 6),
        targeting: new SpellTargetingConfiguration(SpellTargetType.SingleTarget, range: 1),
        effects: new SpellEffect[]
        {
            new WeaponDamageEffect(
                bonusDamageDice: new DiceRoll(2, 6), bonusDamageType: DamageType.Holy,
                hitMessageTemplate: "Your weapon blazes with judgment as it strikes {target}!",
                missMessageTemplate: "Your exorcism fails to find its mark.")
        },
        requiresMeleeWeapon: true,
        preconditionCheck: context => context.TargetActor is Monster { CreatureType: CreatureType.Undead }
            ? null
            : "Exorcism can only be used against the undead.");

    /// <summary>Late-game protection from one lethal event -- see IntercessionEffect (opens the window) and CombatStatsTracker.ApplyDamage (the single centralized place the window is actually consumed). Proficiency never grants more than the one charge; only the 10-turn window scales.</summary>
    public static readonly Skill Intercession = new(
        "Intercession", "Places your life beneath divine protection, preserving you from the next mortal blow.", '+',
        level: 25, allowedClasses: new[] { CharacterClass.Priest },
        proficiencyId: "skill.priest.intercession", governingAttribute: PrimaryAttribute.Wisdom,
        casting: new SkillCastingConfiguration(cooldown: 40),
        targeting: new SpellTargetingConfiguration(SpellTargetType.Self, range: 0),
        effects: new SpellEffect[] { new IntercessionEffect(baseDuration: 10) },
        flavorCastMessage: "You entrust your life to divine protection.",
        preconditionCheck: context => ((Player)context.Caster).IntercessionActive ? "Intercession already guards your life." : null);

    public static readonly IReadOnlyList<Skill> All = new[]
    {
        Bash, Kick, HardyConstitution, ShieldBlock, ShatterGuard, ShieldSlam, BerserkerRage,
        CripplingStrike, Bloodthirst, StalwartDefender, Recklessness, Executioner,
        Evade, PoisonWeapon, Blind, Trip, FleetFooted, Assassinate, ExecutionCall,
        Sneak, Backstab, PickLock, PickPocket, DetectTraps, Shadowstep, DualWield, Riposte, Identify,
        LayOnHands, TurnUndead, Purify, Steadfast, Exorcism, Intercession
    };
}
