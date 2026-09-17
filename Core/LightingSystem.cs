using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities;
using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Core;

/// <summary>
/// Illumination: which tiles an active light source (a lit physical item, or a spell like
/// Arcane Orb/Divine Radiance) currently reaches, and per-turn upkeep of physical light sources
/// (burn duration, random extinguishing). Deliberately doesn't model a single "the player's
/// light" concept -- every actor's lit items and light-granting ActiveEffects are collected the
/// same way, so multiple simultaneous sources (design spec section 18) and a future non-player
/// light carrier (section 20) both fall out for free with no special-casing.
/// </summary>
public static class LightingSystem
{
    /// <summary>
    /// Recomputes Tile.IsIlluminated for the whole level from every currently active light
    /// source, unioned together (a tile is illuminated if ANY source reaches it). Must run
    /// before FieldOfView.Compute -- see that method's own doc comment on why. Reuses
    /// FieldOfView.ComputeVisibleCells for each source's own reach, so light automatically
    /// respects walls (design spec section 4) with no separate geometry of its own.
    /// </summary>
    public static void RecomputeIllumination(Level level)
    {
        for (int x = 0; x < level.Width; x++)
        {
            for (int y = 0; y < level.Height; y++)
            {
                level.Tiles[x, y].IsIlluminated = false;
            }
        }

        foreach (var source in CollectActiveLightSources(level))
        {
            foreach (var (x, y) in FieldOfView.ComputeVisibleCells(level, source.X, source.Y, source.Radius))
            {
                level.Tiles[x, y].IsIlluminated = true;
            }
        }
    }

    /// <summary>Every currently active light source on the level: a lit physical item (position = its carrying actor's), or a light-granting ActiveEffect (Arcane Orb, Divine Radiance -- position = its owning actor's). Internal so Diagnostics/SelfTest.cs can verify source collection without a full illumination pass.</summary>
    internal static List<(int X, int Y, int Radius)> CollectActiveLightSources(Level level)
    {
        var sources = new List<(int X, int Y, int Radius)>();

        foreach (var actor in level.Actors)
        {
            if (!actor.IsAlive)
            {
                continue;
            }

            foreach (var item in actor.Inventory.Items)
            {
                if (item.EmitsLight && item.IsLit)
                {
                    sources.Add((actor.X, actor.Y, item.LightRadius));
                }
            }

            foreach (var effect in actor.ActiveEffects)
            {
                if (effect.LightRadius.HasValue)
                {
                    sources.Add((actor.X, actor.Y, effect.LightRadius.Value));
                }
            }
        }

        return sources;
    }

    /// <summary>
    /// Once-per-completed-turn upkeep for every lit physical light source on the level: burns
    /// down RemainingLightDuration, extinguishes automatically at 0 (destroying the item too if
    /// DestroyedWhenLightExhausted -- Candle/Torch, not Lantern), and separately rolls each
    /// item's own ExtinguishChancePerTurn for an early, random snuff-out that does NOT touch
    /// remaining duration (design spec section 9). Only the player's own lights are worth a
    /// message -- no monster carries/lights a physical source yet (section 20 is forward-looking
    /// infrastructure, not a reachable case today), matching EffectProcessor's own "only report
    /// what the player would actually notice" precedent.
    /// </summary>
    public static List<string> ProcessTurn(Player player, Random rng)
    {
        var messages = new List<string>();

        foreach (var item in player.Inventory.Items.ToList())
        {
            if (!item.EmitsLight || !item.IsLit)
            {
                continue;
            }

            item.RemainingLightDuration--;
            if (item.RemainingLightDuration <= 0)
            {
                item.RemainingLightDuration = 0;
                item.IsLit = false;
                messages.Add(BurnedOutMessage(item));
                if (item.DestroyedWhenLightExhausted)
                {
                    player.Inventory.RemoveItem(item);
                }
                continue;
            }

            if (rng.NextDouble() < item.ExtinguishChancePerTurn)
            {
                item.IsLit = false;
                messages.Add(RandomlyExtinguishedMessage(item));
            }
        }

        return messages;
    }

    /// <summary>
    /// Extinguishes every active, water-vulnerable physical light source the player carries --
    /// called once when the player's destination tile is Water (design spec section 10). Only
    /// ever finds something to extinguish the FIRST turn a lit item is in the water, since IsLit
    /// is already false on every later turn spent standing there -- naturally satisfying "don't
    /// repeatedly attempt to extinguish" with no extra tracking needed.
    /// </summary>
    public static List<string> ExtinguishForWater(Player player)
    {
        var messages = new List<string>();

        foreach (var item in player.Inventory.Items)
        {
            if (item.EmitsLight && item.IsLit && item.ExtinguishedByWater)
            {
                item.IsLit = false;
                messages.Add(WaterExtinguishedMessage(item));
            }
        }

        return messages;
    }

    private static string BurnedOutMessage(Item item) => item.Name switch
    {
        "Candle" => "Your candle flickers out.",
        "Torch" => "Your torch sputters and goes dark.",
        "Lantern" => "The light in your lantern dies.",
        _ => $"Your {item.DisplayName} goes dark."
    };

    private static string RandomlyExtinguishedMessage(Item item) => item.Name switch
    {
        "Candle" => "Your candle suddenly goes out.",
        "Torch" => "Your torch sputters and dies.",
        "Lantern" => "Your lantern flickers, then goes dark.",
        _ => $"Your {item.DisplayName} suddenly goes out."
    };

    private static string WaterExtinguishedMessage(Item item) => item.Name switch
    {
        "Candle" => "Your candle goes dark as you enter the water.",
        "Torch" => "The water extinguishes your torch.",
        "Lantern" => "Water floods your lantern and its light dies.",
        _ => $"The water extinguishes your {item.DisplayName}."
    };
}
