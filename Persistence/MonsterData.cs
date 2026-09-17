using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities;
using BENEATH_FORGOTTEN_STONE.Entities.Sounds;
using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Persistence;

/// <summary>
/// Serializable snapshot of a living Monster's fully-resolved state --
/// deliberately NOT "archetype name + difficulty level to re-derive from,"
/// since that would let a future archetype rebalance silently corrupt an
/// old save. Only living monsters are ever saved -- Level.RemoveDeadActors
/// already drops dead ones from Level.Actors during play, so there's
/// nothing to special-case here. See Monster.RestoreData/Monster.Restore.
/// </summary>
public class MonsterData
{
    public string Name { get; set; }
    public char Symbol { get; set; }
    public ConsoleColor Color { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public string ShortDescription { get; set; }
    public string LongDescription { get; set; }
    public int Level { get; set; }
    public int Agility { get; set; }
    public double DifficultyRating { get; set; }
    public long? XpRewardOverride { get; set; }
    public Size Size { get; set; }
    public AttackType AttackType { get; set; }
    public CreatureType CreatureType { get; set; }
    public bool CanCarryItems { get; set; }
    public bool CanEquipItems { get; set; }
    public bool CanUseItems { get; set; }

    /// <summary>Tile-attuned monster system -- see Monster.PreferredFloorType/ElementalAffinity/OffPreferredFloorDamagePercent. OpposingElement is deliberately not persisted: it's always derived from ElementalAffinity via ElementalOpposition.</summary>
    public FloorType? PreferredFloorType { get; set; }
    public DamageType? ElementalAffinity { get; set; }
    public double OffPreferredFloorDamagePercent { get; set; } = FloorAttunementConfig.DefaultOffPreferredFloorDamagePercent;

    /// <summary>Resistance System spec section 38 -- null (an old save written before this existed) is treated as ResistanceSet.Zero on restore, never a null-reference.</summary>
    public ResistanceSet BaseResistances { get; set; } = ResistanceSet.Zero;

    /// <summary>Ambient Sound / Hearing System spec -- defaults to None, so an old save written before this existed restores as naturally silent.</summary>
    public MonsterSoundType SoundType { get; set; } = MonsterSoundType.None;
    public int BasePhysicalAttackPower { get; set; }
    public int BaseMagicalAttackPower { get; set; }
    public int DefensePower { get; set; }
    public int MagicResistance { get; set; }
    public int Speed { get; set; }
    public int Energy { get; set; }
    public int CurrentHp { get; set; }
    public int MaxHp { get; set; }
    public int CurrentMana { get; set; }
    public int MaxMana { get; set; }
    public bool IsAlerted { get; set; }
    public bool IsBoss { get; set; }
    public string BossName { get; set; }
    public string BossEpithet { get; set; }
    public EpithetFormat BossEpithetFormat { get; set; }
    public int StunnedUntilTurn { get; set; }
    public int SilencedUntilTurn { get; set; }
    public int CcImmuneUntilTurn { get; set; }

    /// <summary>Prone/Knockdown System -- see Actor.IsProne. Absent (defaults to false) for a save written before this existed.</summary>
    public bool IsProne { get; set; }

    /// <summary>New Priest Skill Progression (Turn Undead) -- see Actor.FrightenedUntilTurn. Absent (defaults to 0) for a save written before this existed.</summary>
    public int FrightenedUntilTurn { get; set; }
    public List<string> KnownSpellNames { get; set; } = new();
    public Dictionary<string, int> SpellCooldownsByName { get; set; } = new();
    public List<ActiveEffectData> ActiveEffects { get; set; } = new();

    /// <summary>Items this monster spawned holding (or hasn't used yet) -- see LootGenerator.GenerateSpawnItems and ChaseAI's healing-item check. Resolved by name against Items.All, same as the player's own inventory (ItemData).</summary>
    public List<ItemData> Inventory { get; set; } = new();

    /// <summary>What's equipped, if anything -- see Monster.Equipment/ApplyEquipmentBonus. BasePhysicalAttackPower/BaseMagicalAttackPower/DefensePower above already include the bonus this was granting; this is restored separately so AttackType's equipped-weapon check still works after a reload.</summary>
    public Dictionary<EquipmentSlot, ItemData> Equipment { get; set; } = new();
}
