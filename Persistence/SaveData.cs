using BENEATH_FORGOTTEN_STONE.Entities;

namespace BENEATH_FORGOTTEN_STONE.Persistence;

public class SaveData
{
    public string PlayerName { get; set; }
    public string ClassName { get; set; }
    public string RaceName { get; set; }
    public int Level { get; set; }

    /// <summary>Total normal player turns taken this run -- see Player.TurnCount. Absent (defaults to 0) for saves written before this existed.</summary>
    public long TurnCount { get; set; }

    public long ExperienceCurrent { get; set; }
    public long ExperienceToNextLevel { get; set; }
    public long Gold { get; set; }

    /// <summary>Full per-attribute breakdown (base/race/class/equipment/other), not just the adjusted total, so nothing is lost on reload. Null for saves written before stat persistence existed.</summary>
    public Dictionary<PrimaryAttribute, StatBlockData> Stats { get; set; }

    /// <summary>Carried but unequipped items. Null for saves written before item persistence existed.</summary>
    public List<ItemData> InventoryItems { get; set; }

    /// <summary>All 12 equipment slots that have something in them. Null for saves written before item persistence existed.</summary>
    public Dictionary<EquipmentSlot, ItemData> Equipment { get; set; }

    public int FloorIndex { get; set; }
    public int PlayerX { get; set; }
    public int PlayerY { get; set; }
    public int PlayerHp { get; set; }
    public int PlayerMaxHp { get; set; }
    public int PlayerMana { get; set; }
    public int PlayerMaxMana { get; set; }

    /// <summary>Every floor visited this playthrough, not just the current one -- see LevelData. Null for saves written before floor persistence existed; SaveManager.ToLevels treats that the same as an empty list (every floor just regenerates fresh, the old behavior).</summary>
    public List<LevelData> Floors { get; set; }

    /// <summary>Cross-floor trader-frequency counter (see DungeonManager.LevelsSinceLastTrader). Null for saves written before the trader system existed -- treated as 0 on load, same as a fresh game.</summary>
    public int? LevelsSinceLastTrader { get; set; }

    /// <summary>Lifetime run statistics -- see Entities.AdventureRecord. Null for saves written before this system existed; SaveManager.ToPlayer treats that the same as a freshly-created character's all-zero AdventureRecord.</summary>
    public AdventureRecordData AdventureRecord { get; set; }

    /// <summary>
    /// The player's own known skills/spells/cooldowns/active effects -- previously a
    /// self-documented gap (monsters' equivalent state was already saved; the player's wasn't).
    /// Null for a save written before this existed; SaveManager.ToPlayer falls back to today's
    /// existing re-derivation (GrantSkillsForLevel/StartingSpellsFor) in that case, same as a
    /// freshly-created character missing nothing but scroll-learned spells.
    /// </summary>
    public List<string> KnownSkillNames { get; set; }
    public List<string> KnownSpellNames { get; set; }
    public Dictionary<string, int> SkillCooldownsByName { get; set; }
    public Dictionary<string, int> SpellCooldownsByName { get; set; }
    public List<ActiveEffectData> PlayerActiveEffects { get; set; }

    /// <summary>Ability Proficiency System -- keyed by Skill.ProficiencyId/Spell.ProficiencyId. Null (or a missing entry for a given id) means Novice, same as a never-yet-used ranked ability -- see Player.RankOf.</summary>
    public Dictionary<string, AbilityProficiencyData> Proficiencies { get; set; }

    /// <summary>Prone/Knockdown System -- see Actor.IsProne. Absent (defaults to false) for a save written before this existed, which is exactly correct: no character could ever have been prone before this feature existed.</summary>
    public bool PlayerIsProne { get; set; }

    /// <summary>New Priest Skill Progression (Intercession) -- see Player.IntercessionActive/IntercessionExpiresOnTurn. Both default to false/0 for a save written before this existed.</summary>
    public bool PlayerIntercessionActive { get; set; }
    public int PlayerIntercessionExpiresOnTurn { get; set; }

    /// <summary>
    /// Pet and Companion System -- see Player.Pet. Null for a save written before this existed
    /// (the chosen compatibility rule: a pet is granted only to a NEWLY created character, never
    /// retroactively to an old save on load) AND, going forward, is never expected to be null for
    /// any save written by a version of the game that has this feature, since every new character
    /// now starts with one.
    /// </summary>
    public PetData Pet { get; set; }
}
