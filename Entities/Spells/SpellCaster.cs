namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

/// <summary>
/// The spell execution engine: validates a cast, resolves targets,
/// consumes resources, applies effects, and reports what happened.
/// Works for any Actor -- a Spell definition doesn't know or care
/// whether its caster is the player or a monster.
/// </summary>
public static class SpellCaster
{
    /// <param name="consumesMana">False for item-triggered casts (a wand supplies its own magic) -- skips mana and cooldown checks/consumption entirely; the caller is responsible for charge accounting.</param>
    /// <param name="onTargetsResolved">Fires once, immediately after TargetResolver has committed to the affected actors/tile and before any effect actually applies -- lets the caller (GameLoop, SpellCasterAI) play a projectile-flight animation for the trip there. Purely cosmetic: resolution has already succeeded by the time this runs, so it can never change what the spell hits. Null for every existing caller except a projectile-spell cast, so ordinary casts (including every monster cast today) are completely unaffected.</param>
    public static SpellCastResult Cast(Spell spell, SpellCastingContext context, bool consumesMana = true, Action<SpellCastingContext> onTargetsResolved = null)
    {
        var result = new SpellCastResult();
        var caster = context.Caster;
        context.CastName = spell.Name;

        if (consumesMana && caster.Mana.Current < spell.Casting.ManaCost)
        {
            result.FailureReason = "Not enough mana.";
            result.Message = result.FailureReason;
            return result;
        }

        if (consumesMana && caster.SpellCooldowns.TryGetValue(spell, out int readyTurn) && context.TurnNumber < readyTurn)
        {
            result.FailureReason = $"{spell.Name} is not ready yet.";
            result.Message = result.FailureReason;
            return result;
        }

        if (!TargetResolver.ResolveAffectedActors(spell.Targeting, context, out string failureReason))
        {
            result.FailureReason = failureReason;
            result.Message = failureReason;
            return result;
        }

        if (consumesMana)
        {
            caster.Mana.TrySpend(spell.Casting.ManaCost);
            result.ManaConsumed = spell.Casting.ManaCost;

            if (spell.Casting.Cooldown > 0)
            {
                caster.SpellCooldowns[spell] = context.TurnNumber + spell.Casting.Cooldown;
            }
        }

        onTargetsResolved?.Invoke(context);

        foreach (var effect in spell.Effects)
        {
            effect.Apply(context, result);
        }

        result.Success = true;
        result.Message = BuildMessage(spell, caster, result);
        return result;
    }

    /// <summary>Damage is reported as severity words (minor/major/mortal/...), same as melee combat via CombatMessages -- not the raw number, and not the total across every target hit by an AoE, but one clause per actor actually damaged.</summary>
    private static string BuildMessage(Spell spell, Actor caster, SpellCastResult result)
    {
        string message = spell.FlavorCastMessage ?? $"{caster.DisplayName} casts {spell.Name}.";

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
