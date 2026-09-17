namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

/// <summary>
/// Physical-type damage subtracts the target's DefensePower (same math
/// as Actor.PhysicalAttack); everything else subtracts MagicResistance
/// instead. Adds the caster's matching attack stat, e.g. "2d6 + MagicalAttack".
/// </summary>
public class DamageEffect : SpellEffect
{
    public DiceRoll Dice { get; }
    public DamageType DamageType { get; }

    public DamageEffect(DiceRoll dice, DamageType damageType)
    {
        Dice = dice;
        DamageType = damageType;
    }

    public override void Apply(SpellCastingContext context, SpellCastResult result)
    {
        foreach (var target in context.AffectedActors)
        {
            int roll = Dice.Roll(context.Rng);
            int bonus = DamageType == DamageType.Physical
                ? context.Caster.BasePhysicalAttackPower
                : context.Caster.BaseMagicalAttackPower;
            int resistance = DamageType == DamageType.Physical ? target.DefensePower : target.MagicResistance;

            int damage = Math.Max(0, roll + bonus - resistance);
            damage = context.RankScaling.ScaleMagnitude(damage);
            damage = FloorDamageCalculator.ApplyFloorMultiplier(context.Level, target, DamageType, damage);
            int preDamageHealth = target.Health?.Current ?? 0;
            CombatStatsTracker.ApplyDamage(target, damage, context.Caster);
            result.DamageDealt += damage;

            if (damage > 0)
            {
                result.DamageInstances.Add((target, CombatMessages.ClassifySeverity(damage, preDamageHealth)));
                target.LastDamageSource = $"{CombatMessages.WithArticle(context.Caster)} casting {context.CastName}";
                target.LastDamageOwner = context.Caster;
            }
        }
    }
}
