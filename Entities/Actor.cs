using BENEATH_FORGOTTEN_STONE.Entities.AI;
using BENEATH_FORGOTTEN_STONE.Entities.Components;
using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Entities;

public abstract class Actor : IInspectable
{
    public int X { get; set; }
    public int Y { get; set; }
    public char Symbol { get; set; }
    public ConsoleColor Color { get; set; } = ConsoleColor.White;
    public string Name { get; set; } = "Actor";

    /// <summary>What combat/Look/death text should call this actor -- defaults to Name. Monster overrides it for a boss (see Monster.DisplayName), so display code never needs to special-case bosses itself.</summary>
    public virtual string DisplayName => Name;

    /// <summary>True when DisplayName is a proper name that must never take an "a/an"/"the" article (e.g. a boss's "Lormax Golden Wing") -- see CombatMessages.WithArticle/Label. False (the default) means DisplayName is a common noun like "goblin" that always needs one.</summary>
    public virtual bool UsesProperNounDisplayName => false;

    /// <summary>
    /// Whether this actor can ever be the target of combat, a spell/skill effect, or a hostile
    /// status effect -- the single centralized hook every such guard checks (see
    /// GameLoop.HandleMove's bump interception, TargetResolver's AoE/single-target filtering,
    /// EffectProcessor.Tick). True for every existing actor (Player, Monster); NPC overrides
    /// this to false so all non-combat NPC types get full protection for free.
    /// </summary>
    public virtual bool CanBeTargeted => true;

    /// <summary>Shown by Look when this actor is more than one tile away.</summary>
    public string ShortDescription { get; set; } = "";

    /// <summary>Shown by Look when this actor is adjacent.</summary>
    public string LongDescription { get; set; } = "";

    /// <summary>Energy gained per scheduler tick. 10 = normal speed.</summary>
    public int Speed { get; set; } = 10;

    /// <summary>Current banked energy; the scheduler grants a turn once this crosses its threshold.</summary>
    public int Energy { get; set; }

    /// <summary>Effective character/monster level -- drives XP rewards, gold drops, and level-difference scaling.</summary>
    public int Level { get; set; } = 1;

    /// <summary>Drives PhysicalAttack's hit chance. Player overrides via its rolled Agility stat; Monster via a flat archetype value.</summary>
    public virtual int Agility => 10;

    /// <summary>How this actor's physical attack is delivered. Monster overrides via its archetype (Bite/Sting/Hit); Player overrides via its equipped weapon, falling back to Hit when unarmed. Drives the verb in CombatMessages.Format -- see AttackType.cs.</summary>
    public virtual AttackType AttackType => AttackType.Hit;

    public int BasePhysicalAttackPower { get; set; } = 3;
    public int BaseMagicalAttackPower { get; set; } = 0;
    public int DefensePower { get; set; } = 0;
    public int MagicResistance { get; set; } = 0;

    /// <summary>Temporary bonus to max carry capacity from Bull's Strength/Elixirs of Burden -- see EncumbranceCalculator. Lives on Actor rather than Player only so StatModifierEffect.ApplyStatDelta stays generic over any caster; meaningless for a Monster, which has no encumbrance UI.</summary>
    public int CarryCapacityBonus { get; set; } = 0;

    /// <summary>Evade's temporary bonus to this actor's own effective Agility when DEFENDING -- see PhysicalAttack's hit-chance formula and Stat.Dodge.</summary>
    public int DodgeModifier { get; set; } = 0;

    /// <summary>Blind's temporary penalty to this actor's own effective Agility when ATTACKING -- see PhysicalAttack's hit-chance formula and Stat.Accuracy.</summary>
    public int AccuracyModifier { get; set; } = 0;

    /// <summary>
    /// Turn number this actor's stun/silence/CC-immunity lasts through, compared against
    /// Level.TurnNumber wherever checked (GameLoop's turn loop, SpellCasterAI, the skill
    /// effects that set these). Deliberately NOT ActiveEffect-backed: these are pass/fail
    /// states, not magnitudes, so there's no inverse delta to revert -- a plain "is
    /// currentTurn &lt; X" check needs no EffectProcessor involvement, and refreshing on a
    /// second application is just Math.Max, which is also the correct game-design behavior.
    /// </summary>
    public int StunnedUntilTurn { get; set; } = 0;

    public int SilencedUntilTurn { get; set; } = 0;

    /// <summary>
    /// While Level.TurnNumber is below this, the actor is Frightened -- prefers movement that
    /// increases distance from whatever caused the fear, and never voluntarily moves closer to
    /// it. Mirrors StunnedUntilTurn/SilencedUntilTurn's own shape (a plain turn-number deadline,
    /// not ActiveEffect-backed). Only the player's Turn Undead can cause this today, so the
    /// source itself is never separately tracked here -- see GameLoop's Frightened turn-handling
    /// for why "away from the player" is a safe hardcoded direction rather than a stored
    /// reference. Persisted for monsters (see MonsterData/RestoreData) exactly like the other
    /// three CC deadlines above.
    /// </summary>
    public int FrightenedUntilTurn { get; set; } = 0;

    /// <summary>While Level.TurnNumber is below this, Stun/Silence application is resisted -- see Berserker Rage.</summary>
    public int CcImmuneUntilTurn { get; set; } = 0;

    /// <summary>
    /// Prone/Knockdown System: true while this actor is knocked down (Bash, Trip, Tremor, or a
    /// Water/Ice slip). Unlike Stun/Silence, deliberately NOT a turn-number deadline -- prone has
    /// no duration and only ever clears when the actor actually spends an action standing (the
    /// player's Stand command; a monster's AI.TakeTurn is skipped in favor of an automatic stand
    /// -- see GameLoop.Run). See KnockdownResolver for the only place this is set to true.
    /// </summary>
    public bool IsProne { get; set; }

    public HealthComponent Health { get; set; }
    public ManaComponent Mana { get; set; }
    public InventoryComponent Inventory { get; set; } = new();

    public List<Spell> KnownSpells { get; } = new();
    public Dictionary<Spell, int> SpellCooldowns { get; } = new();
    public List<ActiveEffect> ActiveEffects { get; } = new();

    /// <summary>Null for the player, who is driven by input instead.</summary>
    public IAIComponent AI { get; set; }

    /// <summary>Empty for an Actor with no equipment concept. Player and Monster each own their own EquipmentComponent and override this to expose it -- lets item-driven behavior (on-hit procs, Thorns, resistances, Regeneration, the blessed-vs-undead bonus) be written once against Actor instead of once per subclass. See ItemEffectApplier, EffectProcessor.</summary>
    public virtual IEnumerable<Item> GetEquippedItems() => Enumerable.Empty<Item>();

    public bool IsAlive => Health == null || Health.Current > 0;

    /// <summary>
    /// Human-readable description of whatever last damaged this actor --
    /// "an orc's physical attack", "a goblin shaman casting Fireball", "a
    /// goblin shaman's Burning" -- set at every damage-application site
    /// (PhysicalAttack, DamageEffect, EffectProcessor's DoT ticks) and read
    /// by GameLoop.HandleDeath for the graveyard record. Deliberately just a
    /// string rather than a reference to the attacking Actor, so a future
    /// non-Actor damage source (a trap, a cursed item) can set it too
    /// without this needing to know those types exist.
    /// </summary>
    public string LastDamageSource { get; set; } = "";

    /// <summary>
    /// The Actor credited with this actor's most recent damage -- null when the last damage
    /// came from something other than an Actor (environment, a trap, floor-attunement attrition)
    /// or when nothing has damaged this actor yet. Set alongside LastDamageSource at every
    /// direct-damage site (PhysicalAttack, ProjectileEngine, DamageEffect, ExecuteInstakillEffect)
    /// and propagated from ActiveEffect.Owner by EffectProcessor's DoT ticks, so a delayed kill
    /// (poison, burning) still credits whoever applied it. Every non-Actor damage site must
    /// explicitly reset this to null rather than merely leaving it unset, since a stale
    /// player-owned value from an earlier hit must never be mistaken for credit on a later,
    /// unrelated environmental kill -- see GameLoop.AwardDeathRewards, which reads this to decide
    /// whether a monster's death was the player's doing.
    /// </summary>
    public Actor LastDamageOwner { get; set; }

    /// <summary>
    /// Pet and Companion System: the actor that most recently damaged THIS actor -- stamped
    /// alongside LastDamageOwner at the single centralized CombatStatsTracker.ApplyDamage choke
    /// point every damage source funnels through. Distinct from LastDamageOwner in one way: this
    /// is never reset to null for an environmental/ownerless hit, so a pet's own AI (the only
    /// current reader) can still tell "who last hit me" even across an unrelated trap tick.
    /// Transient -- never persisted, same reasoning as LastDamageOwner.
    /// </summary>
    public Actor LastAttacker { get; set; }

    /// <summary>
    /// The actor THIS actor most recently dealt damage to -- the mirror image of LastAttacker,
    /// stamped on the ATTACKER side of the same ApplyDamage call. Lets a pet's AI answer "what is
    /// my owner's most recent combat target" (proposal section 5) without a separate notification
    /// system: melee, ranged, thrown, spell, skill, and DoT damage all already funnel through
    /// ApplyDamage, so this one field covers all of them for free. An AoE hit updates this once
    /// per affected target in sequence, so it ends up holding whichever was processed last rather
    /// than necessarily "the explicitly selected target" -- an accepted simplification (see the
    /// Pet and Companion System's own design notes). Transient -- never persisted.
    /// </summary>
    public Actor LastCombatTarget { get; set; }

    /// <summary>
    /// Level.TurnNumber at the moment this actor last attacked (melee, a fired projectile, or
    /// an offensive spell/skill) -- set at each of those action sites, never read back except
    /// by GameLoop's own "is the player in combat right now" check (natural regeneration slows
    /// way down while fighting -- see ResourceRegenerationCalculator). Starts far enough in the
    /// past that a fresh actor is never mistaken for "just attacked."
    /// </summary>
    public int LastAttackTurn { get; set; } = -1000;

    public void MoveTo(int x, int y)
    {
        X = x;
        Y = y;
    }

    private const double BaseHitChance = 0.90;
    private const double AgilityScalingPerPoint = 0.02;
    private const double MinHitChance = 0.50;
    private const double MaxHitChance = 0.99;

    /// <param name="bonusHitChance">
    /// A direct percentage-point adjustment to the final hit chance -- the Ability Proficiency
    /// System's rank-based accuracy adjustment for a physical skill's own attack roll (see
    /// WeaponDamageEffect), added as a flat percentage rather than folded into AccuracyModifier
    /// (which is measured in Agility-equivalent points, a different unit) so the conversion never
    /// depends on AgilityScalingPerPoint's own value. Defaults to 0, so every ordinary attack
    /// (and every attack before this feature existed) is completely unaffected -- "the proficiency
    /// adjustment applies only to the named skill attack... not ordinary attacks or general
    /// Accuracy."
    /// </param>
    public AttackResult PhysicalAttack(Actor target, Random rng, double bonusHitChance = 0)
    {
        // AccuracyModifier is this actor's OWN penalty when attacking (Blind); DodgeModifier is
        // the DEFENDER's bonus to being missed (Evade) -- both just widen the same Agility gap
        // the formula already scales, so nothing else about hit resolution needs to change.
        double hitChance = Math.Clamp(
            BaseHitChance + (((Agility + AccuracyModifier) - (target.Agility + target.DodgeModifier)) * AgilityScalingPerPoint) + bonusHitChance,
            MinHitChance, MaxHitChance);

        if (rng.NextDouble() > hitChance)
        {
            return new AttackResult { Attacker = this, Defender = target, Hit = false, Damage = 0, AttackType = AttackType };
        }

        int preDamageHealth = target.Health?.Current ?? 0;
        int variance = rng.Next(-1, 2); // -1, 0, or 1
        // A hit that connects always deals at least 1 -- a "hit" that lands for 0 (Defense >= Attack)
        // reads as nothing happening at all, which is exactly the bug this floor fixes. Misses (the
        // 0-damage case that should exist) are already handled by the hit-chance roll above.
        int damage = Math.Max(1, BasePhysicalAttackPower - target.DefensePower + variance);

        damage += BlessedUndeadBonus(this, target);

        // Protection (a defensive item effect) can reduce a landed hit all the way to a full
        // block -- distinct from the floor-of-1 above, which only guards against Defense
        // silently no-selling Attack; an intentional mitigation layer is allowed to reach 0.
        int protection = target.GetEquippedItems()
            .SelectMany(i => i.StatusEffects)
            .Where(e => e.EffectType == ItemEffectType.Protection)
            .Sum(e => e.Magnitude);
        damage = Math.Max(0, damage - protection);

        CombatStatsTracker.ApplyDamage(target, damage, this);
        // Once monsters can equip weapons too, this naturally starts reflecting the weapon
        // instead of the bare attack-type noun, the same way it already does for the player.
        target.LastDamageSource = $"{CombatMessages.WithArticle(this)}'s {CombatMessages.AttackWord(AttackType)}";
        target.LastDamageOwner = this;

        var severity = CombatMessages.ClassifySeverity(damage, preDamageHealth);
        return new AttackResult { Attacker = this, Defender = target, Hit = true, Damage = damage, Severity = severity, AttackType = AttackType };
    }

    /// <summary>
    /// Bonus damage a blessed hand-equipped weapon deals specifically against the undead --
    /// checked generically via CreatureType, never by weapon/monster name (see Item.IsBlessed).
    /// Extracted from PhysicalAttack so ProjectileEngine's collision resolver can apply the
    /// same bonus to a fired/thrown blessed weapon, not just a melee swing.
    /// </summary>
    internal static int BlessedUndeadBonus(Actor attacker, Actor target)
    {
        if (target is not Monster { CreatureType: CreatureType.Undead })
        {
            return 0;
        }

        return attacker.GetEquippedItems()
            .Where(i => i.EquipmentType == EquipmentType.Hand && i.IsBlessed)
            .Sum(i => i.BlessedUndeadDamageBonus);
    }
}
