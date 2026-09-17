namespace BENEATH_FORGOTTEN_STONE.Entities.Components;

/// <summary>
/// One stat's full breakdown: rolled base value plus every modifier
/// source, producing the adjusted value used in play. Sources stay
/// separate (rather than pre-summed) so chargen can display "Race: Human
/// 5-17 | Rolled: 14 | Class: Warrior +1" and so future systems
/// (equipment, buffs) have their own slot to add into without disturbing
/// the . There's no separate RaceModifier -- a race's influence is
/// already baked into BaseValue via the race-specific roll range (see
/// Race.StatRanges, CharacterStats.Roll), not a second additive term.
/// </summary>
public class StatBlock
{
    public int BaseValue { get; set; }
    public int ClassModifier { get; set; }
    public int EquipmentModifier { get; set; }
    public int OtherModifier { get; set; }

    /// <summary>
    /// BaseValue (the race-range roll) plus ClassModifier are clamped to
    /// the global 1-18 character-creation range first, matching the spec's
    /// worked examples (e.g. a Dwarf's max STR roll of 18 plus a Warrior's
    /// +1 clamps down to 18, not 19). Equipment and OtherModifier (a
    /// generic slot for any future non-race/class/equipment source, e.g. a
    /// spell/skill effect that raises or lowers a primary attribute) stack
    /// on top of that WITHOUT a further ceiling -- capping the fully-
    /// adjusted stat forever would flatten what gear or such an effect can
    /// achieve once a character neared 18. Never below 1 either way.
    /// Primary attributes never change on level-up -- see Player.AddExperience.
    /// </summary>
    public int Adjusted
    {
        get
        {
            int creationValue = Math.Clamp(BaseValue + ClassModifier, 1, 18);
            return Math.Max(1, creationValue + EquipmentModifier + OtherModifier);
        }
    }

    /// <summary>
    /// Same as Adjusted, but deliberately excludes OtherModifier -- the Ability Proficiency
    /// System's aptitude (governing-attribute learning rate) and lucky-insight (Luck) formulas
    /// both use this exact variant (spec sections 9/11: "clamp(BaseValue + ClassModifier, 1, 18)
    /// + EquipmentModifier... It excludes OtherModifier"), so a temporary buff/debuff (which
    /// lives in OtherModifier) never speeds up or slows down learning -- only rolled stats,
    /// class, and equipped gear do.
    /// </summary>
    public int AdjustedForAptitude
    {
        get
        {
            int creationValue = Math.Clamp(BaseValue + ClassModifier, 1, 18);
            return Math.Max(1, creationValue + EquipmentModifier);
        }
    }
}
