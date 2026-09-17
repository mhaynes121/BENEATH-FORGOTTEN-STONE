using System.Text.Json;
using System.Text.RegularExpressions;
using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities;
using BENEATH_FORGOTTEN_STONE.Entities.Components;
using BENEATH_FORGOTTEN_STONE.Entities.Proficiency;
using BENEATH_FORGOTTEN_STONE.Entities.Skills;
using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Persistence;

/// <summary>
/// Save-on-quit / delete-on-death persistence.
///
/// Every floor the player has ever visited is persisted (see LevelData,
/// SaveData.Floors), not just the current one -- ToLevels reconstructs
/// them all so DungeonManager.RestoreFloor can seed them back in before
/// the resume floor is entered, and backtracking after a reload shows an
/// earlier floor exactly as left rather than regenerated.
///
/// Inventory and all 12 equipment slots are persisted by resolving each
/// item back to its shared Items.cs catalog template by name (see
/// ResolveItem) -- a charge-limited item (e.g. a wand) gets its own
/// cloned instance with its saved Charges count, matching how the
/// dungeon generator already clones charged items at spawn time. A
/// StatBlock's EquipmentModifier is NOT restored directly from saved
/// data -- it's derived from whatever's currently equipped, so it gets
/// rebuilt correctly as a side effect of re-equipping each saved item
/// (restoring the raw number too would double it). The same name-
/// resolution pattern is reused for monster known spells/cooldowns
/// (against SpellCatalog.All) and chest/ground-item contents.
///
/// The player's own known skills/spells, cooldowns, active buffs/DoTs, and Ability Proficiency
/// System ranks ARE persisted too (see RestorePlayerAbilityState) -- previously a self-documented
/// gap (a reload came back with a fresh spellbook and no active effects) fixed alongside the
/// Ability Proficiency System, since proficiency data needs to stay in sync with what the player
/// actually knows. Mirrors the same name-resolution pattern already used for monsters below.
///
/// CharacterStats (STR/CON/AGI/WIS/KNO, full base/race/class/equipment/
/// other breakdown) and Gold are both persisted -- a reload restores the
/// exact stat block kept at character creation, not a fresh roll.
/// </summary>
public static class SaveManager
{
    private const string SaveFileName = "savegame.json";

    public static void Save(DungeonManager dungeonManager, Player player)
    {
        var data = new SaveData
        {
            PlayerName = player.Name,
            ClassName = player.Class.Name,
            RaceName = player.Race.Name,
            Level = player.Level,
            TurnCount = player.TurnCount,
            ExperienceCurrent = player.Experience.Current,
            ExperienceToNextLevel = player.Experience.ToNextLevel,
            Gold = player.Gold,
            Stats = SaveStats(player.Stats),
            InventoryItems = player.Inventory.Items.Select(ToItemData).ToList(),
            Equipment = player.Equipment.AllEquipped.ToDictionary(kvp => kvp.Key, kvp => ToItemData(kvp.Value)),
            FloorIndex = dungeonManager.CurrentFloorIndex,
            PlayerX = player.X,
            PlayerY = player.Y,
            PlayerHp = player.Health.Current,
            PlayerMaxHp = player.Health.Max,
            PlayerMana = player.Mana.Current,
            PlayerMaxMana = player.Mana.Max,
            Floors = dungeonManager.Floors.Select(kvp => ToLevelData(kvp.Key, kvp.Value)).ToList(),
            LevelsSinceLastTrader = dungeonManager.LevelsSinceLastTrader,
            AdventureRecord = ToAdventureRecordData(player.AdventureRecord),
            KnownSkillNames = player.KnownSkills.Select(s => s.Name).ToList(),
            KnownSpellNames = player.KnownSpells.Select(s => s.Name).ToList(),
            SkillCooldownsByName = player.SkillCooldowns.ToDictionary(kvp => kvp.Key.Name, kvp => kvp.Value),
            SpellCooldownsByName = player.SpellCooldowns.ToDictionary(kvp => kvp.Key.Name, kvp => kvp.Value),
            PlayerActiveEffects = player.ActiveEffects.Select(ToActiveEffectData).ToList(),
            Proficiencies = player.Proficiencies.ToDictionary(kvp => kvp.Key, kvp => ToAbilityProficiencyData(kvp.Value)),
            PlayerIsProne = player.IsProne,
            PlayerIntercessionActive = player.IntercessionActive,
            PlayerIntercessionExpiresOnTurn = player.IntercessionExpiresOnTurn,
            Pet = player.Pet != null ? ToPetData(player.Pet) : null
        };

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SaveFileName, json);
    }

    public static bool SaveExists() => File.Exists(SaveFileName);

    public static SaveData Load()
    {
        if (!SaveExists())
        {
            return null;
        }

        var json = File.ReadAllText(SaveFileName);
        return JsonSerializer.Deserialize<SaveData>(json);
    }

    /// <summary>Rebuilds a Player from saved data. Position/floor layout are
    /// not restored -- see the type-level note above.</summary>
    public static Player ToPlayer(SaveData data)
    {
        var characterClass = CharacterClass.All.FirstOrDefault(c => c.Name == data.ClassName) ?? CharacterClass.Warrior;
        var race = Race.All.FirstOrDefault(r => r.Name == data.RaceName) ?? Race.Human;

        // data.Stats is null for saves written before stat persistence existed -- fall back to a fresh roll rather than crashing.
        var stats = data.Stats != null
            ? LoadStats(data.Stats, race)
            : CharacterStats.Roll(race, characterClass, new Random());

        var player = new Player(data.PlayerName, characterClass, race, stats)
        {
            Level = data.Level,
            TurnCount = data.TurnCount
        };

        player.X = data.PlayerX;
        player.Y = data.PlayerY;
        player.Health.SetCurrent(data.PlayerHp);
        player.Mana.SetCurrent(data.PlayerMana);
        player.Experience.Current = data.ExperienceCurrent;
        player.Experience.ToNextLevel = data.ExperienceToNextLevel;
        player.RestoreGold(data.Gold);

        // Null for a save written before this system existed -- player.AdventureRecord already
        // defaults to a fresh, all-zero instance via its own field initializer, so there's
        // nothing to do.
        if (data.AdventureRecord != null)
        {
            RestoreAdventureRecord(player.AdventureRecord, data.AdventureRecord);
        }

        if (data.InventoryItems != null)
        {
            foreach (var itemData in data.InventoryItems)
            {
                var item = ResolveItem(itemData);
                if (item != null)
                {
                    player.Inventory.AddItem(item);
                }
            }
        }

        if (data.Equipment != null)
        {
            foreach (var (slot, itemData) in data.Equipment)
            {
                var item = ResolveItem(itemData);
                if (item != null)
                {
                    player.EquipInSlot(slot, item);
                }
            }
        }

        RestorePlayerAbilityState(player, data);

        player.IsProne = data.PlayerIsProne;
        player.IntercessionActive = data.PlayerIntercessionActive;
        player.IntercessionExpiresOnTurn = data.PlayerIntercessionExpiresOnTurn;

        // Pet and Companion System: null means a save written before this existed -- the chosen
        // compatibility rule is "new characters only," so an old save simply loads with no pet,
        // same as it always has.
        if (data.Pet != null)
        {
            FromPetData(data.Pet, player);
        }

        return player;
    }

    /// <summary>Internal (not private) so Diagnostics/SelfTest.cs can round-trip a Pet directly.</summary>
    internal static PetData ToPetData(Pet pet) => new()
    {
        DefinitionId = pet.DefinitionId,
        X = pet.X,
        Y = pet.Y,
        Level = pet.Level,
        CurrentHp = pet.Health.Current,
        Energy = pet.Energy,
        LifecycleState = pet.LifecycleState,
        RespawnAtOwnerTurn = pet.RespawnAtOwnerTurn,
        ActiveEffects = pet.ActiveEffects.Select(ToActiveEffectData).ToList()
    };

    /// <summary>Internal (not private) -- see ToPetData. Reuses PetFactory.CreateDog for presentation/AI/owner-wiring (sets player.Pet as a side effect), then overlays the actually-saved state on top.</summary>
    internal static Pet FromPetData(PetData data, Player player)
    {
        var pet = PetFactory.CreateDog(player);
        pet.MoveTo(data.X, data.Y);
        PetProgression.SyncToLevel(pet, data.Level);
        pet.Health.SetCurrent(data.CurrentHp);
        pet.Energy = data.Energy;
        pet.LifecycleState = data.LifecycleState;
        pet.RespawnAtOwnerTurn = data.RespawnAtOwnerTurn;

        foreach (var effectData in data.ActiveEffects)
        {
            pet.ActiveEffects.Add(FromActiveEffectData(effectData));
        }

        return pet;
    }

    /// <summary>
    /// Fixes the previously self-documented gap where the player's own known skills/spells/
    /// cooldowns/active effects were never persisted at all (monsters' equivalent state already
    /// was -- see ToMonsterData/FromMonsterData, the pattern this mirrors). data.KnownSkillNames
    /// null means a save written before this existed: skills fall back to re-deriving every
    /// level's deterministic grant (GrantSkillsForLevel already ran once for level 1 inside the
    /// Player constructor above, before data.Level was even assigned, so this picks up from
    /// level 2); a scroll-learned spell beyond the single random starting one the constructor
    /// already granted is simply unrecoverable, same as today's existing behavor.
    /// </summary>
    private static void RestorePlayerAbilityState(Player player, SaveData data)
    {
        if (data.KnownSkillNames != null)
        {
            player.KnownSkills.Clear();
            foreach (var name in data.KnownSkillNames)
            {
                var skill = SkillCatalog.All.FirstOrDefault(s => s.Name == name);
                if (skill != null)
                {
                    player.KnownSkills.Add(skill);
                }
            }
        }
        else
        {
            for (int level = 2; level <= player.Level; level++)
            {
                player.GrantSkillsForLevel(level);
            }
        }

        if (data.KnownSpellNames != null)
        {
            player.KnownSpells.Clear();
            foreach (var name in data.KnownSpellNames)
            {
                var spell = SpellCatalog.All.FirstOrDefault(s => s.Name == name);
                if (spell != null)
                {
                    player.KnownSpells.Add(spell);
                }
            }
        }

        if (data.SkillCooldownsByName != null)
        {
            foreach (var (name, readyTurn) in data.SkillCooldownsByName)
            {
                var skill = SkillCatalog.All.FirstOrDefault(s => s.Name == name);
                if (skill != null)
                {
                    player.SkillCooldowns[skill] = readyTurn;
                }
            }
        }

        if (data.SpellCooldownsByName != null)
        {
            foreach (var (name, readyTurn) in data.SpellCooldownsByName)
            {
                var spell = SpellCatalog.All.FirstOrDefault(s => s.Name == name);
                if (spell != null)
                {
                    player.SpellCooldowns[spell] = readyTurn;
                }
            }
        }

        if (data.PlayerActiveEffects != null)
        {
            foreach (var effectData in data.PlayerActiveEffects)
            {
                player.ActiveEffects.Add(FromActiveEffectData(effectData));
            }
        }

        if (data.Proficiencies != null)
        {
            foreach (var (id, proficiencyData) in data.Proficiencies)
            {
                player.Proficiencies[id] = FromAbilityProficiencyData(proficiencyData);
            }
        }
    }

    /// <summary>Reconstructs every saved floor, for seeding DungeonManager via RestoreFloor before entering the resume floor. Null Floors (a save written before floor persistence existed) yields an empty dictionary, matching the old behavior of regenerating every floor fresh.</summary>
    public static Dictionary<int, Level> ToLevels(SaveData data)
    {
        var levels = new Dictionary<int, Level>();
        if (data.Floors == null)
        {
            return levels;
        }

        foreach (var levelData in data.Floors)
        {
            levels[levelData.FloorIndex] = FromLevelData(levelData);
        }

        return levels;
    }

    /// <summary>Internal (not private) so Diagnostics/SelfTest.cs can round-trip a Level's GroundItems (concealment state included) without touching the real save file on disk.</summary>
    internal static LevelData ToLevelData(int floorIndex, Level level)
    {
        var tileTypes = new TileType[level.Width * level.Height];
        var tileExplored = new bool[level.Width * level.Height];
        var tileFloorTypes = new FloorType[level.Width * level.Height];
        var tileIsDarkRoom = new bool[level.Width * level.Height];
        for (int x = 0; x < level.Width; x++)
        {
            for (int y = 0; y < level.Height; y++)
            {
                int i = x * level.Height + y;
                tileTypes[i] = level.Tiles[x, y].Type;
                tileExplored[i] = level.Tiles[x, y].IsExplored;
                tileFloorTypes[i] = level.Tiles[x, y].FloorType;
                tileIsDarkRoom[i] = level.Tiles[x, y].IsDarkRoom;
            }
        }

        return new LevelData
        {
            FloorIndex = floorIndex,
            Width = level.Width,
            Height = level.Height,
            TurnNumber = level.TurnNumber,
            StairsUpX = level.StairsUpPosition.X,
            StairsUpY = level.StairsUpPosition.Y,
            StairsDownX = level.StairsDownPosition.X,
            StairsDownY = level.StairsDownPosition.Y,
            TileTypes = tileTypes,
            TileExplored = tileExplored,
            TileFloorTypes = tileFloorTypes,
            TileIsDarkRoom = tileIsDarkRoom,
            // Only living monsters are ever in Level.Actors -- RemoveDeadActors already
            // drops dead ones during play, so there's nothing to filter here.
            Monsters = level.Actors.OfType<Monster>().Select(ToMonsterData).ToList(),
            Traders = level.Actors.OfType<Trader>().Select(ToTraderData).ToList(),
            GroundItems = level.GroundItems.Select(drop => new GroundItemData
            {
                X = drop.X,
                Y = drop.Y,
                Item = ToItemData(drop.Item),
                IsConcealed = drop.IsConcealed,
                ConcealmentDifficulty = drop.ConcealmentDifficulty,
                LandingOrigin = drop.LandingOrigin,
                CountsAsMisplaced = drop.CountsAsMisplaced,
                HasBeenRecovered = drop.HasBeenRecovered
            }).ToList(),
            Chests = level.Chests.Select(ToChestData).ToList(),
            Corpses = level.Corpses.Select(ToCorpseData).ToList(),
            Traps = level.Traps.Select(ToTrapData).ToList(),
            Doors = level.Doors.Select(ToDoorData).ToList(),
            RoomObjects = level.RoomObjects.Select(ToRoomObjectData).ToList(),
            AmbientSoundSources = level.AmbientSoundSources
                .Select(source => new AmbientSoundSourceData { X = source.X, Y = source.Y, FloorType = source.FloorType })
                .ToList()
        };
    }

    /// <summary>Internal (not private) -- see ToLevelData.</summary>
    internal static Level FromLevelData(LevelData data)
    {
        var level = new Level(data.FloorIndex, data.Width, data.Height)
        {
            StairsUpPosition = (data.StairsUpX, data.StairsUpY),
            StairsDownPosition = (data.StairsDownX, data.StairsDownY)
        };
        level.RestoreTurnNumber(data.TurnNumber);

        for (int x = 0; x < data.Width; x++)
        {
            for (int y = 0; y < data.Height; y++)
            {
                int i = x * data.Height + y;
                var tile = Tile.Create(data.TileTypes[i]);
                tile.IsExplored = data.TileExplored[i];
                // Null for a save written before the floor-type system existed -- every tile
                // already defaults to FloorType.Normal via Tile.Create, so there's nothing to do.
                if (data.TileFloorTypes != null)
                {
                    tile.FloorType = data.TileFloorTypes[i];
                }
                // Null for a save written before dark rooms existed -- every tile already
                // defaults to IsDarkRoom = false via Tile's own default, same fallback shape as
                // TileFloorTypes' own null check above.
                if (data.TileIsDarkRoom != null)
                {
                    tile.IsDarkRoom = data.TileIsDarkRoom[i];
                }
                level.Tiles[x, y] = tile;
            }
        }

        foreach (var monsterData in data.Monsters)
        {
            var monster = FromMonsterData(monsterData);
            level.Actors.Add(monster);
            level.Scheduler.Register(monster);
        }

        foreach (var traderData in data.Traders)
        {
            // Deliberately never Scheduler.Register'd -- see Trader/NPC's own doc comments.
            level.Actors.Add(FromTraderData(traderData));
        }

        foreach (var groundItem in data.GroundItems)
        {
            var item = ResolveItem(groundItem.Item);
            if (item == null)
            {
                continue;
            }

            // Restoring a save replays state that already survived (or already didn't) the first
            // time -- never rerolls concealment, never triggers a discovery check (see
            // LoadingGroundItemsNeverRerollsLoss and ItemDestructionRules' own doc comment on why
            // loading isn't a "landing" event).
            if (groundItem.IsConcealed)
            {
                var restored = level.AddConcealedItem(groundItem.X, groundItem.Y, item, groundItem.ConcealmentDifficulty, groundItem.LandingOrigin);
                restored.CountsAsMisplaced = groundItem.CountsAsMisplaced;
                restored.HasBeenRecovered = groundItem.HasBeenRecovered;
            }
            else
            {
                var restored = level.AddVisibleItem(groundItem.X, groundItem.Y, item, groundItem.LandingOrigin);
                restored.CountsAsMisplaced = groundItem.CountsAsMisplaced;
                restored.HasBeenRecovered = groundItem.HasBeenRecovered;
            }
        }

        foreach (var chestData in data.Chests)
        {
            level.Chests.Add(FromChestData(chestData));
        }

        // Null/empty for a save written before this feature existed -- the loop below simply
        // doesn't run, leaving the level with no corpses at all, same fallback shape as
        // RoomObjects/AmbientSoundSources below.
        foreach (var corpseData in data.Corpses ?? new List<CorpseData>())
        {
            level.Corpses.Add(FromCorpseData(corpseData));
        }

        foreach (var trapData in data.Traps)
        {
            level.Traps.Add(FromTrapData(trapData));
        }

        foreach (var doorData in data.Doors)
        {
            level.Doors.Add(FromDoorData(doorData));
        }

        // Null/empty for a save written before this system existed -- the loop below simply
        // doesn't run, leaving the level with no room objects at all (matches the default any
        // freshly-generated pre-feature floor would already have had).
        foreach (var roomObjectData in data.RoomObjects ?? new List<RoomObjectData>())
        {
            level.RoomObjects.Add(FromRoomObjectData(roomObjectData));
        }

        // Null/empty for a save written before this system existed -- the loop below simply
        // doesn't run, leaving the level with no environmental sound sources (a quieter dungeon
        // rather than an error), same fallback shape as TileFloorTypes' own null check above.
        foreach (var soundSourceData in data.AmbientSoundSources ?? new List<AmbientSoundSourceData>())
        {
            level.AmbientSoundSources.Add(new AmbientSoundSource(soundSourceData.X, soundSourceData.Y, soundSourceData.FloorType));
        }

        level.RecomputeReachability();

        // Retroactive cleanup for a save written before Tile.IsReachable existed (or one written
        // while the "orphaned wall" artifact was still live): a tile's remembered IsExplored can
        // already be wrongly true from an earlier buggy turn, which RecomputeReachability alone
        // can't fix since it only gates FUTURE visibility, not past persisted memory. Forgetting
        // an unreachable tile here means a sealed-off pocket stops rendering as a remembered wall
        // the moment this floor is next loaded, with no other migration needed.
        for (int x = 0; x < level.Width; x++)
        {
            for (int y = 0; y < level.Height; y++)
            {
                if (!level.Tiles[x, y].IsReachable)
                {
                    level.Tiles[x, y].IsExplored = false;
                }
            }
        }

        return level;
    }

    private static MonsterData ToMonsterData(Monster monster) => new()
    {
        Name = monster.Name,
        Symbol = monster.Symbol,
        Color = monster.Color,
        X = monster.X,
        Y = monster.Y,
        ShortDescription = monster.ShortDescription,
        LongDescription = monster.LongDescription,
        Level = monster.Level,
        Agility = monster.Agility,
        DifficultyRating = monster.DifficultyRating,
        XpRewardOverride = monster.XpRewardOverride,
        Size = monster.Size,
        AttackType = monster.AttackType,
        CreatureType = monster.CreatureType,
        CanCarryItems = monster.CanCarryItems,
        CanEquipItems = monster.CanEquipItems,
        CanUseItems = monster.CanUseItems,
        PreferredFloorType = monster.PreferredFloorType,
        ElementalAffinity = monster.ElementalAffinity,
        OffPreferredFloorDamagePercent = monster.OffPreferredFloorDamagePercent,
        BaseResistances = monster.BaseResistances,
        SoundType = monster.SoundType,
        BasePhysicalAttackPower = monster.BasePhysicalAttackPower,
        BaseMagicalAttackPower = monster.BaseMagicalAttackPower,
        DefensePower = monster.DefensePower,
        MagicResistance = monster.MagicResistance,
        Speed = monster.Speed,
        Energy = monster.Energy,
        CurrentHp = monster.Health.Current,
        MaxHp = monster.Health.Max,
        CurrentMana = monster.Mana.Current,
        MaxMana = monster.Mana.Max,
        IsAlerted = monster.IsAlerted,
        IsBoss = monster.IsBoss,
        BossName = monster.BossName,
        BossEpithet = monster.BossEpithet,
        BossEpithetFormat = monster.BossEpithetFormat,
        StunnedUntilTurn = monster.StunnedUntilTurn,
        SilencedUntilTurn = monster.SilencedUntilTurn,
        CcImmuneUntilTurn = monster.CcImmuneUntilTurn,
        IsProne = monster.IsProne,
        FrightenedUntilTurn = monster.FrightenedUntilTurn,
        KnownSpellNames = monster.KnownSpells.Select(s => s.Name).ToList(),
        SpellCooldownsByName = monster.SpellCooldowns.ToDictionary(kvp => kvp.Key.Name, kvp => kvp.Value),
        ActiveEffects = monster.ActiveEffects.Select(ToActiveEffectData).ToList(),
        Inventory = monster.Inventory.Items.Select(ToItemData).ToList(),
        Equipment = monster.Equipment.AllEquipped.ToDictionary(kvp => kvp.Key, kvp => ToItemData(kvp.Value))
    };

    private static Monster FromMonsterData(MonsterData data)
    {
        var restoreData = new Monster.RestoreData
        {
            Name = data.Name,
            Symbol = data.Symbol,
            Color = data.Color,
            X = data.X,
            Y = data.Y,
            ShortDescription = data.ShortDescription,
            LongDescription = data.LongDescription,
            Level = data.Level,
            Agility = data.Agility,
            DifficultyRating = data.DifficultyRating,
            XpRewardOverride = data.XpRewardOverride,
            Size = data.Size,
            AttackType = data.AttackType,
            CreatureType = data.CreatureType,
            CanCarryItems = data.CanCarryItems,
            CanEquipItems = data.CanEquipItems,
            CanUseItems = data.CanUseItems,
            PreferredFloorType = data.PreferredFloorType,
            ElementalAffinity = data.ElementalAffinity,
            OffPreferredFloorDamagePercent = data.OffPreferredFloorDamagePercent,
            BaseResistances = data.BaseResistances,
            SoundType = data.SoundType,
            BasePhysicalAttackPower = data.BasePhysicalAttackPower,
            BaseMagicalAttackPower = data.BaseMagicalAttackPower,
            DefensePower = data.DefensePower,
            MagicResistance = data.MagicResistance,
            Speed = data.Speed,
            Energy = data.Energy,
            CurrentHp = data.CurrentHp,
            MaxHp = data.MaxHp,
            CurrentMana = data.CurrentMana,
            MaxMana = data.MaxMana,
            IsAlerted = data.IsAlerted,
            IsBoss = data.IsBoss,
            BossName = data.BossName,
            BossEpithet = data.BossEpithet,
            BossEpithetFormat = data.BossEpithetFormat,
            StunnedUntilTurn = data.StunnedUntilTurn,
            SilencedUntilTurn = data.SilencedUntilTurn,
            CcImmuneUntilTurn = data.CcImmuneUntilTurn,
            IsProne = data.IsProne,
            FrightenedUntilTurn = data.FrightenedUntilTurn,
            ActiveEffects = data.ActiveEffects.Select(FromActiveEffectData).ToList()
        };

        foreach (var name in data.KnownSpellNames)
        {
            var spell = SpellCatalog.All.FirstOrDefault(s => s.Name == name);
            if (spell != null)
            {
                restoreData.KnownSpells.Add(spell);
            }
        }

        foreach (var (name, readyTurn) in data.SpellCooldownsByName)
        {
            var spell = SpellCatalog.All.FirstOrDefault(s => s.Name == name);
            if (spell != null)
            {
                restoreData.SpellCooldowns[spell] = readyTurn;
            }
        }

        foreach (var (slot, itemData) in data.Equipment)
        {
            var item = ResolveItem(itemData);
            if (item != null)
            {
                restoreData.Equipment[slot] = item;
            }
        }

        var monster = Monster.Restore(restoreData);

        foreach (var itemData in data.Inventory)
        {
            var item = ResolveItem(itemData);
            if (item != null)
            {
                monster.Inventory.AddItem(item);
            }
        }

        return monster;
    }

    private static TraderData ToTraderData(Trader trader) => new()
    {
        Name = trader.Name,
        Symbol = trader.Symbol,
        Color = trader.Color,
        X = trader.X,
        Y = trader.Y,
        ShortDescription = trader.ShortDescription,
        LongDescription = trader.LongDescription,
        Gold = trader.Gold,
        Inventory = trader.Inventory.Items.Select(ToItemData).ToList()
    };

    private static Trader FromTraderData(TraderData data)
    {
        var restoreData = new Trader.RestoreData
        {
            Name = data.Name,
            Symbol = data.Symbol,
            Color = data.Color,
            X = data.X,
            Y = data.Y,
            ShortDescription = data.ShortDescription,
            LongDescription = data.LongDescription,
            Gold = data.Gold
        };

        foreach (var itemData in data.Inventory)
        {
            var item = ResolveItem(itemData);
            if (item != null)
            {
                restoreData.Inventory.Add(item);
            }
        }

        return Trader.Restore(restoreData);
    }

    private static ActiveEffectData ToActiveEffectData(ActiveEffect effect) => new()
    {
        SourceSpellName = effect.SourceSpellName,
        ExpiresOnTurn = effect.ExpiresOnTurn,
        ModifiedStat = effect.ModifiedStat,
        StatAmount = effect.StatAmount,
        TickDamage = effect.TickDamage,
        TickDamageType = effect.TickDamageType,
        DamageSourceDescription = effect.DamageSourceDescription,
        LightRadius = effect.LightRadius,
        CanBePurified = effect.CanBePurified
    };

    private static ActiveEffect FromActiveEffectData(ActiveEffectData data) => new(data.SourceSpellName, data.ExpiresOnTurn)
    {
        ModifiedStat = data.ModifiedStat,
        StatAmount = data.StatAmount,
        TickDamage = data.TickDamage,
        TickDamageType = data.TickDamageType,
        DamageSourceDescription = data.DamageSourceDescription,
        LightRadius = data.LightRadius,
        CanBePurified = data.CanBePurified
    };

    private static AbilityProficiencyData ToAbilityProficiencyData(AbilityProficiency proficiency) => new()
    {
        Proficiency = proficiency.Proficiency,
        ValidUses = proficiency.ValidUses,
        SuccessfulUses = proficiency.SuccessfulUses,
        FailedUses = proficiency.FailedUses,
        LuckyInsights = proficiency.LuckyInsights
    };

    private static AbilityProficiency FromAbilityProficiencyData(AbilityProficiencyData data) => new()
    {
        Proficiency = data.Proficiency,
        ValidUses = data.ValidUses,
        SuccessfulUses = data.SuccessfulUses,
        FailedUses = data.FailedUses,
        LuckyInsights = data.LuckyInsights
    };

    private static ChestData ToChestData(Chest chest) => new()
    {
        X = chest.X,
        Y = chest.Y,
        IsLocked = chest.IsLocked,
        Difficulty = chest.Difficulty,
        Container = ToContainerData(chest.Container)
    };

    /// <summary>Internal (not private) so Diagnostics/SelfTest.cs can verify old-save (pre-Persistent-Containers) fallback behavior directly.</summary>
    internal static Chest FromChestData(ChestData data)
    {
        // Container is null only for a pre-Persistent-Containers save -- fall back to its
        // deprecated top-level Contents/IsOpen fields so an already-opened chest with leftover
        // contents becomes accessible again instead of staying permanently unreachable.
        var contentsData = data.Container?.Contents ?? data.Contents;
        var contents = contentsData.Select(ResolveItem).Where(item => item != null).ToList();
        var chest = new Chest(data.X, data.Y, data.IsLocked, data.Difficulty, contents)
        {
            IsOpen = data.Container?.IsOpen ?? data.IsOpen
        };
        return chest;
    }

    private static TrapData ToTrapData(Trap trap) => new()
    {
        X = trap.X,
        Y = trap.Y,
        Damage = trap.Damage,
        IsRevealed = trap.IsRevealed,
        IsTriggered = trap.IsTriggered
    };

    private static Trap FromTrapData(TrapData data) => new(data.X, data.Y, data.Damage)
    {
        IsRevealed = data.IsRevealed,
        IsTriggered = data.IsTriggered
    };

    private static DoorData ToDoorData(Door door) => new()
    {
        X = door.X,
        Y = door.Y,
        IsLocked = door.IsLocked,
        IsPickable = door.IsPickable,
        IsBashable = door.IsBashable,
        Difficulty = door.Difficulty,
        MaxHealth = door.MaxHealth,
        Health = door.Health,
        IsOpen = door.IsOpen
    };

    private static Door FromDoorData(DoorData data) => new(data.X, data.Y, data.IsLocked, data.IsPickable, data.IsBashable, data.Difficulty, data.MaxHealth)
    {
        Health = data.Health,
        IsOpen = data.IsOpen
    };

    // Bumped to internal (from private, matching every other file's testability convention)
    // so SelfTest can verify the round-trip directly without going through a full Save()/Load()
    // disk round-trip.
    internal static RoomObjectData ToRoomObjectData(RoomObject roomObject) => new()
    {
        Type = roomObject.Type,
        X = roomObject.X,
        Y = roomObject.Y,
        IsMovable = roomObject.IsMovable,
        Trigger = roomObject.Trigger == null ? null : ToRoomObjectTriggerData(roomObject.Trigger)
    };

    internal static RoomObject FromRoomObjectData(RoomObjectData data)
    {
        var roomObject = RoomObjectCatalog.Create(data.Type, data.X, data.Y, data.IsMovable);
        if (data.Trigger != null)
        {
            roomObject.Trigger = FromRoomObjectTriggerData(data.Trigger);
        }
        return roomObject;
    }

    private static RoomObjectTriggerData ToRoomObjectTriggerData(RoomObjectTrigger trigger) => new()
    {
        Condition = trigger.Condition,
        Effect = trigger.Effect,
        Repeatable = trigger.Repeatable,
        HasActivated = trigger.HasActivated,
        Message = trigger.Message,
        TrackedX = trigger.TrackedPosition?.X,
        TrackedY = trigger.TrackedPosition?.Y,
        HiddenDoorX = trigger.HiddenDoorPosition?.X,
        HiddenDoorY = trigger.HiddenDoorPosition?.Y,
        SpawnPositionsX = trigger.SpawnPositions.Select(p => p.X).ToList(),
        SpawnPositionsY = trigger.SpawnPositions.Select(p => p.Y).ToList(),
        SpawnDifficultyLevel = trigger.SpawnDifficultyLevel
    };

    private static RoomObjectTrigger FromRoomObjectTriggerData(RoomObjectTriggerData data)
    {
        (int X, int Y)? trackedPosition = data.TrackedX.HasValue && data.TrackedY.HasValue ? (data.TrackedX.Value, data.TrackedY.Value) : null;
        (int X, int Y)? hiddenDoorPosition = data.HiddenDoorX.HasValue && data.HiddenDoorY.HasValue ? (data.HiddenDoorX.Value, data.HiddenDoorY.Value) : null;
        var spawnPositions = data.SpawnPositionsX.Zip(data.SpawnPositionsY, (x, y) => (x, y)).ToList();

        return new RoomObjectTrigger(
            data.Condition, data.Effect, data.Repeatable, trackedPosition, data.Message, hiddenDoorPosition, spawnPositions, data.SpawnDifficultyLevel)
        {
            HasActivated = data.HasActivated
        };
    }

    // Bumped to internal (from private, matching the RoomObjectData conversion methods' own
    // testability convention) so SelfTest can verify the round-trip directly.
    internal static AdventureRecordData ToAdventureRecordData(AdventureRecord record) => new()
    {
        MonstersKilled = record.MonstersKilled,
        DamageDealt = record.DamageDealt,
        DamageTaken = record.DamageTaken,
        GoldCollected = record.GoldCollected,
        GoldSpent = record.GoldSpent,
        BossesKilled = record.BossesKilled,
        HighestLevelBossName = record.HighestLevelBossName,
        HighestLevelBossLevel = record.HighestLevelBossLevel,
        DeepestFloorReached = record.DeepestFloorReached,
        TimeInDungeonSeconds = record.TimeInDungeonSeconds,
        ItemsPermanentlyLost = record.ItemsPermanentlyLost,
        ItemsMisplaced = record.ItemsMisplaced,
        MisplacedItemsRecovered = record.MisplacedItemsRecovered,
        MostValuableLostItemName = record.MostValuableLostItemName,
        MostValuableLostItemValue = record.MostValuableLostItemValue,
        ItemsBroken = record.ItemsBroken
    };

    internal static void RestoreAdventureRecord(AdventureRecord record, AdventureRecordData data)
    {
        record.MonstersKilled = data.MonstersKilled;
        record.DamageDealt = data.DamageDealt;
        record.DamageTaken = data.DamageTaken;
        record.GoldCollected = data.GoldCollected;
        record.GoldSpent = data.GoldSpent;
        record.BossesKilled = data.BossesKilled;
        record.HighestLevelBossName = data.HighestLevelBossName;
        record.HighestLevelBossLevel = data.HighestLevelBossLevel;
        record.DeepestFloorReached = data.DeepestFloorReached;
        record.TimeInDungeonSeconds = data.TimeInDungeonSeconds;
        record.ItemsPermanentlyLost = data.ItemsPermanentlyLost;
        record.ItemsMisplaced = data.ItemsMisplaced;
        record.MisplacedItemsRecovered = data.MisplacedItemsRecovered;
        record.MostValuableLostItemName = data.MostValuableLostItemName;
        record.MostValuableLostItemValue = data.MostValuableLostItemValue;
        record.ItemsBroken = data.ItemsBroken;
    }

    /// <summary>Internal (not private) so Diagnostics/SelfTest.cs can round-trip an Item (including a portable corpse) directly.</summary>
    internal static ItemData ToItemData(Item item) => new()
    {
        Name = item.Name,
        Charges = item.Charges,
        IsIdentified = item.IsIdentified,
        IsLit = item.IsLit,
        RemainingLightDuration = item.RemainingLightDuration,
        Container = item.Container == null ? null : ToContainerData(item.Container),
        Corpse = item.CorpseMetadata == null ? null : ToCorpseMetadataData(item.CorpseMetadata)
    };

    private static CorpseMetadataData ToCorpseMetadataData(CorpseMetadata metadata) => new()
    {
        CorpseId = metadata.CorpseId.ToString(),
        DefinitionId = metadata.DefinitionId,
        OriginalDisplayName = metadata.OriginalDisplayName,
        CreatureType = metadata.CreatureType,
        OriginalLevel = metadata.OriginalLevel,
        OriginalSize = metadata.OriginalSize,
        Weight = metadata.Weight,
        Origin = metadata.Origin,
        OwnerName = metadata.OwnerName
    };

    private static CorpseMetadata FromCorpseMetadataData(CorpseMetadataData data) => new(
        Guid.Parse(data.CorpseId), data.DefinitionId, data.OriginalDisplayName, data.CreatureType,
        data.OriginalLevel, data.OriginalSize, data.Weight, data.Origin, data.OwnerName);

    /// <summary>Internal (not private) so Diagnostics/SelfTest.cs can round-trip a Corpse directly.</summary>
    internal static CorpseData ToCorpseData(Corpse corpse) => new()
    {
        X = corpse.X,
        Y = corpse.Y,
        Metadata = ToCorpseMetadataData(corpse.Metadata),
        Contents = corpse.Container.Contents.Items.Select(ToItemData).ToList()
    };

    /// <summary>Internal (not private) so Diagnostics/SelfTest.cs can round-trip a Corpse directly.</summary>
    internal static Corpse FromCorpseData(CorpseData data)
    {
        var metadata = FromCorpseMetadataData(data.Metadata);
        var items = data.Contents.Select(ResolveItem).Where(item => item != null).ToList();
        return new Corpse(data.X, data.Y, metadata, items);
    }

    /// <summary>
    /// A save written before version 33.3.0 shortened container names still encodes the old
    /// "Small Pouch of Lightening (4-slot)"-style Name, which no longer exists anywhere in the
    /// catalog now that the size word and slot-count suffix were dropped (see ContainerCatalog's
    /// own doc comment) -- without this, ResolveTemplate would find zero candidates and the bag
    /// would simply vanish on load. Strips the old prefix/suffix back down to the current bare
    /// name ("Pouch of Lightening") so the item resolves normally again.
    /// </summary>
    private static readonly Regex LegacyContainerNamePattern = new(@"^(?:Small|Medium|Large) (.+) \(\d+-slot\)$");

    /// <summary>
    /// Looks up the catalog template for a saved item by Name -- except for a portable bag,
    /// where Name is deliberately NOT unique across the catalog (e.g. every "Pouch of Lightening"
    /// -- 4-slot, 6-slot, and 8-slot -- shares the exact same display Name, since the slot count
    /// is shown elsewhere rather than in the name itself; see ContainerCatalog's own doc comment).
    /// For those, the saved Container's SlotCapacity/WeightReduction (already persisted regardless)
    /// disambiguate which of the same-named templates this particular save entry actually was.
    /// </summary>
    private static Item ResolveTemplate(ItemData data)
    {
        var candidates = Items.All.Where(i => i.Name == data.Name).ToList();
        if (candidates.Count == 0)
        {
            var legacyMatch = LegacyContainerNamePattern.Match(data.Name ?? "");
            if (legacyMatch.Success)
            {
                candidates = Items.All.Where(i => i.Name == legacyMatch.Groups[1].Value).ToList();
            }
        }

        if (candidates.Count <= 1)
        {
            return candidates.FirstOrDefault();
        }

        return candidates.FirstOrDefault(i => i.Container != null && data.Container != null
            && i.Container.SlotCapacity == data.Container.SlotCapacity
            && i.Container.WeightReduction == data.Container.WeightReduction)
            ?? candidates[0];
    }

    /// <returns>Null if the catalog no longer has an item by this name (e.g. removed/renamed since the save was written).</returns>
    // Bumped to internal (from private) so SelfTest can verify catalog-derived properties
    // (ItemSize in particular) resolve correctly and deterministically -- same testability
    // convention as the other internal SaveManager conversion methods.
    internal static Item ResolveItem(ItemData data)
    {
        // Corpse System: a portable corpse item has no catalog template at all -- it's built
        // fresh per death, never authored in Items.cs -- so it's resolved directly from its own
        // embedded metadata instead of the ordinary Name-based lookup below. Never regenerates
        // loot or a fresh identity; CreatePortableItem is a pure function of the (already
        // persisted, never recalculated) metadata alone.
        if (data.Corpse != null)
        {
            return CorpseItemFactory.CreatePortableItem(FromCorpseMetadataData(data.Corpse));
        }

        var template = ResolveTemplate(data);
        if (template == null)
        {
            return null;
        }

        // Nothing per-instance to restore -- the shared catalog reference is fine.
        if (!template.RequiresUniqueInstance)
        {
            return template;
        }

        // Charged items need their own independent Charges counter, an unidentified item needs
        // its own independent IsIdentified flag, a light-emitting item needs its own independent
        // IsLit/RemainingLightDuration, and a container item needs its own independent Contents
        // list -- same as DungeonGenerator.SpawnItems.
        var item = template.Clone();
        item.Charges = data.Charges;
        item.IsIdentified = data.IsIdentified;
        if (template.EmitsLight)
        {
            item.IsLit = data.IsLit;
            item.RemainingLightDuration = data.RemainingLightDuration;
        }
        if (item.Container != null && data.Container != null)
        {
            foreach (var contentData in data.Container.Contents)
            {
                var contentItem = ResolveItem(contentData);
                if (contentItem != null)
                {
                    item.Container.Contents.AddItem(contentItem);
                }
            }
            item.Container.IsOpen = data.Container.IsOpen;
        }
        return item;
    }

    private static ContainerData ToContainerData(ContainerComponent container) => new()
    {
        Kind = container.Kind,
        SizeClass = container.SizeClass,
        SlotCapacity = container.SlotCapacity,
        WeightReduction = container.WeightReduction,
        IsOpen = container.IsOpen,
        Contents = container.Contents.Items.Select(ToItemData).ToList()
    };

    private static Dictionary<PrimaryAttribute, StatBlockData> SaveStats(CharacterStats stats)
    {
        var data = new Dictionary<PrimaryAttribute, StatBlockData>();
        foreach (PrimaryAttribute attribute in Enum.GetValues<PrimaryAttribute>())
        {
            var block = stats.Get(attribute);
            data[attribute] = new StatBlockData
            {
                BaseValue = block.BaseValue,
                ClassModifier = block.ClassModifier,
                EquipmentModifier = block.EquipmentModifier,
                OtherModifier = block.OtherModifier
            };
        }
        return data;
    }

    /// <summary>
    /// Revised Luck proposal's migration rule: an old save written before Luck existed has no
    /// PrimaryAttribute.Luck entry in `data` at all, so this loop below never populates it --
    /// CharacterStats.Get would otherwise lazily default it to a bare `new StatBlock()` (BaseValue
    /// 0, i.e. LCK: 0), which isn't a value that could ever come from an actual roll. A fixed
    /// racial midpoint is used instead of rolling fresh so loading the same old save is always
    /// deterministic -- computed from the race's own Luck range rather than hardcoding the
    /// proposal's four numbers a second time, so the two can never drift apart if the ranges are
    /// ever retuned. Integer math (min+max+1)/2 rounds the two half-integer midpoints (Dwarf 9.5,
    /// Elf 11.5) up, matching the proposal's specified 10 and 12 exactly.
    /// </summary>
    private static CharacterStats LoadStats(Dictionary<PrimaryAttribute, StatBlockData> data, Race race)
    {
        var stats = new CharacterStats();
        foreach (var (attribute, blockData) in data)
        {
            var block = stats.Get(attribute);
            block.BaseValue = blockData.BaseValue;
            block.ClassModifier = blockData.ClassModifier;
            block.OtherModifier = blockData.OtherModifier;
            // EquipmentModifier is deliberately not restored here -- it's derived from
            // whatever's currently equipped and gets rebuilt by ToPlayer's equipment
            // restoration loop below. Restoring the raw saved number too would double it.
        }

        if (!data.ContainsKey(PrimaryAttribute.Luck))
        {
            var luckRange = race.StatRanges[PrimaryAttribute.Luck];
            stats.Get(PrimaryAttribute.Luck).BaseValue = (luckRange.Min + luckRange.Max + 1) / 2;
        }

        return stats;
    }

    /// <summary>Permadeath enforcement: call this the moment the player dies, before anything else.</summary>
    public static void DeleteSave()
    {
        if (SaveExists())
        {
            File.Delete(SaveFileName);
        }
    }
}
