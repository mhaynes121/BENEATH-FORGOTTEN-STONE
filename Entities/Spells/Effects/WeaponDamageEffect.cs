namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

/// <summary>
/// A physical-skill attack that rolls hit/miss through Actor.PhysicalAttack
/// (STR/weapon-based, can miss) rather than DamageEffect's always-hits dice
/// roll -- these are melee strikes (Bash, Kick, Bloodthirst, Shield Slam,
/// Executioner, Assassinate), not spells. Multiplier scales the base attack's
/// damage after the fact (bonus damage applied directly, since PhysicalAttack
/// has no multiplier parameter of its own); 1.0 (default) is just a normal
/// attack. ScaleOffArmor temporarily substitutes the caster's DefensePower
/// for their BasePhysicalAttackPower for the duration of the roll -- Shield
/// Slam deals damage "based entirely on current Armor value." AddShieldDefenseBonus
/// instead ADDS just the equipped shield's own DefenseBonus on top of the caster's
/// normal attack power -- Bash's "normal attack plus shield-assisted bonus," a
/// deliberately narrower and smaller boost than Shield Slam's full-Armor scaling
/// (see Shield-Based Bash Skill proposal's own "Design distinction" section).
/// </summary>
public class WeaponDamageEffect : SpellEffect
{
    public double Multiplier { get; }
    public bool ScaleOffArmor { get; }
    public bool AddShieldDefenseBonus { get; }

    /// <summary>Exorcism: an extra elemental damage roll applied only on a landed hit, on top of the normal physical attack -- see BonusDamageType. Null for every other WeaponDamageEffect skill.</summary>
    public DiceRoll BonusDamageDice { get; }

    /// <summary>The damage type BonusDamageDice resolves against via ResistanceCalculator.ApplyResistance -- e.g. DamageType.Holy for Exorcism. Meaningless (never read) when BonusDamageDice is null.</summary>
    public DamageType? BonusDamageType { get; }

    /// <summary>Exorcism: overrides the generic per-target hit message with custom flavor text, "{target}" replaced with the target's label -- null (every other skill) keeps today's plain severity-word reporting.</summary>
    public string HitMessageTemplate { get; }

    /// <summary>Exorcism: flavor text appended when the attack misses -- null (every other skill) stays silent on a miss, same as today.</summary>
    public string MissMessageTemplate { get; }

    public WeaponDamageEffect(
        double multiplier = 1.0, bool scaleOffArmor = false, bool addShieldDefenseBonus = false,
        DiceRoll bonusDamageDice = null, DamageType? bonusDamageType = null,
        string hitMessageTemplate = null, string missMessageTemplate = null)
    {
        Multiplier = multiplier;
        ScaleOffArmor = scaleOffArmor;
        AddShieldDefenseBonus = addShieldDefenseBonus;
        BonusDamageDice = bonusDamageDice;
        BonusDamageType = bonusDamageType;
        HitMessageTemplate = hitMessageTemplate;
        MissMessageTemplate = missMessageTemplate;
    }

    public override void Apply(SpellCastingContext context, SpellCastResult result)
    {
        var caster = context.Caster;
        int originalPower = caster.BasePhysicalAttackPower;
        if (ScaleOffArmor)
        {
            caster.BasePhysicalAttackPower = caster.DefensePower;
        }
        else if (AddShieldDefenseBonus)
        {
            // Only the equipped shield's own DefenseBonus -- never total DefensePower, which
            // would also pull in body armor, accessories, and temporary Armor effects that have
            // nothing to do with the shield doing the actual bashing. Zero if somehow reached
            // without one equipped (shouldn't happen -- SkillCaster.Cast's RequiresShield gate
            // already rejects the cast before any effect ever runs).
            caster.BasePhysicalAttackPower += FindEquippedShieldDefenseBonus(caster);
        }

        try
        {
            foreach (var target in context.AffectedActors)
            {
                int preDamageHealth = target.Health?.Current ?? 0;
                // The rank-based accuracy adjustment applies only here -- an ordinary attack
                // never passes a non-zero bonusHitChance -- per the Ability Proficiency System's
                // own "does not alter ordinary attacks or general Accuracy" rule.
                var attack = caster.PhysicalAttack(target, context.Rng, context.RankScaling.PercentagePointAdjustment);
                result.AttackHit = attack.Hit;
                if (!attack.Hit)
                {
                    if (MissMessageTemplate != null)
                    {
                        result.FlavorMessages.Add(FormatTemplate(MissMessageTemplate, target));
                    }
                    continue;
                }

                int bonusDamage = Math.Max(0, (int)Math.Round(attack.Damage * (Multiplier - 1)));
                int totalDamage = context.RankScaling.ScaleMagnitude(attack.Damage + bonusDamage);

                // attack.Damage was already applied to target.Health inside caster.PhysicalAttack
                // itself (and already tracked once there) -- only the remaining delta between that
                // and the final rank-scaled total needs applying here, positive (more bonus/rank
                // scaling) or negative (a Novice-rank skill dealing less than a plain attack would).
                int delta = totalDamage - attack.Damage;
                if (delta > 0)
                {
                    CombatStatsTracker.ApplyDamage(target, delta, caster);
                }
                else if (delta < 0)
                {
                    target.Health?.Heal(-delta);
                }

                result.DamageDealt += totalDamage;

                // Exorcism: a separate, resistance-mitigated elemental damage instance layered
                // on top of the physical hit above -- only ever rolled once the physical attack
                // has already connected, per "the Holy damage must explicitly depend on the
                // weapon attack succeeding."
                if (BonusDamageDice != null && BonusDamageType.HasValue)
                {
                    int bonusRoll = context.RankScaling.ScaleMagnitude(BonusDamageDice.Roll(context.Rng));
                    int mitigatedBonus = ResistanceCalculator.ApplyResistance(context.Level, target, BonusDamageType.Value, bonusRoll);
                    CombatStatsTracker.ApplyDamage(target, mitigatedBonus, caster);
                    result.DamageDealt += mitigatedBonus;
                }

                result.DamageInstances.Add((target, CombatMessages.ClassifySeverity(totalDamage, preDamageHealth)));
                target.LastDamageSource = $"{CombatMessages.WithArticle(caster)} using {context.CastName}";

                if (HitMessageTemplate != null)
                {
                    result.FlavorMessages.Add(FormatTemplate(HitMessageTemplate, target));
                }
            }
        }
        finally
        {
            if (ScaleOffArmor || AddShieldDefenseBonus)
            {
                caster.BasePhysicalAttackPower = originalPower;
            }
        }
    }

    /// <summary>The equipped shield's own DefenseBonus (Primary Hand or Off-hand, whichever holds it) -- 0 if none is equipped.</summary>
    private static int FindEquippedShieldDefenseBonus(Actor caster) =>
        caster.GetEquippedItems().FirstOrDefault(i => i.EquipmentCategory == EquipmentCategory.Shield)?.DefenseBonus ?? 0;

    private static string FormatTemplate(string template, Actor target) =>
        template.Replace("{target}", CombatMessages.Label(target, capitalized: false));
}
