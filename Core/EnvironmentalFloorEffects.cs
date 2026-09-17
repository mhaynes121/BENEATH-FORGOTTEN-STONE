using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities;
using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Core;

/// <summary>
/// Fire/Lava damage for standing on the tile itself, plus (see ApplyFloorAttunementAttrition)
/// the tile-attuned monster system's off-preferred-floor attrition -- both called once per
/// completed normal player turn from GameLoop's own centralized end-of-turn phase (the same
/// spot TurnCount, natural regeneration, and EffectProcessor.Tick already run from), never from
/// movement or individual commands, so entering a tile and then WAITing on it both apply damage
/// exactly once per turn rather than twice or zero times.
///
/// The raw per-tile amount is deliberately NOT routed through FloorDamageCalculator's floor
/// multiplier -- that answers "how does the floor react to an incoming attack of a given
/// DamageType"; this damage IS the floor, so running it back through its own multiplier would
/// double-count. It IS routed through ResistanceCalculator.ApplyResistance directly, though
/// (Resistance System spec section 60: elemental resistance should still reduce environmental
/// Fire/Lava damage, just not the floor's own self-multiplier) -- Lava environmental damage
/// uses DamageType.Fire, same as Fire's, per that section's own guidance.
/// </summary>
public static class EnvironmentalFloorEffects
{
    /// <returns>A status message if damage was applied, otherwise null. Only ever non-null for the player -- see ApplyToNonPlayerActors for the same Fire/Lava damage applied to every monster/pet on the level, which stays silent per-tick like every other DoT (EffectProcessor.Tick's own precedent), surfacing only through the normal death message once/if it kills one.</returns>
    public static string Apply(Level level, Player player)
    {
        if (!player.IsAlive)
        {
            return null;
        }

        int damage = CalculateEnvironmentalDamage(level, player, player.X, player.Y, out string source);
        if (damage == 0)
        {
            return null;
        }

        int preDamageHealth = player.Health?.Current ?? 0;
        CombatStatsTracker.ApplyDamage(player, damage, owner: null);
        player.LastDamageSource = source;
        player.LastDamageOwner = null; // environmental -- never credited as a kill for reward purposes, even if a prior hit left a stale owner
        string severityWord = CombatMessages.SeverityWord(CombatMessages.ClassifySeverity(damage, preDamageHealth));
        return source == "the lava" ? $"The lava burns you for {severityWord} damage!" : $"The flames burn you for {severityWord} damage!";
    }

    /// <summary>
    /// The same Fire/Lava tile damage as Apply above, but for every living non-player actor on
    /// the level -- previously player-only, generalized so a floor-attuned monster standing on
    /// Fire/Lava genuinely suffers the existing environmental damage IN ADDITION TO its own
    /// off-preferred-floor attrition (design spec section 10: "does not automatically make
    /// immune to every other floor effect"). Applies to every monster, not just floor-attuned
    /// ones, matching how the player's own version never checked for a special monster-only
    /// property either. Also applies to a Pet (Pet and Companion System) -- without this, a
    /// pet standing on Fire/Lava would take no damage at all, making PetAI's hazard-avoidance
    /// pathing protect against a mechanic that couldn't actually hurt it.
    /// </summary>
    public static void ApplyToNonPlayerActors(Level level)
    {
        foreach (var actor in level.Actors.Where(a => a.IsAlive && a is Monster or Pet))
        {
            int damage = CalculateEnvironmentalDamage(level, actor, actor.X, actor.Y, out string source);
            if (damage == 0)
            {
                continue;
            }
            CombatStatsTracker.ApplyDamage(actor, damage, owner: null);
            actor.LastDamageSource = source;
            actor.LastDamageOwner = null; // environmental -- see Apply above
        }
    }

    /// <summary>
    /// Safe Monster Spawning and Hazard-Aware Movement: a non-mutating query for "how much
    /// environmental Fire/Lava tick damage would `target` take by standing at (x, y) right now" --
    /// exposed publicly (rather than kept private and only ever evaluated at the actor's OWN
    /// current position) so ActorTerrainSafety can ask the identical question about a candidate
    /// spawn/movement tile the actor doesn't occupy yet, without maintaining a second hard-coded
    /// Fire/Lava rule that could drift out of sync with this one. Never changes health or damage
    /// ownership -- Apply/ApplyToNonPlayerActors are the only things that actually apply the
    /// number this returns.
    /// </summary>
    /// <param name="target">
    /// A Monster whose ElementalAffinity matches this exact tile's own EnvironmentalDamageType AND
    /// whose PreferredFloorType is this exact FloorType is immune to the environmental TICK here
    /// (a Fire-attuned monster standing on Fire; a Lava-attuned one standing on Lava) -- immunity
    /// to the tick specifically, not to Fire damage in general: an attack of DamageType.Fire from
    /// another source still resolves through the normal resistance pipeline below, untouched by
    /// this check. A Fire-attuned monster standing on Lava does NOT match (different FloorType)
    /// and is not immune here, even though Lava's own environmental damage is also DamageType.Fire.
    /// </param>
    public static int CalculateEnvironmentalDamage(Level level, Actor target, int x, int y, out string source)
    {
        var tile = level.Tiles[x, y];
        var definition = FloorTypeCatalog.Get(tile.FloorType);
        if (!definition.EnvironmentalDamageEnabled)
        {
            source = null;
            return 0;
        }

        if (target is Monster { IsFloorAttuned: true } monster
            && monster.PreferredFloorType == tile.FloorType && monster.ElementalAffinity == definition.EnvironmentalDamageType)
        {
            source = null;
            return 0;
        }

        int dungeonLevel = Math.Max(1, level.FloorIndex);
        source = tile.FloorType == FloorType.Lava ? "the lava" : "the flames";
        int rawDamage = tile.FloorType switch
        {
            FloorType.Lava => FloorTypeConfig.BaseLavaDamage + ((dungeonLevel - 1) * FloorTypeConfig.LavaDamagePerDungeonLevel),
            _ => FloorTypeConfig.BaseFireDamage + ((dungeonLevel - 1) * FloorTypeConfig.FireDamagePerDungeonLevel)
        };
        return ResistanceCalculator.ApplyResistance(level, target, DamageType.Fire, rawDamage);
    }

    /// <summary>Non-mutating predicate mirroring ApplyFloorAttunementAttrition's own "is this monster off its preferred terrain" condition, so ActorTerrainSafety can ask the identical question about a candidate tile without duplicating the rule.</summary>
    public static bool WouldTakeFloorAttunementAttrition(Monster monster, FloorType currentFloorType) =>
        monster.IsFloorAttuned && currentFloorType != monster.PreferredFloorType;

    /// <returns>Status messages for any VISIBLE floor-attuned monster that took off-preferred-floor attrition this turn (design spec sections 6-8, 43) -- an off-screen monster's attrition still applies (it just never shows), same visibility rule Renderer/ShouldRenderTurn already use elsewhere, to avoid spamming the 2-message status history with creatures the player can't even see.</returns>
    public static List<string> ApplyFloorAttunementAttrition(Level level)
    {
        var messages = new List<string>();

        foreach (var monster in level.Actors.OfType<Monster>().Where(m => m.IsAlive && m.IsFloorAttuned))
        {
            if (!WouldTakeFloorAttunementAttrition(monster, level.Tiles[monster.X, monster.Y].FloorType))
            {
                continue;
            }

            // Always MaxHP, never CurrentHP (design spec section 6) -- otherwise the damage
            // would shrink as the monster gets hurt, undermining the "at least 1" floor below.
            int damage = Math.Max(1, (int)Math.Ceiling(monster.Health.Max * monster.OffPreferredFloorDamagePercent));
            int preDamageHealth = monster.Health.Current;
            CombatStatsTracker.ApplyDamage(monster, damage, owner: null);
            string preferredTerrainName = monster.PreferredFloorType.Value.ToString().ToLowerInvariant();
            monster.LastDamageSource = $"being away from {preferredTerrainName}";
            monster.LastDamageOwner = null; // environmental -- see Apply above

            if (level.Tiles[monster.X, monster.Y].IsVisible)
            {
                string severityWord = CombatMessages.SeverityWord(CombatMessages.ClassifySeverity(damage, preDamageHealth));
                messages.Add($"{CombatMessages.Label(monster, capitalized: true)} withers away from the {preferredTerrainName} for {severityWord} damage.");
            }
        }

        return messages;
    }
}
