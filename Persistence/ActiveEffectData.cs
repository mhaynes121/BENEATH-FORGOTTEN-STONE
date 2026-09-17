using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Persistence;

/// <summary>
/// Serializable mirror of Entities.Spells.ActiveEffect -- that type has
/// constructor-only fields (SourceSpellName, ExpiresOnTurn) plus several
/// init-only ones, which round-trips awkwardly through System.Text.Json
/// directly. This is a plain get/set DTO instead, converted back via
/// `new ActiveEffect(SourceSpellName, ExpiresOnTurn) { ... }` on load --
/// see SaveManager.
/// </summary>
public class ActiveEffectData
{
    public string SourceSpellName { get; set; }
    public int ExpiresOnTurn { get; set; }
    public Stat? ModifiedStat { get; set; }
    public int StatAmount { get; set; }
    public int TickDamage { get; set; }
    public DamageType TickDamageType { get; set; }
    public string DamageSourceDescription { get; set; }

    /// <summary>Mirrors ActiveEffect.LightRadius -- unlike Owner (deliberately not persisted, see that property's own doc comment), this is a plain int that round-trips with no reference-resolution issue, so Arcane Orb/Divine Radiance's remaining duration survives a save/load (design spec section 22).</summary>
    public int? LightRadius { get; set; }

    /// <summary>Mirrors ActiveEffect.CanBePurified -- a plain bool, no reference-resolution issue.</summary>
    public bool CanBePurified { get; set; }
}
