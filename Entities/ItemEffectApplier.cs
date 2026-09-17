using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities.Components;
using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Applies a weapon's on-hit ItemStatusEffect entries to the defender, and a defender's
/// Thorns to the attacker, right after a successful PhysicalAttack -- called from every
/// existing attack site (GameLoop.HandleMove's main-hand/off-hand swings, ChaseAI.TakeTurn),
/// alongside the pre-existing Poison Weapon skill proc it deliberately mirrors: builds a bare
/// ActiveEffect (or sets StunnedUntilTurn directly) rather than routing through
/// SpellCastingContext/StatusEffect.Apply, since no spell is being cast on a melee swing.
/// </summary>
public static class ItemEffectApplier
{
    public static string ApplyOnHitEffects(AttackResult result, Level level, Random rng)
    {
        if (!result.Hit || !result.Defender.IsAlive)
        {
            return null;
        }

        var messages = new List<string>();

        var weaponEffects = result.Attacker.GetEquippedItems()
            .Where(i => i.EquipmentType == EquipmentType.Hand)
            .SelectMany(i => i.StatusEffects);
        string weaponMessage = ApplyStatusEffects(result.Attacker, result.Defender, weaponEffects, level, rng);
        if (weaponMessage != null)
        {
            messages.Add(weaponMessage);
        }

        if (!result.Defender.IsAlive)
        {
            return string.Join(" ", messages);
        }

        if (result.Attacker.IsAlive)
        {
            var thorns = result.Defender.GetEquippedItems()
                .SelectMany(i => i.StatusEffects)
                .FirstOrDefault(e => e.EffectType == ItemEffectType.Thorns);
            if (thorns != null)
            {
                int preDamageHealth = result.Attacker.Health?.Current ?? 0;
                CombatStatsTracker.ApplyDamage(result.Attacker, thorns.Magnitude, result.Defender);
                result.Attacker.LastDamageSource = $"{CombatMessages.WithArticle(result.Defender)}'s thorns";
                result.Attacker.LastDamageOwner = result.Defender;
                string severityWord = CombatMessages.SeverityWord(CombatMessages.ClassifySeverity(thorns.Magnitude, preDamageHealth));
                messages.Add($"{TargetLabel(result.Attacker)} {(result.Attacker is Player ? "take" : "takes")} {severityWord} damage from thorns.");
            }
        }

        return messages.Count > 0 ? string.Join(" ", messages) : null;
    }

    /// <summary>
    /// Applies a set of offensive ItemStatusEffect entries to a target, one by one, stopping
    /// early if the target dies partway through -- the shared core ApplyOnHitEffects wraps for
    /// melee (effects sourced from the attacker's hand-equipped weapons), and ProjectileEngine
    /// reuses directly for arrows/bolts/thrown weapons/spell projectiles (effects sourced from
    /// the fired weapon+ammo or spell instead), so on-hit fire/frost/poison/stun/etc. behave
    /// identically no matter how the hit was delivered.
    /// </summary>
    public static string ApplyStatusEffects(Actor attacker, Actor target, IEnumerable<ItemStatusEffect> effects, Level level, Random rng)
    {
        if (!target.IsAlive)
        {
            return null;
        }

        var messages = new List<string>();
        foreach (var effect in effects)
        {
            if (!IsOffensive(effect.EffectType))
            {
                continue;
            }

            // Resistance reduces the chance an associated elemental status actually takes hold
            // (Resistance System spec section 27) -- Stun/Corrode have no DamageType mapping
            // (DamageTypeFor returns null for them), so AdjustStatusChance is a no-op there and
            // their Chance rolls exactly as authored, unaffected.
            var damageType = DamageTypeFor(effect.EffectType);
            double effectiveChance = damageType == null
                ? effect.Chance
                : ResistanceCalculator.AdjustStatusChance(level, target, damageType.Value, effect.Chance);
            if (rng.NextDouble() >= effectiveChance)
            {
                continue;
            }

            string message = ApplyOffensiveEffect(effect, attacker, target, level);
            if (message != null)
            {
                messages.Add(message);
            }

            if (!target.IsAlive)
            {
                break;
            }
        }

        return messages.Count > 0 ? string.Join(" ", messages) : null;
    }

    private static bool IsOffensive(ItemEffectType type) => type switch
    {
        ItemEffectType.Fire or ItemEffectType.Frost or ItemEffectType.Poison or ItemEffectType.Bleed
            or ItemEffectType.Stun or ItemEffectType.Shock or ItemEffectType.Corrode => true,
        _ => false
    };

    private static string ApplyOffensiveEffect(ItemStatusEffect effect, Actor attacker, Actor target, Level level)
    {
        if (effect.EffectType == ItemEffectType.Stun)
        {
            if (level.TurnNumber < target.CcImmuneUntilTurn)
            {
                return null;
            }
            target.StunnedUntilTurn = Math.Max(target.StunnedUntilTurn, level.TurnNumber + effect.Duration);
            return $"{TargetLabel(target)} {(target is Player ? "are" : "is")} stunned!";
        }

        if (effect.EffectType == ItemEffectType.Corrode)
        {
            target.ActiveEffects.Add(new ActiveEffect("Corroded", level.TurnNumber + effect.Duration)
            {
                ModifiedStat = Stat.Armor,
                StatAmount = -effect.Magnitude
            });
            StatModifierEffect.ApplyStatDelta(target, Stat.Armor, -effect.Magnitude);
            return $"{TargetLabel(target)}'s armor corrodes!";
        }

        var damageType = DamageTypeFor(effect.EffectType);
        if (damageType == null)
        {
            return null; // defensive types never reach here (see IsOffensive)
        }

        string statusName = StatusDisplayName(effect.EffectType);

        if (effect.Duration > 0)
        {
            // Resistance shortens an elemental status's duration too, at half the strength it
            // affects damage/chance (Resistance System spec sections 28/30).
            int duration = ResistanceCalculator.AdjustStatusDuration(level, target, damageType.Value, effect.Duration);
            target.ActiveEffects.Add(new ActiveEffect(statusName, level.TurnNumber + duration)
            {
                TickDamage = effect.Magnitude,
                TickDamageType = damageType.Value,
                DamageSourceDescription = DeathCausePhrase(attacker, statusName),
                Owner = attacker
            });
            return $"{TargetLabel(target)} {(target is Player ? "are" : "is")} {statusName.ToLower()}!";
        }

        // Resistance (race/class/stat/equipment/buffs/environment, all in one place) applies
        // here via the same centralized step FloorDamageCalculator's floor-multiplier callers
        // already use -- see ResistanceCalculator, which this composes with the floor's own
        // elemental multiplier.
        int resisted = FloorDamageCalculator.ApplyFloorMultiplier(level, target, damageType.Value, effect.Magnitude);
        if (resisted <= 0)
        {
            return null;
        }
        int preDamageHealth = target.Health?.Current ?? 0;
        CombatStatsTracker.ApplyDamage(target, resisted, attacker);
        target.LastDamageSource = DeathCausePhrase(attacker, statusName);
        target.LastDamageOwner = attacker;
        string severityWord = CombatMessages.SeverityWord(CombatMessages.ClassifySeverity(resisted, preDamageHealth));
        return $"{TargetLabel(target)} {(target is Player ? "take" : "takes")} {severityWord} {effect.EffectType.ToString().ToLower()} damage.";
    }

    private static DamageType? DamageTypeFor(ItemEffectType type) => type switch
    {
        ItemEffectType.Fire => DamageType.Fire,
        ItemEffectType.Frost => DamageType.Ice,
        ItemEffectType.Poison => DamageType.Poison,
        ItemEffectType.Shock => DamageType.Lightning,
        ItemEffectType.Bleed => DamageType.Physical,
        _ => null
    };

    /// <summary>"an orc's poisoned bite"/"Volgrim Emberblood's burning slash" -- pairs the status adjective (Poisoned/Burning/...) with the attack's actual verb-noun (AttackWord) instead of using the bare adjective as if it were a noun on its own ("...'s Poisoned"), which read as broken grammar in cause-of-death text.</summary>
    private static string DeathCausePhrase(Actor attacker, string statusName) =>
        $"{CombatMessages.WithArticle(attacker)}'s {statusName.ToLowerInvariant()} {CombatMessages.AttackWord(attacker.AttackType)}";

    private static string StatusDisplayName(ItemEffectType type) => type switch
    {
        ItemEffectType.Fire => "Burning",
        ItemEffectType.Frost => "Frostbitten",
        ItemEffectType.Poison => "Poisoned",
        ItemEffectType.Shock => "Shocked",
        ItemEffectType.Bleed => "Bleeding",
        _ => type.ToString()
    };

    private static string TargetLabel(Actor actor) => CombatMessages.Label(actor, capitalized: true);
}
