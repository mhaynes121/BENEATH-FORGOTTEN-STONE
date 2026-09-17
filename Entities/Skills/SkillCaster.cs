using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Entities.Skills;

/// <summary>
/// The physical-skill execution engine -- mirrors SpellCaster.Cast closely,
/// minus mana (a skill costs nothing but its cooldown) and plus the two
/// skill-only preconditions (RequiresShield, target-HP% gates) that have no
/// analog on any of the 39 magic spells. Cooldowns are tracked on
/// Player.SkillCooldowns rather than Actor.SpellCooldowns since only the
/// player ever knows a skill in this solo game -- no monster archetype
/// grants one. Only ever called for active skills; passives apply their
/// effect once via Skill.OnGrant and never reach here.
/// </summary>
public static class SkillCaster
{
    public static SpellCastResult Cast(Skill skill, SpellCastingContext context)
    {
        var result = new SpellCastResult();
        var player = (Player)context.Caster;
        context.CastName = skill.Name;

        if (player.SkillCooldowns.TryGetValue(skill, out int readyTurn) && context.TurnNumber < readyTurn)
        {
            result.FailureReason = $"{skill.Name} is not ready yet.";
            result.Message = result.FailureReason;
            return result;
        }

        // A shield can end up auto-equipped into either hand slot (e.g. no weapon equipped
        // at all), so check both rather than assuming OffHand specifically.
        bool hasShieldEquipped = player.Equipment.Get(EquipmentSlot.PrimaryHand)?.EquipmentCategory == EquipmentCategory.Shield
            || player.Equipment.Get(EquipmentSlot.OffHand)?.EquipmentCategory == EquipmentCategory.Shield;
        if (skill.RequiresShield && !hasShieldEquipped)
        {
            result.FailureReason = $"{skill.Name} requires a shield.";
            result.Message = result.FailureReason;
            return result;
        }

        bool hasMeleeWeaponEquipped = player.Equipment.Get(EquipmentSlot.PrimaryHand)?.EquipmentCategory == EquipmentCategory.Weapon
            || player.Equipment.Get(EquipmentSlot.OffHand)?.EquipmentCategory == EquipmentCategory.Weapon;
        if (skill.RequiresMeleeWeapon && !hasMeleeWeaponEquipped)
        {
            result.FailureReason = $"{skill.Name} requires a melee weapon.";
            result.Message = result.FailureReason;
            return result;
        }

        if (!TargetResolver.ResolveAffectedActors(skill.Targeting, context, out string failureReason))
        {
            result.FailureReason = failureReason;
            result.Message = failureReason;
            return result;
        }

        if (!MeetsHealthThreshold(skill, context.TargetActor, out string thresholdFailure))
        {
            result.FailureReason = thresholdFailure;
            result.Message = thresholdFailure;
            return result;
        }

        if (skill.RequiresUnalertedSneakTarget)
        {
            bool targetAlerted = context.TargetActor is not Monster { IsAlerted: false };
            if (!player.IsSneaking || targetAlerted)
            {
                result.FailureReason = $"{skill.Name} only works while Sneaking against a target that hasn't noticed you.";
                result.Message = result.FailureReason;
                return result;
            }
        }

        if (skill.PreconditionCheck != null)
        {
            string preconditionFailure = skill.PreconditionCheck(context);
            if (preconditionFailure != null)
            {
                result.FailureReason = preconditionFailure;
                result.Message = preconditionFailure;
                return result;
            }
        }

        if (skill.Casting.Cooldown > 0)
        {
            player.SkillCooldowns[skill] = context.TurnNumber + skill.Casting.Cooldown;
        }

        foreach (var effect in skill.Effects)
        {
            effect.Apply(context, result);
        }

        result.Success = true;
        result.Message = BuildMessage(skill, player, result);
        return result;
    }

    private static bool MeetsHealthThreshold(Skill skill, Actor target, out string failureReason)
    {
        failureReason = "";
        if (!skill.TargetHealthPercentAtLeast.HasValue && !skill.TargetHealthPercentBelow.HasValue)
        {
            return true;
        }

        // Self/no-target skills never set these gates, so a null target here would be a
        // catalog mistake -- fail loudly rather than silently treating it as 0% health.
        double percent = (double)target.Health.Current / target.Health.Max;

        if (skill.TargetHealthPercentAtLeast.HasValue && percent < skill.TargetHealthPercentAtLeast.Value)
        {
            failureReason = $"{skill.Name} only works against a target at full health.";
            return false;
        }

        if (skill.TargetHealthPercentBelow.HasValue && percent >= skill.TargetHealthPercentBelow.Value)
        {
            failureReason = $"{skill.Name} only works against a badly wounded target.";
            return false;
        }

        return true;
    }

    /// <summary>Mirrors SpellCaster.BuildMessage's severity-word reporting exactly, just with "uses" instead of "casts".</summary>
    private static string BuildMessage(Skill skill, Actor caster, SpellCastResult result)
    {
        string message = skill.FlavorCastMessage ?? $"{caster.DisplayName} uses {skill.Name}.";

        if (result.DamageInstances.Count > 0)
        {
            var clauses = result.DamageInstances.Select(instance =>
            {
                string targetName = CombatMessages.Label(instance.Target, capitalized: false);
                return $"{CombatMessages.SeverityWord(instance.Severity)} damage to {targetName}";
            });
            message += $" ({string.Join(", ", clauses)})";
        }

        if (result.HealInstances.Count > 0)
        {
            var healClauses = result.HealInstances.Select(instance =>
            {
                string targetName = CombatMessages.Label(instance.Target, capitalized: false);
                return $"{CombatMessages.SeverityWord(instance.Severity)} healing to {targetName}";
            });
            message += $" ({string.Join(", ", healClauses)})";
        }

        if (result.IdentifiedItemName != null)
        {
            message += $" You identify the {result.IdentifiedItemName}.";
        }
        else if (result.FailedIdentifyItemName != null)
        {
            message += $" You fail to identify the {result.FailedIdentifyItemName}.";
        }

        foreach (var flavorMessage in result.FlavorMessages)
        {
            message += $" {flavorMessage}";
        }

        return message;
    }
}
