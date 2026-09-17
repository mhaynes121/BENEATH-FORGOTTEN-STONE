using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Entities.Components;

/// <summary>
/// One effect entry carried by an Item (a weapon's on-hit proc, or a piece
/// of armor's passive defensive effect) -- see Item.StatusEffects. Kept as
/// a plain data record rather than individual bool fields (HasFire, HasPoison,
/// ...) so one item can carry any number of these, and a new effect type
/// never needs a new Item property. Interpretation is ItemEffectApplier's
/// job, not this type's.
/// </summary>
public class ItemStatusEffect
{
    public ItemEffectType EffectType { get; }

    /// <summary>0.0-1.0 chance to trigger on a hit -- meaningless for passive defensive effects, which always apply while equipped.</summary>
    public double Chance { get; }

    /// <summary>Interpretation depends on EffectType -- damage per hit/tick for the elemental types, flat armor reduction for Corrode, flat heal for Regeneration, flat damage reduction for a *Resistance/Protection, flat reflected damage for Thorns. Unused for Stun.</summary>
    public int Magnitude { get; }

    /// <summary>0 means "instant" (the effect's full Magnitude applies immediately, once) rather than a multi-turn condition -- e.g. a Flaming Sword with Duration 0 just adds bonus fire damage to the hit, while Duration > 0 applies a ticking Burning condition instead. Meaningless for passive defensive effects.</summary>
    public int Duration { get; }

    public ItemStatusEffect(ItemEffectType effectType, double chance = 1.0, int magnitude = 0, int duration = 0)
    {
        EffectType = effectType;
        Chance = chance;
        Magnitude = magnitude;
        Duration = duration;
    }
}
