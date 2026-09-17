using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities;
using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Core;

/// <summary>
/// Ticks and expires every actor's ActiveEffects once per completed
/// player turn: applies DoT/status tick damage, and reverts stat
/// modifiers whose duration has run out.
/// </summary>
public static class EffectProcessor
{
    /// <returns>Status-line messages for effects that expired this tick -- the player's own always, plus any VISIBLE monster's (see BuildExpirationMessage/the loop below), oldest first, for GameLoop to append via AddStatusMessage.</returns>
    public static List<string> Tick(Level level)
    {
        var messages = new List<string>();

        foreach (var actor in level.Actors)
        {
            if (!actor.IsAlive || !actor.CanBeTargeted)
            {
                continue;
            }

            // Regeneration (a passive defensive item effect) heals every tick regardless of
            // whether the actor has any ActiveEffects at all -- it isn't time-limited, so it
            // isn't ActiveEffect-backed; just a per-turn scan of whatever's equipped.
            int regen = actor.GetEquippedItems()
                .SelectMany(i => i.StatusEffects)
                .Where(e => e.EffectType == ItemEffectType.Regeneration)
                .Sum(e => e.Magnitude);
            if (regen > 0)
            {
                actor.Health?.Heal(regen);
            }

            if (actor.ActiveEffects.Count == 0)
            {
                continue;
            }

            var expired = new List<ActiveEffect>();

            foreach (var effect in actor.ActiveEffects)
            {
                if (effect.TickDamage > 0)
                {
                    // FloorDamageCalculator composes the floor's own elemental multiplier with
                    // the target's full resistance (race/class/stat/equipment/buffs/environment
                    // -- see ResistanceCalculator) in one centralized step.
                    int damage = FloorDamageCalculator.ApplyFloorMultiplier(level, actor, effect.TickDamageType, effect.TickDamage);
                    CombatStatsTracker.ApplyDamage(actor, damage, effect.Owner);
                    if (damage > 0 && !string.IsNullOrEmpty(effect.DamageSourceDescription))
                    {
                        actor.LastDamageSource = effect.DamageSourceDescription;
                        actor.LastDamageOwner = effect.Owner;
                    }
                }

                if (level.TurnNumber >= effect.ExpiresOnTurn)
                {
                    expired.Add(effect);
                }
            }

            foreach (var effect in expired)
            {
                if (effect.ModifiedStat.HasValue)
                {
                    StatModifierEffect.ApplyStatDelta(actor, effect.ModifiedStat.Value, -effect.StatAmount);
                }
                actor.ActiveEffects.Remove(effect);

                // The player's own expirations always show; a monster's only shows while it's
                // actually visible -- an off-screen monster's buff/debuff wearing off is the
                // same kind of unannounced turn-to-turn state its movement already is. Skip
                // entirely if this tick's own damage just killed them (no expiration to speak
                // of -- HandleDeath's own message already covers the kill).
                bool isVisible = actor is Player || level.Tiles[actor.X, actor.Y].IsVisible;
                if (actor.IsAlive && isVisible)
                {
                    messages.Add(BuildExpirationMessage(actor, effect.SourceSpellName));
                }
            }
        }

        return messages;
    }

    /// <summary>
    /// Atmospheric, in-world phrasing for an effect wearing off, in place of a flat "Your X
    /// fades." -- known ailments (Burning/Frostbitten/Poisoned/Shocked/Bleeding/Corroded, plus
    /// Poison Weapon's own poison) get a matching sensory description; anything else (a named
    /// spell/buff like Bless or Haste) falls back to the original phrasing, which already reads
    /// fine for a proper spell name. Grammatically correct for either the player ("your") or a
    /// named monster ("the goblin's") via the same possessive substitution.
    /// </summary>
    private static string BuildExpirationMessage(Actor actor, string statusName)
    {
        // Arcane Orb/Divine Radiance's expiration lines (design spec sections 16/17) don't fit
        // the generic possessive template below -- neither is phrased as "your X does Y".
        if (statusName == "Arcane Orb")
        {
            return actor is Player
                ? "The glowing orb fades into nothingness."
                : $"The glowing orb around {CombatMessages.Label(actor, capitalized: false)} fades into nothingness.";
        }
        if (statusName == "Divine Radiance")
        {
            return actor is Player
                ? "The holy radiance around you slowly fades."
                : $"The holy radiance around {CombatMessages.Label(actor, capitalized: false)} slowly fades.";
        }

        string possessive = actor is Player ? "your" : $"{CombatMessages.Label(actor, capitalized: false)}'s";

        string sentence = statusName switch
        {
            "Burning" => $"{possessive} burns die down.",
            "Frostbitten" => $"the frost melts from {possessive} skin.",
            "Poisoned" or "Poison Weapon" => $"the poison fades from {possessive} veins.",
            "Shocked" => $"the last crackle of electricity leaves {possessive} body.",
            "Bleeding" => $"{possessive} bleeding stops.",
            "Corroded" => $"the corrosion on {possessive} armor stops spreading.",
            _ => $"{possessive} {statusName} fades."
        };

        return char.ToUpperInvariant(sentence[0]) + sentence[1..];
    }
}
