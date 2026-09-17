using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>
/// Centralized resistance application -- the one place incoming elemental/magic damage,
/// on-hit status application chance, and status duration all get adjusted by a target's
/// effective resistance (design spec sections 23-30). Dispatches to Player.GetEffectiveResistance
/// or Monster.GetEffectiveResistance depending on the target's actual type, so every caller here
/// (and in FloorDamageCalculator/ItemEffectApplier/StatusEffect) stays agnostic to which kind of
/// Actor it's working with. A DamageType with no ResistanceTypeMapping (Physical foremost -- see
/// its own doc comment) passes through every method here completely unaffected.
/// </summary>
public static class ResistanceCalculator
{
    /// <summary>FinalDamage = IncomingDamage * (1 - Resistance / 100). Never used for Physical damage (see ResistanceTypeMapping) -- melee continues to use the existing Armor/Defense system exclusively.</summary>
    public static int ApplyResistance(Level level, Actor target, DamageType damageType, int damage)
    {
        if (damage <= 0)
        {
            return damage;
        }

        var resistanceType = ResistanceTypeMapping.ForDamageType(damageType);
        if (resistanceType == null)
        {
            return damage;
        }

        int resistance = GetEffectiveResistance(target, resistanceType.Value, level);
        double multiplier = 1.0 - (resistance / 100.0);
        if (multiplier == 1.0)
        {
            return damage;
        }

        // Same floor-of-1 guarantee FloorDamageCalculator's own multiplier already uses -- a
        // dampening resistance should never accidentally zero out a hit that otherwise landed.
        return Math.Max(1, (int)Math.Round(damage * multiplier));
    }

    /// <summary>FinalChance = BaseChance * (1 - Resistance / 100), clamped to a real probability -- negative resistance can push this above the base chance, never above 100%.</summary>
    public static double AdjustStatusChance(Level level, Actor target, DamageType damageType, double baseChance)
    {
        var resistanceType = ResistanceTypeMapping.ForDamageType(damageType);
        if (resistanceType == null)
        {
            return baseChance;
        }

        int resistance = GetEffectiveResistance(target, resistanceType.Value, level);
        return Math.Clamp(baseChance * (1.0 - (resistance / 100.0)), 0.0, 1.0);
    }

    /// <summary>Same shape as AdjustStatusChance, but at half strength (ResistanceConfig.StatusDurationResistanceFactor) -- a status a target resists still lingers longer than the damage/chance reduction alone would suggest (design spec sections 28/30).</summary>
    public static int AdjustStatusDuration(Level level, Actor target, DamageType damageType, int baseDuration)
    {
        var resistanceType = ResistanceTypeMapping.ForDamageType(damageType);
        if (resistanceType == null)
        {
            return baseDuration;
        }

        int resistance = GetEffectiveResistance(target, resistanceType.Value, level);
        double reductionPercent = resistance * ResistanceConfig.StatusDurationResistanceFactor;
        return Math.Max(0, (int)Math.Round(baseDuration * (1.0 - (reductionPercent / 100.0))));
    }

    /// <summary>The only place that needs to know Player and Monster compute their own effective resistance differently (a Monster additionally needs its current tile's FloorType, for its own preferred-terrain environmental bonus -- see Monster.GetEffectiveResistance). Every other Actor subtype has no resistance concept and simply contributes 0.</summary>
    public static int GetEffectiveResistance(Actor target, ResistanceType type, Level level) => target switch
    {
        Player player => player.GetEffectiveResistance(type),
        Monster monster => monster.GetEffectiveResistance(type, level.Tiles[monster.X, monster.Y].FloorType),
        _ => 0
    };

    /// <summary>Shared by Player/Monster's own GetEffectiveResistance -- sums ResistanceModifiers from currently equipped items, split into non-cursed vs. cursed subtotals so GetResistanceBreakdown can show them as separate lines (design spec section 41's own example format) even though they feed into the same effective total.</summary>
    internal static int SumEquipmentResistance(Actor actor, ResistanceType type, bool cursedOnly) =>
        actor.GetEquippedItems().Where(i => i.IsCursed == cursedOnly).Sum(i => i.ResistanceModifiers.Get(type));

    /// <summary>Live sum of every currently-active buff/debuff's resistance contribution -- no apply/revert bookkeeping needed (unlike StatModifierEffect's stat deltas): the moment EffectProcessor.Tick removes an expired ActiveEffect from the list, it simply stops being summed here.</summary>
    internal static int SumActiveEffectResistance(Actor actor, ResistanceType type) =>
        actor.ActiveEffects.Where(e => e.ModifiedResistanceType == type).Sum(e => e.ResistanceAmount);
}
