// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Forged.MapServer.Chat;
using Forged.MapServer.Chrono;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.D;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.AreaTriggers;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.LootManagement;
using Forged.MapServer.Mails;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Movement;
using Forged.MapServer.Phasing;
using Forged.MapServer.Quest;
using Forged.MapServer.Reputation;
using Forged.MapServer.Spells;
using Forged.MapServer.World;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Globals;

public sealed class GameObjectManager
{
    public Dictionary<uint, MultiMap<uint, ScriptInfo>> EventScripts { get; set; } = new();
    //Faction Change
    public Dictionary<uint, uint> FactionChangeAchievements { get; set; } = new();

    public Dictionary<uint, uint> FactionChangeItemsAllianceToHorde { get; set; } = new();
    public Dictionary<uint, uint> FactionChangeItemsHordeToAlliance { get; set; } = new();
    public Dictionary<uint, uint> FactionChangeQuests { get; set; } = new();
    public Dictionary<uint, uint> FactionChangeReputation { get; set; } = new();
    public Dictionary<uint, uint> FactionChangeSpells { get; set; } = new();
    public Dictionary<uint, uint> FactionChangeTitles { get; set; } = new();
    //Maps
    public Dictionary<uint, GameTele> GameTeleStorage { get; set; } = new();

    public MultiMap<uint, GraveYardData> GraveYardStorage { get; set; } = new();
    public Dictionary<uint, MultiMap<uint, ScriptInfo>> SpellScripts { get; set; } = new();
    public Dictionary<uint, MultiMap<uint, ScriptInfo>> WaypointScripts { get; set; } = new();
    private static readonly float[] armorMultipliers = new float[]
    {
        0.00f, // INVTYPE_NON_EQUIP
        0.60f, // INVTYPE_HEAD
        0.00f, // INVTYPE_NECK
        0.60f, // INVTYPE_SHOULDERS
        0.00f, // INVTYPE_BODY
        1.00f, // INVTYPE_CHEST
        0.33f, // INVTYPE_WAIST
        0.72f, // INVTYPE_LEGS
        0.48f, // INVTYPE_FEET
        0.33f, // INVTYPE_WRISTS
        0.33f, // INVTYPE_HANDS
        0.00f, // INVTYPE_FINGER
        0.00f, // INVTYPE_TRINKET
        0.00f, // INVTYPE_WEAPON
        0.72f, // INVTYPE_SHIELD
        0.00f, // INVTYPE_RANGED
        0.00f, // INVTYPE_CLOAK
        0.00f, // INVTYPE_2HWEAPON
        0.00f, // INVTYPE_BAG
        0.00f, // INVTYPE_TABARD
        1.00f, // INVTYPE_ROBE
        0.00f, // INVTYPE_WEAPONMAINHAND
        0.00f, // INVTYPE_WEAPONOFFHAND
        0.00f, // INVTYPE_HOLDABLE
        0.00f, // INVTYPE_AMMO
        0.00f, // INVTYPE_THROWN
        0.00f, // INVTYPE_RANGEDRIGHT
        0.00f, // INVTYPE_QUIVER
        0.00f, // INVTYPE_RELIC
        0.00f, // INVTYPE_PROFESSION_TOOL
        0.00f, // INVTYPE_PROFESSION_GEAR
        0.00f, // INVTYPE_EQUIPABLE_SPELL_OFFENSIVE
        0.00f, // INVTYPE_EQUIPABLE_SPELL_UTILITY
        0.00f, // INVTYPE_EQUIPABLE_SPELL_DEFENSIVE
        0.00f, // INVTYPE_EQUIPABLE_SPELL_MOBILITY
    };

    private static readonly float[] qualityMultipliers = new float[]
        {
        0.92f, 0.92f, 0.92f, 1.11f, 1.32f, 1.61f, 0.0f, 0.0f
    };
    private static readonly float[] weaponMultipliers = new float[]
    {
        0.91f, // ITEM_SUBCLASS_WEAPON_AXE
        1.00f, // ITEM_SUBCLASS_WEAPON_AXE2
        1.00f, // ITEM_SUBCLASS_WEAPON_BOW
        1.00f, // ITEM_SUBCLASS_WEAPON_GUN
        0.91f, // ITEM_SUBCLASS_WEAPON_MACE
        1.00f, // ITEM_SUBCLASS_WEAPON_MACE2
        1.00f, // ITEM_SUBCLASS_WEAPON_POLEARM
        0.91f, // ITEM_SUBCLASS_WEAPON_SWORD
        1.00f, // ITEM_SUBCLASS_WEAPON_SWORD2
        1.00f, // ITEM_SUBCLASS_WEAPON_WARGLAIVES
        1.00f, // ITEM_SUBCLASS_WEAPON_STAFF
        0.00f, // ITEM_SUBCLASS_WEAPON_EXOTIC
        0.00f, // ITEM_SUBCLASS_WEAPON_EXOTIC2
        0.66f, // ITEM_SUBCLASS_WEAPON_FIST_WEAPON
        0.00f, // ITEM_SUBCLASS_WEAPON_MISCELLANEOUS
        0.66f, // ITEM_SUBCLASS_WEAPON_DAGGER
        0.00f, // ITEM_SUBCLASS_WEAPON_THROWN
        0.00f, // ITEM_SUBCLASS_WEAPON_SPEAR
        1.00f, // ITEM_SUBCLASS_WEAPON_CROSSBOW
        0.66f, // ITEM_SUBCLASS_WEAPON_WAND
        0.66f, // ITEM_SUBCLASS_WEAPON_FISHING_POLE
    };

    private readonly Dictionary<ulong, AccessRequirement> _accessRequirementStorage = new();
    private readonly MultiMap<uint, uint> _areaTriggerScriptStorage = new();
    private readonly Dictionary<uint, AreaTriggerStruct> _areaTriggerStorage = new();
    private readonly Dictionary<uint, uint> _baseXPTable = new();
    private readonly Dictionary<uint, VendorItemData> _cacheVendorItemStorage = new();
    private readonly List<RaceClassAvailability> _classExpansionRequirementStorage = new();
    private readonly CliDB _cliDB;
    private readonly IConfiguration _configuration;
    private readonly ClassFactory _classFactory;
    private readonly Dictionary<ulong, CreatureAddon> _creatureAddonStorage = new();
    private readonly Dictionary<uint, CreatureBaseStats> _creatureBaseStatsStorage = new();
    private readonly Dictionary<ulong, CreatureData> _creatureDataStorage = new();
    private readonly Dictionary<(uint creatureId, uint gossipMenuId, uint gossipOptionIndex), uint> _creatureDefaultTrainers = new();
    //Locales
    private readonly Dictionary<uint, CreatureLocale> _creatureLocaleStorage = new();

    private readonly Dictionary<uint, CreatureModelInfo> _creatureModelStorage = new();
    private readonly Dictionary<ulong, CreatureMovementData> _creatureMovementOverrides = new();
    private readonly MultiMap<uint, uint> _creatureQuestInvolvedRelations = new();
    private readonly MultiMap<uint, uint> _creatureQuestInvolvedRelationsReverse = new();
    private readonly MultiMap<uint, uint> _creatureQuestItemStorage = new();
    private readonly MultiMap<uint, uint> _creatureQuestRelations = new();
    private readonly Dictionary<uint, CreatureSummonedData> _creatureSummonedDataStorage = new();
    private readonly Dictionary<uint, CreatureAddon> _creatureTemplateAddonStorage = new();
    //Creature
    private readonly Dictionary<uint, CreatureTemplate> _creatureTemplateStorage = new();

    //General
    private readonly Dictionary<uint, StringArray> _cypherStringStorage = new();

    private readonly List<uint>[] _difficultyEntries = new List<uint>[SharedConst.MaxCreatureDifficulties];
    private readonly MultiMap<ulong, DungeonEncounter> _dungeonEncounterStorage = new();
    private readonly MultiMap<uint, Tuple<uint, EquipmentInfo>> _equipmentInfoStorage = new();
    private readonly MultiMap<int, uint> _exclusiveQuestGroups = new();
    private readonly Dictionary<uint, int> _fishingBaseForAreaStorage = new();
    private readonly Dictionary<ulong, GameObjectAddon> _gameObjectAddonStorage = new();
    private readonly Dictionary<ulong, GameObjectData> _gameObjectDataStorage = new();
    private readonly List<uint> _gameObjectForQuestStorage = new();
    private readonly Dictionary<uint, GameObjectLocale> _gameObjectLocaleStorage = new();
    private readonly Dictionary<ulong, GameObjectOverride> _gameObjectOverrideStorage = new();
    private readonly MultiMap<uint, uint> _gameObjectQuestItemStorage = new();
    private readonly Dictionary<ulong, GameObjectTemplateAddon> _gameObjectTemplateAddonStorage = new();
    //GameObject
    private readonly Dictionary<uint, GameObjectTemplate> _gameObjectTemplateStorage = new();

    private readonly MultiMap<uint, uint> _goQuestInvolvedRelations = new();
    private readonly MultiMap<uint, uint> _goQuestInvolvedRelationsReverse = new();
    private readonly MultiMap<uint, uint> _goQuestRelations = new();
    private readonly Dictionary<uint, GossipMenuAddon> _gossipMenuAddonStorage = new();
    private readonly Dictionary<Tuple<uint, uint>, GossipMenuItemsLocale> _gossipMenuItemsLocaleStorage = new();
    private readonly MultiMap<uint, GossipMenuItems> _gossipMenuItemsStorage = new();
    //Gossip
    private readonly MultiMap<uint, GossipMenus> _gossipMenusStorage = new();

    private readonly Dictionary<HighGuid, ObjectGuidGenerator> _guidGenerators = new();
    // already loaded difficulty 1 value in creatures, used in CheckCreatureTemplate
    private readonly List<uint>[] _hasDifficultyEntries = new List<uint>[SharedConst.MaxCreatureDifficulties];

    private readonly MultiMap<ushort, InstanceSpawnGroupInfo> _instanceSpawnGroupStorage = new();
    private readonly Dictionary<uint, InstanceTemplate> _instanceTemplateStorage = new();
    //Item
    private readonly Dictionary<uint, ItemTemplate> _itemTemplateStorage = new();

    private readonly Dictionary<int, JumpChargeParams> _jumpChargeParams = new();
    private readonly Dictionary<ObjectGuid, ObjectGuid> _linkedRespawnStorage = new();
    private readonly MultiMap<byte, MailLevelReward> _mailLevelRewardStorage = new();
    private readonly Dictionary<(uint mapId, Difficulty difficulty), Dictionary<uint, CellObjectGuids>> _mapObjectGuidsStore = new();
    private readonly Dictionary<(uint mapId, Difficulty diffuculty, uint phaseId), Dictionary<uint, CellObjectGuids>> _mapPersonalObjectGuidsStore = new();
    // already loaded creatures with difficulty 1 values, used in CheckCreatureTemplate
    private readonly Dictionary<uint, NpcText> _npcTextStorage = new();

    private readonly Dictionary<uint, PageTextLocale> _pageTextLocaleStorage = new();
    private readonly Dictionary<uint, PageText> _pageTextStorage = new();
    private readonly MultiMap<uint, string> _petHalfName0 = new();
    private readonly MultiMap<uint, string> _petHalfName1 = new();
    //Pets
    private readonly Dictionary<uint, PetLevelInfo[]> _petInfoStore = new();

    private readonly MultiMap<uint, PhaseAreaInfo> _phaseInfoByArea = new();
    //Spells /Skills / Phases
    private readonly Dictionary<uint, PhaseInfoStruct> _phaseInfoById = new();

    private readonly Dictionary<uint, string> _phaseNameStorage = new();
    private readonly Dictionary<int, PlayerChoiceLocale> _playerChoiceLocales = new();
    private readonly Dictionary<int /*choiceId*/, PlayerChoice> _playerChoices = new();
    private readonly Dictionary<uint, PointOfInterestLocale> _pointOfInterestLocaleStorage = new();
    private readonly Dictionary<uint, PointOfInterest> _pointsOfInterestStorage = new();
    private readonly MultiMap<uint, uint> _questAreaTriggerStorage = new();
    private readonly Dictionary<uint, QuestGreetingLocale>[] _questGreetingLocaleStorage = new Dictionary<uint, QuestGreetingLocale>[2];
    private readonly Dictionary<uint, QuestGreeting>[] _questGreetingStorage = new Dictionary<uint, QuestGreeting>[2];
    private readonly Dictionary<uint, QuestObjective> _questObjectives = new();
    private readonly Dictionary<uint, QuestObjectivesLocale> _questObjectivesLocaleStorage = new();
    private readonly Dictionary<uint, QuestOfferRewardLocale> _questOfferRewardLocaleStorage = new();
    private readonly Dictionary<uint, QuestPOIData> _questPOIStorage = new();
    private readonly Dictionary<uint, QuestRequestItemsLocale> _questRequestItemsLocaleStorage = new();
    private readonly Dictionary<uint, QuestTemplateLocale> _questTemplateLocaleStorage = new();
    //QuestId
    private readonly Dictionary<uint, Quest.Quest> _questTemplates = new();

    private readonly List<Quest.Quest> _questTemplatesAutoPush = new();
    private readonly Dictionary<byte, RaceUnlockRequirement> _raceUnlockRequirementStorage = new();
    private readonly Dictionary<uint, string> _realmNameStorage = new();
    private readonly Dictionary<uint, ReputationOnKillEntry> _repOnKillStorage = new();
    private readonly Dictionary<uint, RepRewardRate> _repRewardRateStorage = new();
    private readonly Dictionary<uint, RepSpilloverTemplate> _repSpilloverTemplateStorage = new();
    private readonly List<string> _reservedNamesStorage = new();
    private readonly Dictionary<uint, SceneTemplate> _sceneTemplateStorage = new();
    //Scripts
    private readonly ScriptNameContainer _scriptNamesStorage = new();

    private readonly Dictionary<uint, SkillTiersEntry> _skillTiers = new();
    private readonly Dictionary<uint, SpawnGroupTemplateData> _spawnGroupDataStorage = new();
    private readonly MultiMap<uint, SpawnMetadata> _spawnGroupMapStorage = new();
    private readonly MultiMap<uint, uint> _spawnGroupsByMap = new();
    private readonly MultiMap<uint, SpellClickInfo> _spellClickInfoStorage = new();
    private readonly MultiMap<uint, uint> _spellScriptsStorage = new();
    private readonly List<uint> _tavernAreaTriggerStorage = new();
    private readonly MultiMap<Tuple<uint, SummonerType, byte>, TempSummonData> _tempSummonDataStorage = new();
    private readonly Dictionary<uint, TerrainSwapInfo> _terrainSwapInfoById = new();
    private readonly MultiMap<uint, TerrainSwapInfo> _terrainSwapInfoByMap = new();
    private readonly Dictionary<uint, Trainer> _trainers = new();
    private readonly List<ushort> _transportMaps = new();
    private readonly MultiMap<ulong, VehicleAccessory> _vehicleAccessoryStore = new();
    private readonly Dictionary<uint, VehicleSeatAddon> _vehicleSeatAddonStore = new();
    private readonly MultiMap<uint, VehicleAccessory> _vehicleTemplateAccessoryStore = new();
    //Player
    //Vehicles
    private readonly Dictionary<uint, VehicleTemplate> _vehicleTemplateStore = new();

    private readonly WorldDatabase _worldDatabase;
    private readonly Dictionary<uint, WorldSafeLocsEntry> _worldSafeLocs = new();
    // first free id for selected id type
    private uint _auctionId;
    private ulong _creatureSpawnId;
    private ulong _equipmentSetGuid;
    private ulong _gameObjectSpawnId;
    private uint _hiPetNumber;
    private ulong _mailId;
    private uint[] _playerXPperLevel;
    private ulong _voidItemId;
    public GameObjectManager(CliDB cliDB, WorldDatabase worldDatabase, IConfiguration configuration, ClassFactory classFactory)
    {
        _cliDB = cliDB;
        _worldDatabase = worldDatabase;
        _configuration = configuration;
        _classFactory = classFactory;

        for (var i = 0; i < SharedConst.MaxCreatureDifficulties; ++i)
        {
            _difficultyEntries[i] = new List<uint>();
            _hasDifficultyEntries[i] = new List<uint>();
        }
    }

    public Dictionary<Race, Dictionary<PlayerClass, PlayerInfo>> PlayerInfos { get; } = new();
    public static void AddLocaleString(string value, Locale locale, StringArray data)
    {
        if (!string.IsNullOrEmpty(value))
            data[(int)locale] = value;
    }

    public static PetNameInvalidReason CheckPetName(string name)
    {
        if (name.Length > 12)
            return PetNameInvalidReason.TooLong;

        var minName = _configuration.GetDefaultValue("MinPetName", 2);

        if (name.Length < minName)
            return PetNameInvalidReason.TooShort;

        var strictMask = _configuration.GetDefaultValue("StrictPetNames", 0);

        if (!IsValidString(name, strictMask, false))
            return PetNameInvalidReason.MixedLanguages;

        return PetNameInvalidReason.Success;
    }

    public static ResponseCodes CheckPlayerName(string name, Locale locale, bool create = false)
    {
        if (name.Length > 12)
            return ResponseCodes.CharNameTooLong;

        var minName = _configuration.GetDefaultValue("MinPlayerName", 2);

        if (name.Length < minName)
            return ResponseCodes.CharNameTooShort;

        var strictMask = _configuration.GetDefaultValue("StrictPlayerNames", 0);

        if (!IsValidString(name, strictMask, false, create))
            return ResponseCodes.CharNameMixedLanguages;

        name = name.ToLower();

        for (var i = 2; i < name.Length; ++i)
            if (name[i] == name[i - 1] && name[i] == name[i - 2])
                return ResponseCodes.CharNameThreeConsecutive;

        return Global.DB2Mgr.ValidateName(name, locale);
    }

    public static void ChooseCreatureFlags(CreatureTemplate cInfo, out ulong npcFlag, out uint unitFlags, out uint unitFlags2, out uint unitFlags3, out uint dynamicFlags, CreatureData data = null)
    {
        npcFlag = data != null && data.Npcflag != 0 ? data.Npcflag : cInfo.Npcflag;
        unitFlags = data != null && data.UnitFlags != 0 ? data.UnitFlags : (uint)cInfo.UnitFlags;
        unitFlags2 = data != null && data.UnitFlags2 != 0 ? data.UnitFlags2 : cInfo.UnitFlags2;
        unitFlags3 = data != null && data.UnitFlags3 != 0 ? data.UnitFlags3 : cInfo.UnitFlags3;
        dynamicFlags = data != null && data.Dynamicflags != 0 ? data.Dynamicflags : cInfo.DynamicFlags;
    }

    public static CreatureModel ChooseDisplayId(CreatureTemplate cinfo, CreatureData data = null)
    {
        // Load creature model (display id)
        if (data != null && data.Displayid != 0)
        {
            var model = cinfo.GetModelWithDisplayId(data.Displayid);

            if (model != null)
                return model;
        }

        if (!cinfo.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.Trigger))
        {
            var model = cinfo.GetRandomValidModel();

            if (model != null)
                return model;
        }

        // Triggers by default receive the invisible model
        return cinfo.GetFirstInvisibleModel();
    }

    public static ExtendedPlayerName ExtractExtendedPlayerName(string name)
    {
        var pos = name.IndexOf('-');

        if (pos != -1)
            return new ExtendedPlayerName(name.Substring(0, pos), name[(pos + 1)..]);
        else
            return new ExtendedPlayerName(name, "");
    }

    public static void GetLocaleString(StringArray data, Locale locale, ref string value)
    {
        if (data.Length > (int)locale && !string.IsNullOrEmpty(data[(int)locale]))
            value = data[(int)locale];
    }

    public static bool IsValidCharterName(string name)
    {
        if (name.Length > 24)
            return false;

        var minName = _configuration.GetDefaultValue("MinCharterName", 2);

        if (name.Length < minName)
            return false;

        var strictMask = _configuration.GetDefaultValue("StrictCharterNames", 0);

        return IsValidString(name, strictMask, true);
    }

    //Static Methods
    public static bool NormalizePlayerName(ref string name)
    {
        if (name.IsEmpty())
            return false;

        //CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
        //TextInfo textInfo = cultureInfo.TextInfo;

        //str = textInfo.ToTitleCase(str);

        name = name.ToLower();

        var charArray = name.ToCharArray();
        charArray[0] = char.ToUpper(charArray[0]);

        name = new string(charArray);

        return true;
    }
    public void AddCreatureToGrid(CreatureData data)
    {
        AddSpawnDataToGrid(data);
    }

    public void AddGameObjectToGrid(GameObjectData data)
    {
        AddSpawnDataToGrid(data);
    }

    public bool AddGameTele(GameTele tele)
    {
        // find max id
        uint newId = 0;

        foreach (var itr in GameTeleStorage)
            if (itr.Key > newId)
                newId = itr.Key;

        // use next
        ++newId;

        GameTeleStorage[newId] = tele;

        var stmt = _worldDatabase.GetPreparedStatement(WorldStatements.INS_GAME_TELE);

        stmt.AddValue(0, newId);
        stmt.AddValue(1, tele.PosX);
        stmt.AddValue(2, tele.PosY);
        stmt.AddValue(3, tele.PosZ);
        stmt.AddValue(4, tele.Orientation);
        stmt.AddValue(5, tele.MapId);
        stmt.AddValue(6, tele.Name);

        _worldDatabase.Execute(stmt);

        return true;
    }

    public bool AddGraveYardLink(uint id, uint zoneId, TeamFaction team, bool persist = true)
    {
        if (FindGraveYardData(id, zoneId) != null)
            return false;

        // add link to loaded data
        GraveYardData data = new()
        {
            SafeLocId = id,
            Team = (uint)team
        };

        GraveYardStorage.Add(zoneId, data);

        // add link to DB
        if (persist)
        {
            var stmt = _worldDatabase.GetPreparedStatement(WorldStatements.INS_GRAVEYARD_ZONE);

            stmt.AddValue(0, id);
            stmt.AddValue(1, zoneId);
            stmt.AddValue(2, (uint)team);

            _worldDatabase.Execute(stmt);
        }

        return true;
    }

    public void AddVendorItem(uint entry, VendorItem vItem, bool persist = true)
    {
        var vList = _cacheVendorItemStorage[entry];
        vList.AddItem(vItem);

        if (persist)
        {
            var stmt = _worldDatabase.GetPreparedStatement(WorldStatements.INS_NPC_VENDOR);

            stmt.AddValue(0, entry);
            stmt.AddValue(1, vItem.Item);
            stmt.AddValue(2, vItem.Maxcount);
            stmt.AddValue(3, vItem.Incrtime);
            stmt.AddValue(4, vItem.ExtendedCost);
            stmt.AddValue(5, (byte)vItem.Type);

            _worldDatabase.Execute(stmt);
        }
    }

    public void CheckCreatureTemplate(CreatureTemplate cInfo)
    {
        if (cInfo == null)
            return;

        var ok = true; // bool to allow continue outside this loop

        for (uint diff = 0; diff < SharedConst.MaxCreatureDifficulties && ok; ++diff)
        {
            if (cInfo.DifficultyEntry[diff] == 0)
                continue;

            ok = false; // will be set to true at the end of this loop again

            var difficultyInfo = GetCreatureTemplate(cInfo.DifficultyEntry[diff]);

            if (difficultyInfo == null)
            {
                Log.Logger.Error("Creature (Entry: {0}) has `difficulty_entry_{1}`={2} but creature entry {3} does not exist.",
                                 cInfo.Entry,
                                 diff + 1,
                                 cInfo.DifficultyEntry[diff],
                                 cInfo.DifficultyEntry[diff]);

                continue;
            }

            var ok2 = true;

            for (uint diff2 = 0; diff2 < SharedConst.MaxCreatureDifficulties && ok2; ++diff2)
            {
                ok2 = false;

                if (_difficultyEntries[diff2].Contains(cInfo.Entry))
                {
                    Log.Logger.Error("Creature (Entry: {0}) is listed as `difficulty_entry_{1}` of another creature, but itself lists {2} in `difficulty_entry_{3}`.",
                                     cInfo.Entry,
                                     diff2 + 1,
                                     cInfo.DifficultyEntry[diff],
                                     diff + 1);

                    continue;
                }

                if (_difficultyEntries[diff2].Contains(cInfo.DifficultyEntry[diff]))
                {
                    Log.Logger.Error("Creature (Entry: {0}) already listed as `difficulty_entry_{1}` for another entry.", cInfo.DifficultyEntry[diff], diff2 + 1);

                    continue;
                }

                if (_hasDifficultyEntries[diff2].Contains(cInfo.DifficultyEntry[diff]))
                {
                    Log.Logger.Error("Creature (Entry: {0}) has `difficulty_entry_{1}`={2} but creature entry {3} has itself a value in `difficulty_entry_{4}`.",
                                     cInfo.Entry,
                                     diff + 1,
                                     cInfo.DifficultyEntry[diff],
                                     cInfo.DifficultyEntry[diff],
                                     diff2 + 1);

                    continue;
                }

                ok2 = true;
            }

            if (!ok2)
                continue;

            if (cInfo.HealthScalingExpansion > difficultyInfo.HealthScalingExpansion)
                Log.Logger.Error("Creature (Id: {0}, Expansion {1}) has different `HealthScalingExpansion` in difficulty {2} mode (Id: {3}, Expansion: {4}).",
                                 cInfo.Entry,
                                 cInfo.HealthScalingExpansion,
                                 diff + 1,
                                 cInfo.DifficultyEntry[diff],
                                 difficultyInfo.HealthScalingExpansion);

            if (cInfo.Minlevel > difficultyInfo.Minlevel)
                Log.Logger.Error("Creature (Entry: {0}, minlevel: {1}) has lower `minlevel` in difficulty {2} mode (Entry: {3}, minlevel: {4}).",
                                 cInfo.Entry,
                                 cInfo.Minlevel,
                                 diff + 1,
                                 cInfo.DifficultyEntry[diff],
                                 difficultyInfo.Minlevel);

            if (cInfo.Maxlevel > difficultyInfo.Maxlevel)
                Log.Logger.Error("Creature (Entry: {0}, maxlevel: {1}) has lower `maxlevel` in difficulty {2} mode (Entry: {3}, maxlevel: {4}).",
                                 cInfo.Entry,
                                 cInfo.Maxlevel,
                                 diff + 1,
                                 cInfo.DifficultyEntry[diff],
                                 difficultyInfo.Maxlevel);

            if (cInfo.Faction != difficultyInfo.Faction)
                Log.Logger.Error("Creature (Entry: {0}, faction: {1}) has different `faction` in difficulty {2} mode (Entry: {3}, faction: {4}).",
                                 cInfo.Entry,
                                 cInfo.Faction,
                                 diff + 1,
                                 cInfo.DifficultyEntry[diff],
                                 difficultyInfo.Faction);

            if (cInfo.UnitClass != difficultyInfo.UnitClass)
            {
                Log.Logger.Error("Creature (Entry: {0}, class: {1}) has different `unit_class` in difficulty {2} mode (Entry: {3}, class: {4}).",
                                 cInfo.Entry,
                                 cInfo.UnitClass,
                                 diff + 1,
                                 cInfo.DifficultyEntry[diff],
                                 difficultyInfo.UnitClass);

                continue;
            }

            if (cInfo.Npcflag != difficultyInfo.Npcflag)
            {
                Log.Logger.Error("Creature (Entry: {0}) has different `npcflag` in difficulty {1} mode (Entry: {2}).", cInfo.Entry, diff + 1, cInfo.DifficultyEntry[diff]);
                Log.Logger.Error("Possible FIX: UPDATE `creature_template` SET `npcflag`=`npcflag`^{0} WHERE `entry`={1};", cInfo.Npcflag ^ difficultyInfo.Npcflag, cInfo.DifficultyEntry[diff]);

                continue;
            }

            if (cInfo.DmgSchool != difficultyInfo.DmgSchool)
            {
                Log.Logger.Error("Creature (Entry: {0}, `dmgschool`: {1}) has different `dmgschool` in difficulty {2} mode (Entry: {3}, `dmgschool`: {4}).",
                                 cInfo.Entry,
                                 cInfo.DmgSchool,
                                 diff + 1,
                                 cInfo.DifficultyEntry[diff],
                                 difficultyInfo.DmgSchool);

                Log.Logger.Error("Possible FIX: UPDATE `creature_template` SET `dmgschool`={0} WHERE `entry`={1};", cInfo.DmgSchool, cInfo.DifficultyEntry[diff]);
            }

            if (cInfo.UnitFlags2 != difficultyInfo.UnitFlags2)
            {
                Log.Logger.Error("Creature (Entry: {0}, `unit_flags2`: {1}) has different `unit_flags2` in difficulty {2} mode (Entry: {3}, `unit_flags2`: {4}).",
                                 cInfo.Entry,
                                 cInfo.UnitFlags2,
                                 diff + 1,
                                 cInfo.DifficultyEntry[diff],
                                 difficultyInfo.UnitFlags2);

                Log.Logger.Error("Possible FIX: UPDATE `creature_template` SET `unit_flags2`=`unit_flags2`^{0} WHERE `entry`={1};", cInfo.UnitFlags2 ^ difficultyInfo.UnitFlags2, cInfo.DifficultyEntry[diff]);
            }

            if (cInfo.Family != difficultyInfo.Family)
                Log.Logger.Error("Creature (Entry: {0}, family: {1}) has different `family` in difficulty {2} mode (Entry: {3}, family: {4}).",
                                 cInfo.Entry,
                                 cInfo.Family,
                                 diff + 1,
                                 cInfo.DifficultyEntry[diff],
                                 difficultyInfo.Family);

            if (cInfo.TrainerClass != difficultyInfo.TrainerClass)
            {
                Log.Logger.Error("Creature (Entry: {0}) has different `trainer_class` in difficulty {1} mode (Entry: {2}).", cInfo.Entry, diff + 1, cInfo.DifficultyEntry[diff]);

                continue;
            }

            if (cInfo.CreatureType != difficultyInfo.CreatureType)
                Log.Logger.Error("Creature (Entry: {0}, type: {1}) has different `type` in difficulty {2} mode (Entry: {3}, type: {4}).",
                                 cInfo.Entry,
                                 cInfo.CreatureType,
                                 diff + 1,
                                 cInfo.DifficultyEntry[diff],
                                 difficultyInfo.CreatureType);

            if (cInfo.VehicleId == 0 && difficultyInfo.VehicleId != 0)
                Log.Logger.Error("Non-vehicle Creature (Entry: {0}, VehicleId: {1}) has `VehicleId` set in difficulty {2} mode (Entry: {3}, VehicleId: {4}).",
                                 cInfo.Entry,
                                 cInfo.VehicleId,
                                 diff + 1,
                                 cInfo.DifficultyEntry[diff],
                                 difficultyInfo.VehicleId);

            if (cInfo.RegenHealth != difficultyInfo.RegenHealth)
            {
                Log.Logger.Error("Creature (Entry: {0}, RegenHealth: {1}) has different `RegenHealth` in difficulty {2} mode (Entry: {3}, RegenHealth: {4}).",
                                 cInfo.Entry,
                                 cInfo.RegenHealth,
                                 diff + 1,
                                 cInfo.DifficultyEntry[diff],
                                 difficultyInfo.RegenHealth);

                Log.Logger.Error("Possible FIX: UPDATE `creature_template` SET `RegenHealth`={0} WHERE `entry`={1};", cInfo.RegenHealth, cInfo.DifficultyEntry[diff]);
            }

            var differenceMask = cInfo.MechanicImmuneMask & (~difficultyInfo.MechanicImmuneMask);

            if (differenceMask != 0)
            {
                Log.Logger.Error("Creature (Entry: {0}, mechanic_immune_mask: {1}) has weaker immunities in difficulty {2} mode (Entry: {3}, mechanic_immune_mask: {4}).",
                                 cInfo.Entry,
                                 cInfo.MechanicImmuneMask,
                                 diff + 1,
                                 cInfo.DifficultyEntry[diff],
                                 difficultyInfo.MechanicImmuneMask);

                Log.Logger.Error("Possible FIX: UPDATE `creature_template` SET `mechanic_immune_mask`=`mechanic_immune_mask`|{0} WHERE `entry`={1};", differenceMask, cInfo.DifficultyEntry[diff]);
            }

            differenceMask = (uint)((cInfo.FlagsExtra ^ difficultyInfo.FlagsExtra) & (~CreatureFlagsExtra.InstanceBind));

            if (differenceMask != 0)
            {
                Log.Logger.Error("Creature (Entry: {0}, flags_extra: {1}) has different `flags_extra` in difficulty {2} mode (Entry: {3}, flags_extra: {4}).",
                                 cInfo.Entry,
                                 cInfo.FlagsExtra,
                                 diff + 1,
                                 cInfo.DifficultyEntry[diff],
                                 difficultyInfo.FlagsExtra);

                Log.Logger.Error("Possible FIX: UPDATE `creature_template` SET `flags_extra`=`flags_extra`^{0} WHERE `entry`={1};", differenceMask, cInfo.DifficultyEntry[diff]);
            }

            if (difficultyInfo.AIName.IsEmpty())
            {
                Log.Logger.Error("Creature (Entry: {0}) lists difficulty {1} mode entry {2} with `AIName` filled in. `AIName` of difficulty 0 mode creature is always used instead.",
                                 cInfo.Entry,
                                 diff + 1,
                                 cInfo.DifficultyEntry[diff]);

                continue;
            }

            if (difficultyInfo.ScriptID != 0)
            {
                Log.Logger.Error("Creature (Entry: {0}) lists difficulty {1} mode entry {2} with `ScriptName` filled in. `ScriptName` of difficulty 0 mode creature is always used instead.",
                                 cInfo.Entry,
                                 diff + 1,
                                 cInfo.DifficultyEntry[diff]);

                continue;
            }

            _hasDifficultyEntries[diff].Add(cInfo.Entry);
            _difficultyEntries[diff].Add(cInfo.DifficultyEntry[diff]);
            ok = true;
        }

        if (cInfo.MinGold > cInfo.MaxGold)
        {
            Log.Logger.Verbose($"Creature (Entry: {cInfo.Entry}) has `mingold` {cInfo.MinGold} which is greater than `maxgold` {cInfo.MaxGold}, setting `maxgold` to {cInfo.MinGold}.");
            cInfo.MaxGold = cInfo.MinGold;
        }

        if (!_cliDB.FactionTemplateStorage.ContainsKey(cInfo.Faction))
        {
            Log.Logger.Verbose("Creature (Entry: {0}) has non-existing faction template ({1}). This can lead to crashes, set to faction 35", cInfo.Entry, cInfo.Faction);
            cInfo.Faction = 35;
        }

        for (var k = 0; k < SharedConst.MaxCreatureKillCredit; ++k)
            if (cInfo.KillCredit[k] != 0)
                if (GetCreatureTemplate(cInfo.KillCredit[k]) == null)
                {
                    Log.Logger.Verbose("Creature (Entry: {0}) lists non-existing creature entry {1} in `KillCredit{2}`.", cInfo.Entry, cInfo.KillCredit[k], k + 1);
                    cInfo.KillCredit[k] = 0;
                }

        if (cInfo.Models.Empty())
            Log.Logger.Error($"Creature (Entry: {cInfo.Entry}) does not have any existing display id in creature_template_model.");

        if (cInfo.UnitClass == 0 || ((1 << ((int)cInfo.UnitClass - 1)) & (int)PlayerClass.ClassMaskAllCreatures) == 0)
        {
            Log.Logger.Verbose("Creature (Entry: {0}) has invalid unit_class ({1}) in creature_template. Set to 1 (UNIT_CLASS_WARRIOR).", cInfo.Entry, cInfo.UnitClass);
            cInfo.UnitClass = (uint)PlayerClass.Warrior;
        }

        if (cInfo.DmgSchool >= (uint)SpellSchools.Max)
        {
            Log.Logger.Verbose("Creature (Entry: {0}) has invalid spell school value ({1}) in `dmgschool`.", cInfo.Entry, cInfo.DmgSchool);
            cInfo.DmgSchool = (uint)SpellSchools.Normal;
        }

        if (cInfo.BaseAttackTime == 0)
            cInfo.BaseAttackTime = SharedConst.BaseAttackTime;

        if (cInfo.RangeAttackTime == 0)
            cInfo.RangeAttackTime = SharedConst.BaseAttackTime;

        if (cInfo.SpeedWalk == 0.0f)
        {
            Log.Logger.Verbose("Creature (Entry: {0}) has wrong value ({1}) in speed_walk, set to 1.", cInfo.Entry, cInfo.SpeedWalk);
            cInfo.SpeedWalk = 1.0f;
        }

        if (cInfo.SpeedRun == 0.0f)
        {
            Log.Logger.Verbose("Creature (Entry: {0}) has wrong value ({1}) in speed_run, set to 1.14286.", cInfo.Entry, cInfo.SpeedRun);
            cInfo.SpeedRun = 1.14286f;
        }

        if (cInfo.CreatureType != 0 && !_cliDB.CreatureTypeStorage.ContainsKey((uint)cInfo.CreatureType))
        {
            Log.Logger.Verbose("Creature (Entry: {0}) has invalid creature type ({1}) in `type`.", cInfo.Entry, cInfo.CreatureType);
            cInfo.CreatureType = CreatureType.Humanoid;
        }

        if (cInfo.Family != 0 && !_cliDB.CreatureFamilyStorage.ContainsKey(cInfo.Family))
        {
            Log.Logger.Verbose("Creature (Entry: {0}) has invalid creature family ({1}) in `family`.", cInfo.Entry, cInfo.Family);
            cInfo.Family = CreatureFamily.None;
        }

        CheckCreatureMovement("creature_template_movement", cInfo.Entry, cInfo.Movement);

        if (cInfo.HoverHeight < 0.0f)
        {
            Log.Logger.Verbose("Creature (Entry: {0}) has wrong value ({1}) in `HoverHeight`", cInfo.Entry, cInfo.HoverHeight);
            cInfo.HoverHeight = 1.0f;
        }

        if (cInfo.VehicleId != 0)
            if (!_cliDB.VehicleStorage.ContainsKey(cInfo.VehicleId))
            {
                Log.Logger.Verbose("Creature (Entry: {0}) has a non-existing VehicleId ({1}). This *WILL* cause the client to freeze!", cInfo.Entry, cInfo.VehicleId);
                cInfo.VehicleId = 0;
            }

        for (byte j = 0; j < SharedConst.MaxCreatureSpells; ++j)
            if (cInfo.Spells[j] != 0 && !Global.SpellMgr.HasSpellInfo(cInfo.Spells[j], Difficulty.None))
            {
                Log.Logger.Verbose("Creature (Entry: {0}) has non-existing Spell{1} ({2}), set to 0.", cInfo.Entry, j + 1, cInfo.Spells[j]);
                cInfo.Spells[j] = 0;
            }

        if (cInfo.MovementType >= (uint)MovementGeneratorType.MaxDB)
        {
            Log.Logger.Verbose("Creature (Entry: {0}) has wrong movement generator type ({1}), ignored and set to IDLE.", cInfo.Entry, cInfo.MovementType);
            cInfo.MovementType = (uint)MovementGeneratorType.Idle;
        }

        if (cInfo.HealthScalingExpansion is < (int)Expansion.LevelCurrent or >= (int)Expansion.Max)
        {
            Log.Logger.Verbose("Table `creature_template` lists creature (Id: {0}) with invalid `HealthScalingExpansion` {1}. Ignored and set to 0.", cInfo.Entry, cInfo.HealthScalingExpansion);
            cInfo.HealthScalingExpansion = 0;
        }

        if (cInfo.RequiredExpansion > (int)Expansion.Max)
        {
            Log.Logger.Verbose("Table `creature_template` lists creature (Entry: {0}) with `RequiredExpansion` {1}. Ignored and set to 0.", cInfo.Entry, cInfo.RequiredExpansion);
            cInfo.RequiredExpansion = 0;
        }

        var badFlags = (uint)(cInfo.FlagsExtra & ~CreatureFlagsExtra.DBAllowed);

        if (badFlags != 0)
        {
            Log.Logger.Verbose("Table `creature_template` lists creature (Entry: {0}) with disallowed `flags_extra` {1}, removing incorrect Id.", cInfo.Entry, badFlags);
            cInfo.FlagsExtra &= CreatureFlagsExtra.DBAllowed;
        }

        var disallowedUnitFlags = (uint)(cInfo.UnitFlags & ~UnitFlags.Allowed);

        if (disallowedUnitFlags != 0)
        {
            Log.Logger.Verbose($"Table `creature_template` lists creature (Entry: {cInfo.Entry}) with disallowed `unit_flags` {disallowedUnitFlags}, removing incorrect Id.");
            cInfo.UnitFlags &= UnitFlags.Allowed;
        }

        var disallowedUnitFlags2 = (cInfo.UnitFlags2 & ~(uint)UnitFlags2.Allowed);

        if (disallowedUnitFlags2 != 0)
        {
            Log.Logger.Verbose($"Table `creature_template` lists creature (Entry: {cInfo.Entry}) with disallowed `unit_flags2` {disallowedUnitFlags2}, removing incorrect Id.");
            cInfo.UnitFlags2 &= (uint)UnitFlags2.Allowed;
        }

        var disallowedUnitFlags3 = (cInfo.UnitFlags3 & ~(uint)UnitFlags3.Allowed);

        if (disallowedUnitFlags3 != 0)
        {
            Log.Logger.Verbose($"Table `creature_template` lists creature (Entry: {cInfo.Entry}) with disallowed `unit_flags2` {disallowedUnitFlags3}, removing incorrect Id.");
            cInfo.UnitFlags3 &= (uint)UnitFlags3.Allowed;
        }

        if (cInfo.DynamicFlags != 0)
        {
            Log.Logger.Verbose($"Table `creature_template` lists creature (Entry: {cInfo.Entry}) with `dynamicflags` > 0. Ignored and set to 0.");
            cInfo.DynamicFlags = 0;
        }

        var levels = cInfo.GetMinMaxLevel();

        if (levels[0] < 1 || levels[0] > SharedConst.StrongMaxLevel)
        {
            Log.Logger.Verbose($"Creature (ID: {cInfo.Entry}): Calculated minLevel {cInfo.Minlevel} is not within [1, 255], value has been set to {(cInfo.HealthScalingExpansion == (int)Expansion.LevelCurrent ? SharedConst.MaxLevel : 1)}.");
            cInfo.Minlevel = (short)(cInfo.HealthScalingExpansion == (int)Expansion.LevelCurrent ? 0 : 1);
        }

        if (levels[1] < 1 || levels[1] > SharedConst.StrongMaxLevel)
        {
            Log.Logger.Verbose($"Creature (ID: {cInfo.Entry}): Calculated maxLevel {cInfo.Maxlevel} is not within [1, 255], value has been set to {(cInfo.HealthScalingExpansion == (int)Expansion.LevelCurrent ? SharedConst.MaxLevel : 1)}.");
            cInfo.Maxlevel = (short)(cInfo.HealthScalingExpansion == (int)Expansion.LevelCurrent ? 0 : 1);
        }

        cInfo.ModDamage *= GetDamageMod(cInfo.Rank);

        float GetDamageMod(CreatureEliteType rank)
        {
            return rank switch // define rates for each elite rank
            {
                CreatureEliteType.Normal    => _configuration.GetDefaultValue("Rate.Creature.Normal.Damage", 1.0f),
                CreatureEliteType.Elite     => _configuration.GetDefaultValue("Rate.Creature.Elite.Elite.Damage", 1.0f),
                CreatureEliteType.RareElite => _configuration.GetDefaultValue("Rate.Creature.Elite.RAREELITE.Damage", 1.0f),
                CreatureEliteType.WorldBoss => _configuration.GetDefaultValue("Rate.Creature.Elite.WORLDBOSS.Damage", 1.0f),
                CreatureEliteType.Rare      => _configuration.GetDefaultValue("Rate.Creature.Elite.RARE.Damage", 1.0f),
                _                           => _configuration.GetDefaultValue("Rate.Creature.Elite.Elite.Damage", 1.0f)
            };
        }

        if (cInfo.GossipMenuId != 0 && !cInfo.Npcflag.HasAnyFlag((ulong)NPCFlags.Gossip))
            Log.Logger.Information($"Creature (Entry: {cInfo.Entry}) has assigned gossip menu {cInfo.GossipMenuId}, but npcflag does not include UNIT_NPC_FLAG_GOSSIP.");
        else if (cInfo.GossipMenuId == 0 && cInfo.Npcflag.HasAnyFlag((ulong)NPCFlags.Gossip))
            Log.Logger.Information($"Creature (Entry: {cInfo.Entry}) has npcflag UNIT_NPC_FLAG_GOSSIP, but gossip menu is unassigned.");
    }

    public void DeleteCreatureData(ulong spawnId)
    {
        var data = GetCreatureData(spawnId);

        if (data != null)
        {
            RemoveCreatureFromGrid(data);
            OnDeleteSpawnData(data);
        }

        _creatureDataStorage.Remove(spawnId);
    }

    public void DeleteGameObjectData(ulong spawnId)
    {
        var data = GetGameObjectData(spawnId);

        if (data != null)
        {
            RemoveGameObjectFromGrid(data);
            OnDeleteSpawnData(data);
        }

        _gameObjectDataStorage.Remove(spawnId);
    }

    public bool DeleteGameTele(string name)
    {
        name = name.ToLowerInvariant();

        foreach (var pair in GameTeleStorage.ToList())
            if (pair.Value.NameLow == name)
            {
                var stmt = _worldDatabase.GetPreparedStatement(WorldStatements.DEL_GAME_TELE);
                stmt.AddValue(0, pair.Value.Name);
                _worldDatabase.Execute(stmt);

                GameTeleStorage.Remove(pair.Key);

                return true;
            }

        return false;
    }

    public GraveYardData FindGraveYardData(uint id, uint zoneId)
    {
        var range = GraveYardStorage.LookupByKey(zoneId);

        foreach (var data in range)
            if (data.SafeLocId == id)
                return data;

        return null;
    }

    public uint GenerateAuctionID()
    {
        if (_auctionId >= 0xFFFFFFFE)
        {
            Log.Logger.Error("Auctions ids overflow!! Can't continue, shutting down server. ");
            Global.WorldMgr.StopNow();
        }

        return _auctionId++;
    }

    public ulong GenerateCreatureSpawnId()
    {
        if (_creatureSpawnId >= 0xFFFFFFFFFFFFFFFE)
        {
            Log.Logger.Fatal("Creature spawn id overflow!! Can't continue, shutting down server. ");
            Global.WorldMgr.StopNow();
        }

        return _creatureSpawnId++;
    }

    public ulong GenerateEquipmentSetGuid()
    {
        if (_equipmentSetGuid >= 0xFFFFFFFFFFFFFFFE)
        {
            Log.Logger.Error("EquipmentSet guid overflow!! Can't continue, shutting down server. ");
            Global.WorldMgr.StopNow();
        }

        return _equipmentSetGuid++;
    }

    public ulong GenerateGameObjectSpawnId()
    {
        if (_gameObjectSpawnId >= 0xFFFFFFFFFFFFFFFE)
        {
            Log.Logger.Fatal("GameObject spawn id overflow!! Can't continue, shutting down server. ");
            Global.WorldMgr.StopNow();
        }

        return _gameObjectSpawnId++;
    }

    public ulong GenerateMailID()
    {
        if (_mailId >= 0xFFFFFFFFFFFFFFFE)
        {
            Log.Logger.Error("Mail ids overflow!! Can't continue, shutting down server. ");
            Global.WorldMgr.StopNow();
        }

        return _mailId++;
    }

    public string GeneratePetName(uint entry)
    {
        var list0 = _petHalfName0[entry];
        var list1 = _petHalfName1[entry];

        if (list0.Empty() || list1.Empty())
        {
            var cinfo = GetCreatureTemplate(entry);

            if (cinfo == null)
                return "";

            var petname = Global.DB2Mgr.GetCreatureFamilyPetName(cinfo.Family, Global.WorldMgr.DefaultDbcLocale);

            if (!string.IsNullOrEmpty(petname))
                return petname;
            else
                return cinfo.Name;
        }

        return list0[RandomHelper.IRand(0, list0.Count - 1)] + list1[RandomHelper.IRand(0, list1.Count - 1)];
    }

    public uint GeneratePetNumber()
    {
        if (_hiPetNumber >= 0xFFFFFFFE)
        {
            Log.Logger.Error("_hiPetNumber Id overflow!! Can't continue, shutting down server. ");
            Global.WorldMgr.StopNow(ShutdownExitCode.Error);
        }

        return _hiPetNumber++;
    }

    public ulong GenerateVoidStorageItemId()
    {
        if (_voidItemId >= 0xFFFFFFFFFFFFFFFE)
        {
            Log.Logger.Error("_voidItemId overflow!! Can't continue, shutting down server. ");
            Global.WorldMgr.StopNow(ShutdownExitCode.Error);
        }

        return _voidItemId++;
    }

    public AccessRequirement GetAccessRequirement(uint mapid, Difficulty difficulty)
    {
        return _accessRequirementStorage.LookupByKey(MathFunctions.MakePair64(mapid, (uint)difficulty));
    }

    public Dictionary<ulong, CreatureData> GetAllCreatureData()
    {
        return _creatureDataStorage;
    }

    public List<string> GetAllDBScriptNames()
    {
        return _scriptNamesStorage.GetAllDBScriptNames();
    }

    public Dictionary<ulong, GameObjectData> GetAllGameObjectData()
    {
        return _gameObjectDataStorage;
    }

    public AreaTriggerStruct GetAreaTrigger(uint trigger)
    {
        return _areaTriggerStorage.LookupByKey(trigger);
    }

    public List<uint> GetAreaTriggerScriptIds(uint triggerid)
    {
        return _areaTriggerScriptStorage.LookupByKey(triggerid);
    }

    public uint GetBaseXP(uint level)
    {
        return _baseXPTable.ContainsKey(level) ? _baseXPTable[level] : 0;
    }

    public CellObjectGuids GetCellObjectGuids(uint mapid, Difficulty difficulty, uint cellid)
    {
        var key = (mapid, difficulty);

        if (_mapObjectGuidsStore.TryGetValue(key, out var internDict) && internDict.TryGetValue(cellid, out var val))
            return val;

        return null;
    }

    public CellObjectGuids GetCellPersonalObjectGuids(uint mapid, Difficulty spawnMode, uint phaseId, uint cell_id)
    {
        var guids = _mapPersonalObjectGuidsStore.LookupByKey((mapid, spawnMode, phaseId));

        return guids?.LookupByKey(cell_id);
    }

    public ClassAvailability GetClassExpansionRequirement(Race raceId, PlayerClass classId)
    {
        var raceClassAvailability = _classExpansionRequirementStorage.Find(raceClass => { return raceClass.RaceID == (byte)raceId; });

        var classAvailability = raceClassAvailability?.Classes.Find(availability => { return availability.ClassID == (byte)classId; });

        return classAvailability;
    }

    public ClassAvailability GetClassExpansionRequirementFallback(byte classId)
    {
        foreach (var raceClassAvailability in _classExpansionRequirementStorage)
            foreach (var classAvailability in raceClassAvailability.Classes)
                if (classAvailability.ClassID == classId)
                    return classAvailability;

        return null;
    }

    public List<RaceClassAvailability> GetClassExpansionRequirements()
    {
        return _classExpansionRequirementStorage;
    }

    public WorldSafeLocsEntry GetClosestGraveYard(WorldLocation location, TeamFaction team, WorldObject conditionObject)
    {
        var MapId = location.MapId;

        // search for zone associated closest graveyard
        var zoneId = Global.TerrainMgr.GetZoneId(conditionObject ? conditionObject.Location.PhaseShift : PhasingHandler.EmptyPhaseShift, MapId, location);

        if (zoneId == 0)
            if (location.Z > -500)
            {
                Log.Logger.Error("ZoneId not found for map {0} coords ({1}, {2}, {3})", MapId, location.X, location.Y, location.Z);

                return GetDefaultGraveYard(team);
            }

        // Simulate std. algorithm:
        //   found some graveyard associated to (ghost_zone, ghost_map)
        //
        //   if mapId == graveyard.mapId (ghost in plain zone or city or Battleground) and search graveyard at same map
        //     then check faction
        //   if mapId != graveyard.mapId (ghost in instance) and search any graveyard associated
        //     then check faction
        var range = GraveYardStorage.LookupByKey(zoneId);
        var mapEntry = _cliDB.MapStorage.LookupByKey(MapId);

        ConditionSourceInfo conditionSource = new(conditionObject);

        // not need to check validity of map object; MapId _MUST_ be valid here
        if (range.Empty() && !mapEntry.IsBattlegroundOrArena())
        {
            if (zoneId != 0) // zone == 0 can't be fixed, used by bliz for bugged zones
                Log.Logger.Error("Table `game_graveyard_zone` incomplete: Zone {0} Team {1} does not have a linked graveyard.", zoneId, team);

            return GetDefaultGraveYard(team);
        }

        // at corpse map
        var foundNear = false;
        float distNear = 10000;
        WorldSafeLocsEntry entryNear = null;

        // at entrance map for corpse map
        var foundEntr = false;
        float distEntr = 10000;
        WorldSafeLocsEntry entryEntr = null;

        // some where other
        WorldSafeLocsEntry entryFar = null;

        foreach (var data in range)
        {
            var entry = GetWorldSafeLoc(data.SafeLocId);

            if (entry == null)
            {
                Log.Logger.Error("Table `game_graveyard_zone` has record for not existing graveyard (WorldSafeLocs.dbc id) {0}, skipped.", data.SafeLocId);

                continue;
            }

            // skip enemy faction graveyard
            // team == 0 case can be at call from .neargrave
            if (data.Team != 0 && team != 0 && data.Team != (uint)team)
                continue;

            if (conditionObject)
            {
                if (!Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.Graveyard, data.SafeLocId, conditionSource))
                    continue;

                if (entry.Loc.MapId == mapEntry.ParentMapID && !conditionObject.Location.PhaseShift.HasVisibleMapId(entry.Loc.MapId))
                    continue;
            }

            // find now nearest graveyard at other map
            if (MapId != entry.Loc.MapId && entry.Loc.MapId != mapEntry.ParentMapID)
            {
                // if find graveyard at different map from where entrance placed (or no entrance data), use any first
                if (mapEntry == null || mapEntry.CorpseMapID < 0 || mapEntry.CorpseMapID != entry.Loc.MapId || mapEntry.Corpse is { X: 0, Y: 0 })
                {
                    // not have any corrdinates for check distance anyway
                    entryFar = entry;

                    continue;
                }

                // at entrance map calculate distance (2D);
                var dist2 = (entry.Loc.X - mapEntry.Corpse.X) * (entry.Loc.X - mapEntry.Corpse.X) + (entry.Loc.Y - mapEntry.Corpse.Y) * (entry.Loc.Y - mapEntry.Corpse.Y);

                if (foundEntr)
                {
                    if (dist2 < distEntr)
                    {
                        distEntr = dist2;
                        entryEntr = entry;
                    }
                }
                else
                {
                    foundEntr = true;
                    distEntr = dist2;
                    entryEntr = entry;
                }
            }
            // find now nearest graveyard at same map
            else
            {
                var dist2 = (entry.Loc.X - location.X) * (entry.Loc.X - location.X) + (entry.Loc.Y - location.Y) * (entry.Loc.Y - location.Y) + (entry.Loc.Z - location.Z) * (entry.Loc.Z - location.Z);

                if (foundNear)
                {
                    if (dist2 < distNear)
                    {
                        distNear = dist2;
                        entryNear = entry;
                    }
                }
                else
                {
                    foundNear = true;
                    distNear = dist2;
                    entryNear = entry;
                }
            }
        }

        if (entryNear != null)
            return entryNear;

        if (entryEntr != null)
            return entryEntr;

        return entryFar;
    }

    public CreatureAddon GetCreatureAddon(ulong lowguid)
    {
        return _creatureAddonStorage.LookupByKey(lowguid);
    }

    public CreatureBaseStats GetCreatureBaseStats(uint level, uint unitClass)
    {
        var stats = _creatureBaseStatsStorage.LookupByKey(MathFunctions.MakePair16(level, unitClass));

        if (stats != null)
            return stats;

        return new DefaultCreatureBaseStats();
    }

    public CreatureData GetCreatureData(ulong spawnId)
    {
        return _creatureDataStorage.LookupByKey(spawnId);
    }

    public uint GetCreatureDefaultTrainer(uint creatureId)
    {
        return GetCreatureTrainerForGossipOption(creatureId, 0, 0);
    }

    public CreatureLocale GetCreatureLocale(uint entry)
    {
        return _creatureLocaleStorage.LookupByKey(entry);
    }

    public CreatureModelInfo GetCreatureModelInfo(uint modelId)
    {
        return _creatureModelStorage.LookupByKey(modelId);
    }

    public CreatureModelInfo GetCreatureModelRandomGender(ref CreatureModel model, CreatureTemplate creatureTemplate)
    {
        var modelInfo = GetCreatureModelInfo(model.CreatureDisplayId);

        if (modelInfo == null)
            return null;

        // If a model for another gender exists, 50% chance to use it
        if (modelInfo.DisplayIdOtherGender != 0 && RandomHelper.URand(0, 1) == 0)
        {
            var minfotmp = GetCreatureModelInfo(modelInfo.DisplayIdOtherGender);

            if (minfotmp == null)
            {
                Log.Logger.Error($"Model (Entry: {model.CreatureDisplayId}) has modelidothergender {modelInfo.DisplayIdOtherGender} not found in table `creaturemodelinfo`. ");
            }
            else
            {
                // DisplayID changed
                model.CreatureDisplayId = modelInfo.DisplayIdOtherGender;

                var creatureModel = creatureTemplate?.Models.Find(templateModel => { return templateModel.CreatureDisplayId == modelInfo.DisplayIdOtherGender; });

                if (creatureModel != null)
                    model = creatureModel;

                return minfotmp;
            }
        }

        return modelInfo;
    }

    public CreatureMovementData GetCreatureMovementOverride(ulong spawnId)
    {
        return _creatureMovementOverrides.LookupByKey(spawnId);
    }

    public List<uint> GetCreatureQuestInvolvedRelationReverseBounds(uint questId)
    {
        return _creatureQuestInvolvedRelationsReverse.LookupByKey(questId);
    }

    public QuestRelationResult GetCreatureQuestInvolvedRelations(uint entry)
    {
        return GetQuestRelationsFrom(_creatureQuestInvolvedRelations, entry, false);
    }

    public List<uint> GetCreatureQuestItemList(uint id)
    {
        return _creatureQuestItemStorage.LookupByKey(id);
    }

    public MultiMap<uint, uint> GetCreatureQuestRelationMapHACK()
    {
        return _creatureQuestRelations;
    }

    public QuestRelationResult GetCreatureQuestRelations(uint entry)
    {
        return GetQuestRelationsFrom(_creatureQuestRelations, entry, true);
    }

    public CreatureSummonedData GetCreatureSummonedData(uint entryId)
    {
        return _creatureSummonedDataStorage.LookupByKey(entryId);
    }

    public CreatureTemplate GetCreatureTemplate(uint entry)
    {
        return _creatureTemplateStorage.LookupByKey(entry);
    }

    public CreatureAddon GetCreatureTemplateAddon(uint entry)
    {
        return _creatureTemplateAddonStorage.LookupByKey(entry);
    }

    public Dictionary<uint, CreatureTemplate> GetCreatureTemplates()
    {
        return _creatureTemplateStorage;
    }

    public uint GetCreatureTrainerForGossipOption(uint creatureId, uint gossipMenuId, uint gossipOptionIndex)
    {
        return _creatureDefaultTrainers.LookupByKey((creatureId, gossipMenuId, gossipOptionIndex));
    }

    public string GetCypherString(uint entry, Locale locale = Locale.enUS)
    {
        if (!_cypherStringStorage.ContainsKey(entry))
        {
            Log.Logger.Error("Cypher string entry {0} not found in DB.", entry);

            return "<Error>";
        }

        var cs = _cypherStringStorage[entry];

        if (cs.Length > (int)locale && !string.IsNullOrEmpty(cs[(int)locale]))
            return cs[(int)locale];

        return cs[(int)SharedConst.DefaultLocale];
    }

    public string GetCypherString(CypherStrings cmd, Locale locale = Locale.enUS)
    {
        return GetCypherString((uint)cmd, locale);
    }

    public WorldSafeLocsEntry GetDefaultGraveYard(TeamFaction team)
    {
        if (team == TeamFaction.Horde)
            return GetWorldSafeLoc(10);
        else if (team == TeamFaction.Alliance)
            return GetWorldSafeLoc(4);
        else return null;
    }

    public SpawnGroupTemplateData GetDefaultSpawnGroup()
    {
        if (!_spawnGroupDataStorage.TryGetValue(0, out var gt))
            gt = _spawnGroupDataStorage.ElementAt(0).Value;

        return gt;
    }

    public List<DungeonEncounter> GetDungeonEncounterList(uint mapId, Difficulty difficulty)
    {
        return _dungeonEncounterStorage.LookupByKey(MathFunctions.MakePair64(mapId, (uint)difficulty));
    }

    public EquipmentInfo GetEquipmentInfo(uint entry, int id)
    {
        var equip = _equipmentInfoStorage.LookupByKey(entry);

        if (equip.Empty())
            return null;

        if (id == -1)
            return equip[RandomHelper.IRand(0, equip.Count - 1)].Item2;
        else
            return equip.Find(p => p.Item1 == id)?.Item2;
    }

    public List<uint> GetExclusiveQuestGroupBounds(int exclusiveGroupId)
    {
        return _exclusiveQuestGroups.LookupByKey(exclusiveGroupId);
    }

    public int GetFishingBaseSkillLevel(uint entry)
    {
        return _fishingBaseForAreaStorage.LookupByKey(entry);
    }

    public GameObjectAddon GetGameObjectAddon(ulong lowguid)
    {
        return _gameObjectAddonStorage.LookupByKey(lowguid);
    }

    public GameObjectData GetGameObjectData(ulong spawnId)
    {
        return _gameObjectDataStorage.LookupByKey(spawnId);
    }

    public GameObjectLocale GetGameObjectLocale(uint entry)
    {
        return _gameObjectLocaleStorage.LookupByKey(entry);
    }

    public GameObjectOverride GetGameObjectOverride(ulong spawnId)
    {
        return _gameObjectOverrideStorage.LookupByKey(spawnId);
    }

    public List<uint> GetGameObjectQuestItemList(uint id)
    {
        return _gameObjectQuestItemStorage.LookupByKey(id);
    }

    public GameObjectTemplate GetGameObjectTemplate(uint entry)
    {
        return _gameObjectTemplateStorage.LookupByKey(entry);
    }

    public GameObjectTemplateAddon GetGameObjectTemplateAddon(uint entry)
    {
        return _gameObjectTemplateAddonStorage.LookupByKey(entry);
    }

    public Dictionary<uint, GameObjectTemplate> GetGameObjectTemplates()
    {
        return _gameObjectTemplateStorage;
    }

    public GameTele GetGameTele(uint id)
    {
        return GameTeleStorage.LookupByKey(id);
    }

    public GameTele GetGameTele(string name)
    {
        name = name.ToLower();

        // Alternative first GameTele what contains wnameLow as substring in case no GameTele location found
        GameTele alt = null;

        foreach (var (_, tele) in GameTeleStorage)
            if (tele.NameLow == name)
                return tele;
            else if (alt == null && tele.NameLow.Contains(name))
                alt = tele;

        return alt;
    }

    public GameTele GetGameTeleExactName(string name)
    {
        name = name.ToLower();

        foreach (var (_, tele) in GameTeleStorage)
            if (tele.NameLow == name)
                return tele;

        return null;
    }

    public ObjectGuidGenerator GetGenerator(HighGuid high)
    {
        return GetGuidSequenceGenerator(high);
    }

    public AreaTriggerStruct GetGoBackTrigger(uint Map)
    {
        uint? parentId = null;
        var mapEntry = _cliDB.MapStorage.LookupByKey(Map);

        if (mapEntry == null || mapEntry.CorpseMapID < 0)
            return null;

        if (mapEntry.IsDungeon())
        {
            var iTemplate = GetInstanceTemplate(Map);

            if (iTemplate != null)
                parentId = iTemplate.Parent;
        }

        var entrance_map = parentId.GetValueOrDefault((uint)mapEntry.CorpseMapID);

        foreach (var pair in _areaTriggerStorage)
            if (pair.Value.TargetMapId == entrance_map)
            {
                var atEntry = _cliDB.AreaTriggerStorage.LookupByKey(pair.Key);

                if (atEntry != null && atEntry.ContinentID == Map)
                    return pair.Value;
            }

        return null;
    }

    public List<uint> GetGOQuestInvolvedRelationReverseBounds(uint questId)
    {
        return _goQuestInvolvedRelationsReverse.LookupByKey(questId);
    }

    public QuestRelationResult GetGOQuestInvolvedRelations(uint entry)
    {
        return GetQuestRelationsFrom(_goQuestInvolvedRelations, entry, false);
    }

    public MultiMap<uint, uint> GetGOQuestRelationMapHACK()
    {
        return _goQuestRelations;
    }

    public QuestRelationResult GetGOQuestRelations(uint entry)
    {
        return GetQuestRelationsFrom(_goQuestRelations, entry, true);
    }

    public GossipMenuAddon GetGossipMenuAddon(uint menuId)
    {
        return _gossipMenuAddonStorage.LookupByKey(menuId);
    }

    public GossipMenuItemsLocale GetGossipMenuItemsLocale(uint menuId, uint optionIndex)
    {
        return _gossipMenuItemsLocaleStorage.LookupByKey(Tuple.Create(menuId, optionIndex));
    }

    public List<GossipMenuItems> GetGossipMenuItemsMapBounds(uint uiMenuId)
    {
        return _gossipMenuItemsStorage.LookupByKey(uiMenuId);
    }

    public List<GossipMenus> GetGossipMenusMapBounds(uint uiMenuId)
    {
        return _gossipMenusStorage.LookupByKey(uiMenuId);
    }

    public List<InstanceSpawnGroupInfo> GetInstanceSpawnGroupsForMap(uint mapId)
    {
        return _instanceSpawnGroupStorage.LookupByKey(mapId);
    }

    public InstanceTemplate GetInstanceTemplate(uint mapID)
    {
        return _instanceTemplateStorage.LookupByKey(mapID);
    }

    public Dictionary<uint, InstanceTemplate> GetInstanceTemplates()
    {
        return _instanceTemplateStorage;
    }

    public ItemTemplate GetItemTemplate(uint ItemId)
    {
        return _itemTemplateStorage.LookupByKey(ItemId);
    }

    public Dictionary<uint, ItemTemplate> GetItemTemplates()
    {
        return _itemTemplateStorage;
    }

    public JumpChargeParams GetJumpChargeParams(int id)
    {
        return _jumpChargeParams.LookupByKey(id);
    }

    public SpawnGroupTemplateData GetLegacySpawnGroup()
    {
        if (!_spawnGroupDataStorage.TryGetValue(1, out var gt))
            gt = _spawnGroupDataStorage.ElementAt(1).Value;

        return gt;
    }

    public ObjectGuid GetLinkedRespawnGuid(ObjectGuid spawnId)
    {
        var retGuid = _linkedRespawnStorage.LookupByKey(spawnId);

        if (retGuid.IsEmpty)
            return ObjectGuid.Empty;

        return retGuid;
    }

    public MailLevelReward GetMailLevelReward(uint level, ulong raceMask)
    {
        var mailList = _mailLevelRewardStorage.LookupByKey((byte)level);

        if (mailList.Empty())
            return null;

        foreach (var mailReward in mailList)
            if (Convert.ToBoolean(mailReward.RaceMask & raceMask))
                return mailReward;

        return null;
    }

    public AreaTriggerStruct GetMapEntranceTrigger(uint Map)
    {
        foreach (var pair in _areaTriggerStorage)
            if (pair.Value.TargetMapId == Map)
            {
                var atEntry = _cliDB.AreaTriggerStorage.LookupByKey(pair.Key);

                if (atEntry != null)
                    return pair.Value;
            }

        return null;
    }

    public Dictionary<uint, CellObjectGuids> GetMapObjectGuids(uint mapid, Difficulty difficulty)
    {
        var key = (mapid, difficulty);

        return _mapObjectGuidsStore.LookupByKey(key);
    }

    public uint GetMaxLevelForExpansion(Expansion expansion)
    {
        switch (expansion)
        {
            case Expansion.Classic:
                return _configuration.GetDefaultValue<uint>("Character.MaxLevelFor.Classic", 30);
            case Expansion.BurningCrusade:
                return _configuration.GetDefaultValue<uint>("Character.MaxLevelFor.BurningCrusade", 30);
            case Expansion.WrathOfTheLichKing:
                return _configuration.GetDefaultValue<uint>("Character.MaxLevelFor.WrathOfTheLichKing", 30);
            case Expansion.Cataclysm:
                return _configuration.GetDefaultValue<uint>("Character.MaxLevelFor.Cataclysm", 35);
            case Expansion.MistsOfPandaria:
                return _configuration.GetDefaultValue<uint>("Character.MaxLevelFor.MistsOfPandaria", 35);
            case Expansion.WarlordsOfDraenor:
                return _configuration.GetDefaultValue<uint>("Character.MaxLevelFor.WarlordsOfDraenor", 40);
            case Expansion.Legion:
                return _configuration.GetDefaultValue<uint>("Character.MaxLevelFor.Legion", 45);
            case Expansion.BattleForAzeroth:
                return _configuration.GetDefaultValue<uint>("Character.MaxLevelFor.BattleForAzeroth", 50);
            case Expansion.ShadowLands:
                return _configuration.GetDefaultValue<uint>("Character.MaxLevelFor.ShadowLands", 60);
            case Expansion.Dragonflight:
                return _configuration.GetDefaultValue<uint>("Character.MaxLevelFor.Dragonflight", 70);
            default:
                break;
        }

        return 0;
    }

    public uint GetNearestTaxiNode(float x, float y, float z, uint mapid, TeamFaction team)
    {
        var found = false;
        float dist = 10000;
        uint id = 0;

        var requireFlag = (team == TeamFaction.Alliance) ? TaxiNodeFlags.Alliance : TaxiNodeFlags.Horde;

        foreach (var node in _cliDB.TaxiNodesStorage.Values)
        {
            var i = node.Id;

            if (node.ContinentID != mapid || !node.Flags.HasAnyFlag(requireFlag))
                continue;

            var field = (i - 1) / 8;
            var submask = (byte)(1 << (int)((i - 1) % 8));

            // skip not taxi network nodes
            if ((_cliDB.TaxiNodesMask[field] & submask) == 0)
                continue;

            var dist2 = (node.Pos.X - x) * (node.Pos.X - x) + (node.Pos.Y - y) * (node.Pos.Y - y) + (node.Pos.Z - z) * (node.Pos.Z - z);

            if (found)
            {
                if (dist2 < dist)
                {
                    dist = dist2;
                    id = i;
                }
            }
            else
            {
                found = true;
                dist = dist2;
                id = i;
            }
        }

        return id;
    }

    public NpcText GetNpcText(uint textId)
    {
        return _npcTextStorage.LookupByKey(textId);
    }

    public VendorItemData GetNpcVendorItemList(uint entry)
    {
        return _cacheVendorItemStorage.LookupByKey(entry);
    }

    public PageText GetPageText(uint pageEntry)
    {
        return _pageTextStorage.LookupByKey(pageEntry);
    }

    public PageTextLocale GetPageTextLocale(uint entry)
    {
        return _pageTextLocaleStorage.LookupByKey(entry);
    }

    public PetLevelInfo GetPetLevelInfo(uint creatureid, uint level)
    {
        if (level > _configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel))
            level = _configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel);

        var petinfo = _petInfoStore.LookupByKey(creatureid);

        return petinfo?[level - 1]; // data for level 1 stored in [0] array element, ...
    }

    public PhaseInfoStruct GetPhaseInfo(uint phaseId)
    {
        return _phaseInfoById.LookupByKey(phaseId);
    }

    public string GetPhaseName(uint phaseId)
    {
        return _phaseNameStorage.TryGetValue(phaseId, out var value) ? value : "Unknown Name";
    }

    public List<PhaseAreaInfo> GetPhasesForArea(uint areaId)
    {
        return _phaseInfoByArea.LookupByKey(areaId);
    }

    public PlayerChoice GetPlayerChoice(int choiceId)
    {
        return _playerChoices.LookupByKey((uint)choiceId);
    }

    public PlayerChoiceLocale GetPlayerChoiceLocale(int ChoiceID)
    {
        return _playerChoiceLocales.LookupByKey((uint)ChoiceID);
    }

    public void GetPlayerClassLevelInfo(PlayerClass _class, uint level, out uint baseMana)
    {
        baseMana = 0;

        if (level < 1 || _class >= PlayerClass.Max)
            return;

        if (level > _configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel))
            level = (byte)_configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel);

        var mp = _cliDB.BaseMPGameTable.GetRow(level);

        if (mp == null)
        {
            Log.Logger.Error("Tried to get non-existant Class-Level combination data for base mp. Class {0} Level {1}", _class, level);

            return;
        }

        baseMana = (uint)_cliDB.GetGameTableColumnForClass(mp, _class);
    }

    public PlayerInfo GetPlayerInfo(Race raceId, PlayerClass classId)
    {
        if (raceId >= Race.Max)
            return null;

        if (classId >= PlayerClass.Max)
            return null;

        if (!PlayerInfos.TryGetValue(raceId, classId, out var info))
            return null;

        return info;
    }

    public PlayerLevelInfo GetPlayerLevelInfo(Race race, PlayerClass _class, uint level)
    {
        if (level < 1 || race >= Race.Max || _class >= PlayerClass.Max)
            return null;

        if (!PlayerInfos.TryGetValue(race, _class, out var pInfo))
            return null;

        if (level <= _configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel))
            return pInfo.LevelInfo[level - 1];
        else
            return BuildPlayerLevelInfo(race, _class, level);
    }

    public PointOfInterest GetPointOfInterest(uint id)
    {
        return _pointsOfInterestStorage.LookupByKey(id);
    }

    public PointOfInterestLocale GetPointOfInterestLocale(uint id)
    {
        return _pointOfInterestLocaleStorage.LookupByKey(id);
    }

    public QuestGreeting GetQuestGreeting(TypeId type, uint id)
    {
        byte typeIndex;

        if (type == TypeId.Unit)
            typeIndex = 0;
        else if (type == TypeId.GameObject)
            typeIndex = 1;
        else
            return null;

        return _questGreetingStorage[typeIndex].LookupByKey(id);
    }

    public QuestGreetingLocale GetQuestGreetingLocale(TypeId type, uint id)
    {
        byte typeIndex;

        if (type == TypeId.Unit)
            typeIndex = 0;
        else if (type == TypeId.GameObject)
            typeIndex = 1;
        else
            return null;

        return _questGreetingLocaleStorage[typeIndex].LookupByKey(id);
    }

    public QuestTemplateLocale GetQuestLocale(uint entry)
    {
        return _questTemplateLocaleStorage.LookupByKey(entry);
    }

    public QuestObjective GetQuestObjective(uint questObjectiveId)
    {
        return _questObjectives.LookupByKey(questObjectiveId);
    }

    public QuestObjectivesLocale GetQuestObjectivesLocale(uint entry)
    {
        return _questObjectivesLocaleStorage.LookupByKey(entry);
    }

    public QuestOfferRewardLocale GetQuestOfferRewardLocale(uint entry)
    {
        return _questOfferRewardLocaleStorage.LookupByKey(entry);
    }

    public QuestPOIData GetQuestPOIData(uint questId)
    {
        return _questPOIStorage.LookupByKey(questId);
    }

    public QuestRequestItemsLocale GetQuestRequestItemsLocale(uint entry)
    {
        return _questRequestItemsLocaleStorage.LookupByKey(entry);
    }

    public List<uint> GetQuestsForAreaTrigger(uint triggerId)
    {
        return _questAreaTriggerStorage.LookupByKey(triggerId);
    }

    public Quest.Quest GetQuestTemplate(uint questId)
    {
        return _questTemplates.LookupByKey(questId);
    }

    public Dictionary<uint, Quest.Quest> GetQuestTemplates()
    {
        return _questTemplates;
    }

    public List<Quest.Quest> GetQuestTemplatesAutoPush()
    {
        return _questTemplatesAutoPush;
    }

    public RaceUnlockRequirement GetRaceUnlockRequirement(Race race)
    {
        return _raceUnlockRequirementStorage.LookupByKey((byte)race);
    }

    public Dictionary<byte, RaceUnlockRequirement> GetRaceUnlockRequirements()
    {
        return _raceUnlockRequirementStorage;
    }

    public string GetRealmName(uint realm)
    {
        return _realmNameStorage.LookupByKey(realm);
    }

    public bool GetRealmName(uint realmId, ref string name, ref string normalizedName)
    {
        var realmName = _realmNameStorage.LookupByKey(realmId);

        if (realmName != null)
        {
            name = realmName;
            normalizedName = realmName.Normalize();

            return true;
        }

        return false;
    }

    public RepRewardRate GetRepRewardRate(uint factionId)
    {
        return _repRewardRateStorage.LookupByKey(factionId);
    }

    public RepSpilloverTemplate GetRepSpillover(uint factionId)
    {
        return _repSpilloverTemplateStorage.LookupByKey(factionId);
    }

    public ReputationOnKillEntry GetReputationOnKilEntry(uint id)
    {
        return _repOnKillStorage.LookupByKey(id);
    }

    public SceneTemplate GetSceneTemplate(uint sceneId)
    {
        return _sceneTemplateStorage.LookupByKey(sceneId);
    }

    public uint GetScriptId(string name, bool isDatabaseBound = true)
    {
        if (string.IsNullOrEmpty(name))
            return 0;

        return _scriptNamesStorage.Insert(name, isDatabaseBound);
    }

    public string GetScriptName(uint id)
    {
        var entry = _scriptNamesStorage.Find(id);

        if (entry != null)
            return entry.Name;

        return "";
    }

    public Dictionary<uint, MultiMap<uint, ScriptInfo>> GetScriptsMapByType(ScriptsType type)
    {
        return type switch
        {
            ScriptsType.Spell    => SpellScripts,
            ScriptsType.Event    => EventScripts,
            ScriptsType.Waypoint => WaypointScripts,
            _                    => null
        };
    }

    public string GetScriptsTableNameByType(ScriptsType type)
    {
        return type switch
        {
            ScriptsType.Spell    => "spell_scripts",
            ScriptsType.Event    => "event_scripts",
            ScriptsType.Waypoint => "waypoint_scripts",
            _                    => ""
        };
    }

    public SkillTiersEntry GetSkillTier(uint skillTierId)
    {
        return _skillTiers.LookupByKey(skillTierId);
    }

    public SpawnData GetSpawnData(SpawnObjectType type, ulong spawnId)
    {
        if (!SpawnMetadata.TypeHasData(type))
            return null;

        return type switch
        {
            SpawnObjectType.Creature    => GetCreatureData(spawnId),
            SpawnObjectType.GameObject  => GetGameObjectData(spawnId),
            SpawnObjectType.AreaTrigger => Global.AreaTriggerDataStorage.GetAreaTriggerSpawn(spawnId),
            _                           => null
        };
    }

    public SpawnGroupTemplateData GetSpawnGroupData(uint groupId)
    {
        return _spawnGroupDataStorage.LookupByKey(groupId);
    }

    public SpawnGroupTemplateData GetSpawnGroupData(SpawnObjectType type, ulong spawnId)
    {
        var data = GetSpawnMetadata(type, spawnId);

        return data?.SpawnGroupData;
    }

    public List<uint> GetSpawnGroupsForMap(uint mapId)
    {
        return _spawnGroupsByMap.LookupByKey(mapId);
    }

    public SpawnMetadata GetSpawnMetadata(SpawnObjectType type, ulong spawnId)
    {
        if (SpawnMetadata.TypeHasData(type))
            return GetSpawnData(type, spawnId);
        else
            return null;
    }

    public List<SpawnMetadata> GetSpawnMetadataForGroup(uint groupId)
    {
        return _spawnGroupMapStorage.LookupByKey(groupId);
    }

    public List<SpellClickInfo> GetSpellClickInfoMapBounds(uint creature_id)
    {
        return _spellClickInfoStorage.LookupByKey(creature_id);
    }

    public List<uint> GetSpellScriptsBounds(uint spellId)
    {
        return _spellScriptsStorage.LookupByKey(spellId);
    }

    public List<TempSummonData> GetSummonGroup(uint summonerId, SummonerType summonerType, byte group)
    {
        var key = Tuple.Create(summonerId, summonerType, group);

        return _tempSummonDataStorage.LookupByKey(key);
    }

    public uint GetTaxiMountDisplayId(uint id, TeamFaction team, bool allowed_alt_team = false)
    {
        CreatureModel mountModel = new();
        CreatureTemplate mount_info = null;

        // select mount creature id
        var node = _cliDB.TaxiNodesStorage.LookupByKey(id);

        if (node != null)
        {
            uint mount_entry;

            if (team == TeamFaction.Alliance)
                mount_entry = node.MountCreatureID[1];
            else
                mount_entry = node.MountCreatureID[0];

            // Fix for Alliance not being able to use Acherus taxi
            // only one mount type for both sides
            if (mount_entry == 0 && allowed_alt_team)
                // Simply reverse the selection. At least one team in theory should have a valid mount ID to choose.
                mount_entry = team == TeamFaction.Alliance ? node.MountCreatureID[0] : node.MountCreatureID[1];

            mount_info = GetCreatureTemplate(mount_entry);

            if (mount_info != null)
            {
                var model = mount_info.GetRandomValidModel();

                if (model == null)
                {
                    Log.Logger.Error($"No displayid found for the taxi mount with the entry {mount_entry}! Can't load it!");

                    return 0;
                }

                mountModel = model;
            }
        }

        // minfo is not actually used but the mount_id was updated
        GetCreatureModelRandomGender(ref mountModel, mount_info);

        return mountModel.CreatureDisplayId;
    }

    public void GetTaxiPath(uint source, uint destination, out uint path, out uint cost)
    {
        var pathSet = _cliDB.TaxiPathSetBySource.LookupByKey(source);

        if (pathSet == null)
        {
            path = 0;
            cost = 0;

            return;
        }

        var dest_i = pathSet.LookupByKey(destination);

        if (dest_i == null)
        {
            path = 0;
            cost = 0;

            return;
        }

        cost = dest_i.price;
        path = dest_i.Id;
    }

    public TerrainSwapInfo GetTerrainSwapInfo(uint terrainSwapId)
    {
        return _terrainSwapInfoById.LookupByKey(terrainSwapId);
    }

    public MultiMap<uint, TerrainSwapInfo> GetTerrainSwaps()
    {
        return _terrainSwapInfoByMap;
    }

    public Trainer GetTrainer(uint trainerId)
    {
        return _trainers.LookupByKey(trainerId);
    }

    public List<VehicleAccessory> GetVehicleAccessoryList(Vehicle veh)
    {
        var cre = veh.GetBase().AsCreature;

        if (cre != null)
        {
            // Give preference to GUID-based accessories
            var list = _vehicleAccessoryStore.LookupByKey(cre.SpawnId);

            if (!list.Empty())
                return list;
        }

        // Otherwise return entry-based
        return _vehicleTemplateAccessoryStore.LookupByKey(veh.GetCreatureEntry());
    }

    public VehicleSeatAddon GetVehicleSeatAddon(uint seatId)
    {
        return _vehicleSeatAddonStore.LookupByKey(seatId);
    }

    public VehicleTemplate GetVehicleTemplate(Vehicle veh)
    {
        return _vehicleTemplateStore.LookupByKey(veh.GetCreatureEntry());
    }

    public WorldSafeLocsEntry GetWorldSafeLoc(uint id)
    {
        return _worldSafeLocs.LookupByKey(id);
    }

    public Dictionary<uint, WorldSafeLocsEntry> GetWorldSafeLocs()
    {
        return _worldSafeLocs;
    }

    public uint GetXPForLevel(uint level)
    {
        if (level < _playerXPperLevel.Length)
            return _playerXPperLevel[level];

        return 0;
    }

    public bool HasPersonalSpawns(uint mapid, Difficulty spawnMode, uint phaseId)
    {
        return _mapPersonalObjectGuidsStore.ContainsKey((mapid, spawnMode, phaseId));
    }

    public void InitializeQueriesData(QueryDataGroup mask)
    {
        var oldMSTime = Time.MSTime;

        // cache disabled
        if (!_configuration.GetDefaultValue("CacheDataQueries", true))
        {
            Log.Logger.Information("Query data caching is disabled. Skipped initialization.");

            return;
        }

        // Initialize Query data for creatures
        if (mask.HasAnyFlag(QueryDataGroup.Creatures))
            foreach (var creaturePair in _creatureTemplateStorage)
                creaturePair.Value.InitializeQueryData();

        // Initialize Query Data for gameobjects
        if (mask.HasAnyFlag(QueryDataGroup.Gameobjects))
            foreach (var gameobjectPair in _gameObjectTemplateStorage)
                gameobjectPair.Value.InitializeQueryData();

        // Initialize Query Data for quests
        if (mask.HasAnyFlag(QueryDataGroup.Quests))
            foreach (var questPair in _questTemplates)
                questPair.Value.InitializeQueryData();

        // Initialize QuestId POI data
        if (mask.HasAnyFlag(QueryDataGroup.POIs))
            foreach (var poiPair in _questPOIStorage)
                poiPair.Value.InitializeQueryData();

        Log.Logger.Information($"Initialized query cache data in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    public bool IsGameObjectForQuests(uint entry)
    {
        return _gameObjectForQuestStorage.Contains(entry);
    }

    public bool IsReservedName(string name)
    {
        return _reservedNamesStorage.Contains(name.ToLower());
    }

    public bool IsTavernAreaTrigger(uint Trigger_ID)
    {
        return _tavernAreaTriggerStorage.Contains(Trigger_ID);
    }

    public bool IsTransportMap(uint mapId)
    {
        return _transportMaps.Contains((ushort)mapId);
    }

    public bool IsVendorItemValid(uint vendorentry, VendorItem vItem, Player player = null, List<uint> skipvendors = null, ulong ORnpcflag = 0)
    {
        var cInfo = GetCreatureTemplate(vendorentry);

        if (cInfo == null)
        {
            if (player != null)
                player.SendSysMessage(CypherStrings.CommandVendorselection);
            else if (_configuration.GetDefaultValue("load.autoclean", false))
                _worldDatabase.Execute($"DELETE FROM npc_vendor WHERE entry = {vendorentry}");
            else
                Log.Logger.Error("Table `(gameevent)npcvendor` have data for not existed creature template (Entry: {0}), ignore", vendorentry);

            return false;
        }

        if (!Convert.ToBoolean(((ulong)cInfo.Npcflag | ORnpcflag) & (ulong)NPCFlags.Vendor))
        {
            if (skipvendors == null || skipvendors.Count == 0)
            {
                if (player != null)
                    player.SendSysMessage(CypherStrings.CommandVendorselection);
                else if (_configuration.GetDefaultValue("load.autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM npc_vendor WHERE entry = {vendorentry}");
                else
                    Log.Logger.Error("Table `(gameevent)npcvendor` have data for not creature template (Entry: {0}) without vendor Id, ignore", vendorentry);

                skipvendors?.Add(vendorentry);
            }

            return false;
        }

        if ((vItem.Type == ItemVendorType.Item && GetItemTemplate(vItem.Item) == null) ||
            (vItem.Type == ItemVendorType.Currency && _cliDB.CurrencyTypesStorage.LookupByKey(vItem.Item) == null))
        {
            if (player != null)
                player.SendSysMessage(CypherStrings.ItemNotFound, vItem.Item, vItem.Type);
            else
                Log.Logger.Error("Table `(gameevent)npcvendor` for Vendor (Entry: {0}) have in item list non-existed item ({1}, type {2}), ignore", vendorentry, vItem.Item, vItem.Type);

            return false;
        }

        if (vItem.PlayerConditionId != 0 && !_cliDB.PlayerConditionStorage.ContainsKey(vItem.PlayerConditionId))
        {
            Log.Logger.Error("Table `(game_event_)npc_vendor` has Item (Entry: {0}) with invalid PlayerConditionId ({1}) for vendor ({2}), ignore", vItem.Item, vItem.PlayerConditionId, vendorentry);

            return false;
        }

        if (vItem.ExtendedCost != 0 && !_cliDB.ItemExtendedCostStorage.ContainsKey(vItem.ExtendedCost))
        {
            if (player != null)
                player.SendSysMessage(CypherStrings.ExtendedCostNotExist, vItem.ExtendedCost);
            else
                Log.Logger.Error("Table `(gameevent)npcvendor` have Item (Entry: {0}) with wrong ExtendedCost ({1}) for vendor ({2}), ignore", vItem.Item, vItem.ExtendedCost, vendorentry);

            return false;
        }

        if (vItem.Type == ItemVendorType.Item) // not applicable to currencies
        {
            if (vItem.Maxcount > 0 && vItem.Incrtime == 0)
            {
                if (player != null)
                    player.SendSysMessage("MaxCount != 0 ({0}) but IncrTime == 0", vItem.Maxcount);
                else
                    Log.Logger.Error("Table `(gameevent)npcvendor` has `maxcount` ({0}) for item {1} of vendor (Entry: {2}) but `incrtime`=0, ignore", vItem.Maxcount, vItem.Item, vendorentry);

                return false;
            }
            else if (vItem.Maxcount == 0 && vItem.Incrtime > 0)
            {
                if (player != null)
                    player.SendSysMessage("MaxCount == 0 but IncrTime<>= 0");
                else
                    Log.Logger.Error("Table `(gameevent)npcvendor` has `maxcount`=0 for item {0} of vendor (Entry: {0}) but `incrtime`<>0, ignore", vItem.Item, vendorentry);

                return false;
            }

            foreach (var bonusList in vItem.BonusListIDs)
                if (Global.DB2Mgr.GetItemBonusList(bonusList) == null)
                {
                    Log.Logger.Error("Table `(game_event_)npc_vendor` have Item (Entry: {0}) with invalid bonus {1} for vendor ({2}), ignore", vItem.Item, bonusList, vendorentry);

                    return false;
                }
        }

        var vItems = GetNpcVendorItemList(vendorentry);

        if (vItems == null)
            return true; // later checks for non-empty lists

        if (vItems.FindItemCostPair(vItem.Item, vItem.ExtendedCost, vItem.Type) != null)
        {
            if (player != null)
                player.SendSysMessage(CypherStrings.ItemAlreadyInList, vItem.Item, vItem.ExtendedCost, vItem.Type);
            else
                Log.Logger.Error("Table `npcvendor` has duplicate items {0} (with extended cost {1}, type {2}) for vendor (Entry: {3}), ignoring", vItem.Item, vItem.ExtendedCost, vItem.Type, vendorentry);

            return false;
        }

        if (vItem.Type == ItemVendorType.Currency && vItem.Maxcount == 0)
        {
            Log.Logger.Error("Table `(game_event_)npc_vendor` have Item (Entry: {0}, type: {1}) with missing maxcount for vendor ({2}), ignore", vItem.Item, vItem.Type, vendorentry);

            return false;
        }

        return true;
    }

    public void LoadAccessRequirements()
    {
        var oldMSTime = Time.MSTime;

        _accessRequirementStorage.Clear();

        //                                          0      1           2          3          4           5      6             7             8                      9     
        var result = _worldDatabase.Query("SELECT mapid, difficulty, level_min, level_max, item, item2, quest_done_A, quest_done_H, completed_achievement, quest_failed_text FROM access_requirement");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 access requirement definitions. DB table `access_requirement` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var mapid = result.Read<uint>(0);

            if (!_cliDB.MapStorage.ContainsKey(mapid))
            {
                Log.Logger.Error("Map {0} referenced in `access_requirement` does not exist, skipped.", mapid);

                continue;
            }

            var difficulty = result.Read<uint>(1);

            if (Global.DB2Mgr.GetMapDifficultyData(mapid, (Difficulty)difficulty) == null)
            {
                Log.Logger.Error("Map {0} referenced in `access_requirement` does not have difficulty {1}, skipped", mapid, difficulty);

                continue;
            }

            var requirementId = MathFunctions.MakePair64(mapid, difficulty);

            AccessRequirement ar = new()
            {
                LevelMin = result.Read<byte>(2),
                LevelMax = result.Read<byte>(3),
                Item = result.Read<uint>(4),
                Item2 = result.Read<uint>(5),
                QuestA = result.Read<uint>(6),
                QuestH = result.Read<uint>(7),
                Achievement = result.Read<uint>(8),
                QuestFailedText = result.Read<string>(9)
            };

            if (ar.Item != 0)
            {
                var pProto = GetItemTemplate(ar.Item);

                if (pProto == null)
                {
                    Log.Logger.Error("Key item {0} does not exist for map {1} difficulty {2}, removing key requirement.", ar.Item, mapid, difficulty);
                    ar.Item = 0;
                }
            }

            if (ar.Item2 != 0)
            {
                var pProto = GetItemTemplate(ar.Item2);

                if (pProto == null)
                {
                    Log.Logger.Error("Second item {0} does not exist for map {1} difficulty {2}, removing key requirement.", ar.Item2, mapid, difficulty);
                    ar.Item2 = 0;
                }
            }

            if (ar.QuestA != 0)
                if (GetQuestTemplate(ar.QuestA) == null)
                {
                    Log.Logger.Error("Required Alliance QuestId {0} not exist for map {1} difficulty {2}, remove quest done requirement.", ar.QuestA, mapid, difficulty);
                    ar.QuestA = 0;
                }

            if (ar.QuestH != 0)
                if (GetQuestTemplate(ar.QuestH) == null)
                {
                    Log.Logger.Error("Required Horde QuestId {0} not exist for map {1} difficulty {2}, remove quest done requirement.", ar.QuestH, mapid, difficulty);
                    ar.QuestH = 0;
                }

            if (ar.Achievement != 0)
                if (!_cliDB.AchievementStorage.ContainsKey(ar.Achievement))
                {
                    Log.Logger.Error("Required Achievement {0} not exist for map {1} difficulty {2}, remove quest done requirement.", ar.Achievement, mapid, difficulty);
                    ar.Achievement = 0;
                }

            _accessRequirementStorage[requirementId] = ar;
            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} access requirement definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    //Scripts
    public void LoadAreaTriggerScripts()
    {
        var oldMSTime = Time.MSTime;

        _areaTriggerScriptStorage.Clear(); // need for reload case
        var result = _worldDatabase.Query("SELECT entry, ScriptName FROM areatrigger_scripts");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 areatrigger scripts. DB table `areatrigger_scripts` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var id = result.Read<uint>(0);
            var scriptName = result.Read<string>(1);

            var atEntry = _cliDB.AreaTriggerStorage.LookupByKey(id);

            if (atEntry == null)
            {
                Log.Logger.Error("Area trigger (Id:{0}) does not exist in `AreaTrigger.dbc`.", id);

                continue;
            }

            ++count;
            _areaTriggerScriptStorage.AddUnique(id, GetScriptId(scriptName));
        } while (result.NextRow());

        _areaTriggerScriptStorage.RemoveIfMatching((script) =>
        {
            var areaTriggerScriptLoaders = ScriptManager.CreateAreaTriggerScriptLoaders(script.Key);

            foreach (var pair in areaTriggerScriptLoaders)
            {
                var areaTriggerScript = pair.Key.GetAreaTriggerScript();
                var valid = true;

                if (areaTriggerScript == null)
                {
                    Log.Logger.Error("Functions LoadAreaTriggerScripts() of script `{0}` do not return object - script skipped", GetScriptName(pair.Value));
                    valid = false;
                }

                if (areaTriggerScript != null)
                {
                    areaTriggerScript._Init(pair.Key.GetName(), script.Key);
                    areaTriggerScript._Register();
                }

                if (!valid)
                    return true;
            }

            return false;
        });

        Log.Logger.Information("Loaded {0} areatrigger scripts in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadAreaTriggerTeleports()
    {
        var oldMSTime = Time.MSTime;

        _areaTriggerStorage.Clear(); // need for reload case

        //                                         0   1
        var result = _worldDatabase.Query("SELECT ID, PortLocID FROM areatrigger_teleport");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 area trigger teleport definitions. DB table `areatrigger_teleport` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            ++count;

            var Trigger_ID = result.Read<uint>(0);
            var PortLocID = result.Read<uint>(1);

            var portLoc = GetWorldSafeLoc(PortLocID);

            if (portLoc == null)
            {
                Log.Logger.Error("Area Trigger (ID: {0}) has a non-existing Port Loc (ID: {1}) in WorldSafeLocs.dbc, skipped", Trigger_ID, PortLocID);

                continue;
            }

            AreaTriggerStruct at = new()
            {
                TargetMapId = portLoc.Loc.MapId,
                TargetX = portLoc.Loc.X,
                TargetY = portLoc.Loc.Y,
                TargetZ = portLoc.Loc.Z,
                TargetOrientation = portLoc.Loc.Orientation,
                PortLocId = portLoc.Id
            };

            var atEntry = _cliDB.AreaTriggerStorage.LookupByKey(Trigger_ID);

            if (atEntry == null)
            {
                Log.Logger.Error("Area trigger (ID: {0}) does not exist in `AreaTrigger.dbc`.", Trigger_ID);

                continue;
            }

            _areaTriggerStorage[Trigger_ID] = at;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} area trigger teleport definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadCreatureAddons()
    {
        var time = Time.MSTime;
        //                                         0     1        2      3           4         5         6            7         8      9          10               11            12                      13
        var result = _worldDatabase.Query("SELECT guid, path_id, mount, StandState, AnimTier, VisFlags, SheathState, PvPFlags, emote, aiAnimKit, movementAnimKit, meleeAnimKit, visibilityDistanceType, auras FROM creature_addon");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 creature addon definitions. DB table `creature_addon` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var guid = result.Read<ulong>(0);
            var creData = GetCreatureData(guid);

            if (creData == null)
            {
                if (_configuration.GetDefaultValue("load.autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM creature_addon WHERE guid = {guid}");
                else
                    Log.Logger.Error($"Creature (GUID: {guid}) does not exist but has a record in `creatureaddon`");

                continue;
            }

            CreatureAddon creatureAddon = new()
            {
                PathId = result.Read<uint>(1)
            };

            if (creData.MovementType == (byte)MovementGeneratorType.Waypoint && creatureAddon.PathId == 0)
            {
                creData.MovementType = (byte)MovementGeneratorType.Idle;
                Log.Logger.Error($"Creature (GUID {guid}) has movement type set to WAYPOINTMOTIONTYPE but no path assigned");
            }

            creatureAddon.Mount = result.Read<uint>(2);
            creatureAddon.StandState = result.Read<byte>(3);
            creatureAddon.AnimTier = result.Read<byte>(4);
            creatureAddon.VisFlags = result.Read<byte>(5);
            creatureAddon.SheathState = result.Read<byte>(6);
            creatureAddon.PvpFlags = result.Read<byte>(7);
            creatureAddon.Emote = result.Read<uint>(8);
            creatureAddon.AiAnimKit = result.Read<ushort>(9);
            creatureAddon.MovementAnimKit = result.Read<ushort>(10);
            creatureAddon.MeleeAnimKit = result.Read<ushort>(11);
            creatureAddon.VisibilityDistanceType = (VisibilityDistanceType)result.Read<byte>(12);

            var tokens = new StringArray(result.Read<string>(13), ' ');

            for (var c = 0; c < tokens.Length; ++c)
            {
                var id = tokens[c].Trim().Replace(",", "");

                if (!uint.TryParse(id, out var spellId))
                    continue;

                var AdditionalSpellInfo = Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None);

                if (AdditionalSpellInfo == null)
                {
                    Log.Logger.Error($"Creature (GUID: {guid}) has wrong spell {spellId} defined in `auras` field in `creatureaddon`.");

                    continue;
                }

                if (AdditionalSpellInfo.HasAura(AuraType.ControlVehicle))
                    Log.Logger.Error($"Creature (GUID: {guid}) has SPELL_AURA_CONTROL_VEHICLE aura {spellId} defined in `auras` field in `creature_addon`.");

                if (creatureAddon.Auras.Contains(spellId))
                {
                    Log.Logger.Error($"Creature (GUID: {guid}) has duplicate aura (spell {spellId}) in `auras` field in `creature_addon`.");

                    continue;
                }

                if (AdditionalSpellInfo.Duration > 0)
                {
                    Log.Logger.Debug($"Creature (GUID: {guid}) has temporary aura (spell {spellId}) in `auras` field in `creature_addon`.");

                    continue;
                }

                creatureAddon.Auras.Add(spellId);
            }

            if (creatureAddon.Mount != 0)
                if (!_cliDB.CreatureDisplayInfoStorage.ContainsKey(creatureAddon.Mount))
                {
                    Log.Logger.Error($"Creature (GUID: {guid}) has invalid displayInfoId ({creatureAddon.Mount}) for mount defined in `creatureaddon`");
                    creatureAddon.Mount = 0;
                }

            if (creatureAddon.StandState >= (int)UnitStandStateType.Max)
            {
                Log.Logger.Error($"Creature (GUID: {guid}) has invalid unit stand state ({creatureAddon.StandState}) defined in `creature_addon`. Truncated to 0.");
                creatureAddon.StandState = 0;
            }

            if (creatureAddon.AnimTier >= (int)AnimTier.Max)
            {
                Log.Logger.Error($"Creature (GUID: {guid}) has invalid animation tier ({creatureAddon.AnimTier}) defined in `creature_addon`. Truncated to 0.");
                creatureAddon.AnimTier = 0;
            }

            if (creatureAddon.SheathState >= (int)SheathState.Max)
            {
                Log.Logger.Error($"Creature (GUID: {guid}) has invalid sheath state ({creatureAddon.SheathState}) defined in `creature_addon`. Truncated to 0.");
                creatureAddon.SheathState = 0;
            }

            // PvPFlags don't need any checking for the time being since they cover the entire range of a byte

            if (!_cliDB.EmotesStorage.ContainsKey(creatureAddon.Emote))
            {
                Log.Logger.Error($"Creature (GUID: {guid}) has invalid emote ({creatureAddon.Emote}) defined in `creatureaddon`.");
                creatureAddon.Emote = 0;
            }


            if (creatureAddon.AiAnimKit != 0 && !_cliDB.AnimKitStorage.ContainsKey(creatureAddon.AiAnimKit))
            {
                Log.Logger.Error($"Creature (Guid: {guid}) has invalid aiAnimKit ({creatureAddon.AiAnimKit}) defined in `creature_addon`.");
                creatureAddon.AiAnimKit = 0;
            }

            if (creatureAddon.MovementAnimKit != 0 && !_cliDB.AnimKitStorage.ContainsKey(creatureAddon.MovementAnimKit))
            {
                Log.Logger.Error($"Creature (Guid: {guid}) has invalid movementAnimKit ({creatureAddon.MovementAnimKit}) defined in `creature_addon`.");
                creatureAddon.MovementAnimKit = 0;
            }

            if (creatureAddon.MeleeAnimKit != 0 && !_cliDB.AnimKitStorage.ContainsKey(creatureAddon.MeleeAnimKit))
            {
                Log.Logger.Error($"Creature (Guid: {guid}) has invalid meleeAnimKit ({creatureAddon.MeleeAnimKit}) defined in `creature_addon`.");
                creatureAddon.MeleeAnimKit = 0;
            }

            if (creatureAddon.VisibilityDistanceType >= VisibilityDistanceType.Max)
            {
                Log.Logger.Error($"Creature (GUID: {guid}) has invalid visibilityDistanceType ({creatureAddon.VisibilityDistanceType}) defined in `creature_addon`.");
                creatureAddon.VisibilityDistanceType = VisibilityDistanceType.Normal;
            }

            _creatureAddonStorage.Add(guid, creatureAddon);
            count++;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {count} creature addons in {Time.GetMSTimeDiffToNow(time)} ms");
    }

    public void LoadCreatureClassLevelStats()
    {
        var time = Time.MSTime;

        _creatureBaseStatsStorage.Clear();

        //                                         0      1      2         3            4
        var result = _worldDatabase.Query("SELECT level, class, basemana, attackpower, rangedattackpower FROM creature_classlevelstats");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 creature base stats. DB table `creature_classlevelstats` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var Level = result.Read<byte>(0);
            var _class = result.Read<byte>(1);

            if (_class == 0 || ((1 << (_class - 1)) & (int)PlayerClass.ClassMaskAllCreatures) == 0)
                Log.Logger.Error("Creature base stats for level {0} has invalid class {1}", Level, _class);

            CreatureBaseStats stats = new()
            {
                BaseMana = result.Read<uint>(2),
                AttackPower = result.Read<ushort>(3),
                RangedAttackPower = result.Read<ushort>(4)
            };

            _creatureBaseStatsStorage.Add(MathFunctions.MakePair16(Level, _class), stats);

            ++count;
        } while (result.NextRow());

        foreach (var creatureTemplate in _creatureTemplateStorage.Values)
            for (var lvl = creatureTemplate.Minlevel; lvl <= creatureTemplate.Maxlevel; ++lvl)
                if (_creatureBaseStatsStorage.LookupByKey(MathFunctions.MakePair16((uint)lvl, creatureTemplate.UnitClass)) == null)
                    Log.Logger.Error("Missing base stats for creature class {0} level {1}", creatureTemplate.UnitClass, lvl);

        Log.Logger.Information("Loaded {0} creature base stats in {1} ms", count, Time.GetMSTimeDiffToNow(time));
    }

    //Locales
    public void LoadCreatureLocales()
    {
        var oldMSTime = Time.MSTime;

        _creatureLocaleStorage.Clear(); // need for reload case

        //                                         0      1       2     3        4      5
        var result = _worldDatabase.Query("SELECT entry, locale, Name, NameAlt, Title, TitleAlt FROM creature_template_locale");

        if (result.IsEmpty())
            return;

        do
        {
            var id = result.Read<uint>(0);
            var localeName = result.Read<string>(1);
            var locale = localeName.ToEnum<Locale>();

            if (!SharedConst.IsValidLocale(locale) || locale == Locale.enUS)
                continue;

            if (!_creatureLocaleStorage.ContainsKey(id))
                _creatureLocaleStorage[id] = new CreatureLocale();

            var data = _creatureLocaleStorage[id];
            AddLocaleString(result.Read<string>(2), locale, data.Name);
            AddLocaleString(result.Read<string>(3), locale, data.NameAlt);
            AddLocaleString(result.Read<string>(4), locale, data.Title);
            AddLocaleString(result.Read<string>(5), locale, data.TitleAlt);
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} creature locale strings in {1} ms", _creatureLocaleStorage.Count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadCreatureModelInfo()
    {
        var time = Time.MSTime;
        var result = _worldDatabase.Query("SELECT DisplayID, BoundingRadius, CombatReach, DisplayID_Other_Gender FROM creature_model_info");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 creature model definitions. DB table `creaturemodelinfo` is empty.");

            return;
        }

        // List of model FileDataIDs that the client treats as invisible stalker
        uint[] trigggerCreatureModelFileID =
        {
            124640, 124641, 124642, 343863, 439302
        };

        uint count = 0;

        do
        {
            var displayId = result.Read<uint>(0);

            var creatureDisplay = _cliDB.CreatureDisplayInfoStorage.LookupByKey(displayId);

            if (creatureDisplay == null)
            {
                Log.Logger.Debug("Table `creature_model_info` has a non-existent DisplayID (ID: {0}). Skipped.", displayId);

                continue;
            }

            CreatureModelInfo modelInfo = new()
            {
                BoundingRadius = result.Read<float>(1),
                CombatReach = result.Read<float>(2),
                DisplayIdOtherGender = result.Read<uint>(3),
                Gender = creatureDisplay.Gender
            };

            // Checks
            if (modelInfo.Gender == (sbyte)Gender.Unknown)
                modelInfo.Gender = (sbyte)Gender.Male;

            if (modelInfo.DisplayIdOtherGender != 0 && !_cliDB.CreatureDisplayInfoStorage.ContainsKey(modelInfo.DisplayIdOtherGender))
            {
                Log.Logger.Debug("Table `creature_model_info` has a non-existent DisplayID_Other_Gender (ID: {0}) being used by DisplayID (ID: {1}).", modelInfo.DisplayIdOtherGender, displayId);
                modelInfo.DisplayIdOtherGender = 0;
            }

            if (modelInfo.CombatReach < 0.1f)
                modelInfo.CombatReach = SharedConst.DefaultPlayerCombatReach;

            var modelData = _cliDB.CreatureModelDataStorage.LookupByKey(creatureDisplay.ModelID);

            if (modelData != null)
                for (uint i = 0; i < 5; ++i)
                    if (modelData.FileDataID == trigggerCreatureModelFileID[i])
                    {
                        modelInfo.IsTrigger = true;

                        break;
                    }

            _creatureModelStorage.Add(displayId, modelInfo);
            count++;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} creature model based info in {1} ms", count, Time.GetMSTimeDiffToNow(time));
    }

    public void LoadCreatureMovementOverrides()
    {
        var oldMSTime = Time.MSTime;

        _creatureMovementOverrides.Clear();

        // Load the data from creature_movement_override and if NULL fallback to creature_template_movement
        var result = _worldDatabase.Query("SELECT cmo.SpawnId,COALESCE(cmo.Ground, ctm.Ground),COALESCE(cmo.Swim, ctm.Swim),COALESCE(cmo.Flight, ctm.Flight),COALESCE(cmo.Rooted, ctm.Rooted),COALESCE(cmo.Chase, ctm.Chase),COALESCE(cmo.Random, ctm.Random)," +
                                          "COALESCE(cmo.InteractionPauseTimer, ctm.InteractionPauseTimer) FROM creature_movement_override AS cmo LEFT JOIN creature AS c ON c.guid = cmo.SpawnId LEFT JOIN creature_template_movement AS ctm ON ctm.CreatureId = c.id");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 creature movement overrides. DB table `creature_movement_override` is empty!");

            return;
        }

        do
        {
            var spawnId = result.Read<ulong>(0);

            if (GetCreatureData(spawnId) == null)
            {
                if (_configuration.GetDefaultValue("load.autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM creature_movement_override WHERE SpawnId = {spawnId}");
                else
                    Log.Logger.Error($"Creature (GUID: {spawnId}) does not exist but has a record in `creature_movement_override`");

                continue;
            }

            CreatureMovementData movement = new();

            if (!result.IsNull(1))
                movement.Ground = (CreatureGroundMovementType)result.Read<byte>(1);

            if (!result.IsNull(2))
                movement.Swim = result.Read<bool>(2);

            if (!result.IsNull(3))
                movement.Flight = (CreatureFlightMovementType)result.Read<byte>(3);

            if (!result.IsNull(4))
                movement.Rooted = result.Read<bool>(4);

            if (!result.IsNull(5))
                movement.Chase = (CreatureChaseMovementType)result.Read<byte>(5);

            if (!result.IsNull(6))
                movement.Random = (CreatureRandomMovementType)result.Read<byte>(6);

            if (!result.IsNull(7))
                movement.InteractionPauseTimer = result.Read<uint>(7);

            CheckCreatureMovement("creature_movement_override", spawnId, movement);

            _creatureMovementOverrides[spawnId] = movement;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {_creatureMovementOverrides.Count} movement overrides in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    public void LoadCreatureQuestEnders()
    {
        LoadQuestRelationsHelper(_creatureQuestInvolvedRelations, _creatureQuestInvolvedRelationsReverse, "creature_questender");

        foreach (var pair in _creatureQuestInvolvedRelations.KeyValueList)
        {
            var cInfo = GetCreatureTemplate(pair.Key);

            if (cInfo == null)
            {
                Log.Logger.Error("Table `creature_questender` have data for not existed creature entry ({0}) and existed quest {1}", pair.Key, pair.Value);
            }
            else if (!Convert.ToBoolean(cInfo.Npcflag & (uint)NPCFlags.QuestGiver))
            {
                Log.Logger.Verbose("Table `creature_questender` has creature entry ({0}) for quest {1}, but npcflag does not include UNIT_NPC_FLAG_QUESTGIVER", pair.Key, pair.Value);
                cInfo.Npcflag &= (uint)NPCFlags.QuestGiver;
            }
        }
    }

    public void LoadCreatureQuestItems()
    {
        var oldMSTime = Time.MSTime;

        //                                          0              1      2
        var result = _worldDatabase.Query("SELECT CreatureEntry, ItemId, Idx FROM creature_questitem ORDER BY Idx ASC");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 creature quest items. DB table `creature_questitem` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var entry = result.Read<uint>(0);
            var item = result.Read<uint>(1);
            var idx = result.Read<uint>(2);

            if (!_creatureTemplateStorage.ContainsKey(entry))
            {
                if (_configuration.GetDefaultValue("load.autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM creature_questitem WHERE CreatureEntry = {entry}");
                else
                    Log.Logger.Error("Table `creature_questitem` has data for nonexistent creature (entry: {0}, idx: {1}), skipped", entry, idx);

                continue;
            }

            if (!_cliDB.ItemStorage.ContainsKey(item))
            {
                Log.Logger.Error("Table `creature_questitem` has nonexistent item (ID: {0}) in creature (entry: {1}, idx: {2}), skipped", item, entry, idx);

                continue;
            }

            _creatureQuestItemStorage.Add(entry, item);

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} creature quest items in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadCreatureQuestStarters()
    {
        LoadQuestRelationsHelper(_creatureQuestRelations, null, "creature_queststarter");

        foreach (var pair in _creatureQuestRelations.KeyValueList)
        {
            var cInfo = GetCreatureTemplate(pair.Key);

            if (cInfo == null)
            {
                Log.Logger.Debug("Table `creature_queststarter` have data for not existed creature entry ({0}) and existed quest {1}", pair.Key, pair.Value);
            }
            else if (!Convert.ToBoolean(cInfo.Npcflag & (uint)NPCFlags.QuestGiver))
            {
                Log.Logger.Verbose("Table `creature_queststarter` has creature entry ({0}) for quest {1}, but npcflag does not include UNIT_NPC_FLAG_QUESTGIVER", pair.Key, pair.Value);
                cInfo.Npcflag &= (uint)NPCFlags.QuestGiver;
            }
        }
    }

    public void LoadCreatures()
    {
        var time = Time.MSTime;

        //                                         0              1   2    3           4           5           6            7        8             9              10
        var result = _worldDatabase.Query("SELECT creature.guid, id, map, position_x, position_y, position_z, orientation, modelid, equipment_id, spawntimesecs, wander_distance, " +
                                          //11               12         13       14            15                 16          17           18                19                   20                    21
                                          "currentwaypoint, curhealth, curmana, MovementType, spawnDifficulties, eventEntry, poolSpawnId, creature.npcflag, creature.unit_flags, creature.unit_flags2, creature.unit_flags3, " +
                                          //   22                     23                      24                25                   26                       27                   28
                                          "creature.dynamicflags, creature.phaseUseFlags, creature.phaseid, creature.phasegroup, creature.terrainSwapMap, creature.ScriptName, creature.StringId " +
                                          "FROM creature LEFT OUTER JOIN game_event_creature ON creature.guid = game_event_creature.guid LEFT OUTER JOIN pool_members ON pool_members.type = 0 AND creature.guid = pool_members.spawnId");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 creatures. DB table `creature` is empty.");

            return;
        }

        // Build single time for check spawnmask
        Dictionary<uint, List<Difficulty>> spawnMasks = new();

        foreach (var mapDifficultyPair in Global.DB2Mgr.GetMapDifficulties())
        {
            foreach (var difficultyPair in mapDifficultyPair.Value)
            {
                if (!spawnMasks.ContainsKey(mapDifficultyPair.Key))
                    spawnMasks[mapDifficultyPair.Key] = new List<Difficulty>();

                spawnMasks[mapDifficultyPair.Key].Add((Difficulty)difficultyPair.Key);
            }
        }

        PhaseShift phaseShift = new();

        uint count = 0;

        do
        {
            var guid = result.Read<ulong>(0);
            var entry = result.Read<uint>(1);

            var cInfo = GetCreatureTemplate(entry);

            if (cInfo == null)
            {
                if (_configuration.GetDefaultValue("load.autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM creature WHERE id = {entry}");
                else
                    Log.Logger.Error("Table `creature` has creature (GUID: {0}) with non existing creature entry {1}, skipped.", guid, entry);

                continue;
            }

            CreatureData data = new()
            {
                SpawnId = guid,
                Id = entry,
                MapId = result.Read<ushort>(2),
                SpawnPoint = new Position(result.Read<float>(3), result.Read<float>(4), result.Read<float>(5), result.Read<float>(6)),
                Displayid = result.Read<uint>(7),
                EquipmentId = result.Read<sbyte>(8),
                spawntimesecs = result.Read<int>(9),
                WanderDistance = result.Read<float>(10),
                Currentwaypoint = result.Read<uint>(11),
                Curhealth = result.Read<uint>(12),
                Curmana = result.Read<uint>(13),
                MovementType = result.Read<byte>(14)
            };

            data.SpawnDifficulties = ParseSpawnDifficulties(result.Read<string>(15), "creature", guid, data.MapId, spawnMasks.LookupByKey(data.MapId));
            var gameEvent = result.Read<short>(16);
            data.poolId = result.Read<uint>(17);
            data.Npcflag = result.Read<ulong>(18);
            data.UnitFlags = result.Read<uint>(19);
            data.UnitFlags2 = result.Read<uint>(20);
            data.UnitFlags3 = result.Read<uint>(21);
            data.Dynamicflags = result.Read<uint>(22);
            data.PhaseUseFlags = (PhaseUseFlagsValues)result.Read<byte>(23);
            data.PhaseId = result.Read<uint>(24);
            data.PhaseGroup = result.Read<uint>(25);
            data.terrainSwapMap = result.Read<int>(26);

            var scriptId = result.Read<string>(27);

            if (string.IsNullOrEmpty(scriptId))
                data.ScriptId = GetScriptId(scriptId);

            data.StringId = result.Read<string>(28);
            data.SpawnGroupData = _spawnGroupDataStorage[IsTransportMap(data.MapId) ? 1 : 0u]; // transport spawns default to compatibility group

            var mapEntry = _cliDB.MapStorage.LookupByKey(data.MapId);

            if (mapEntry == null)
            {
                Log.Logger.Error("Table `creature` have creature (GUID: {0}) that spawned at not existed map (Id: {1}), skipped.", guid, data.MapId);

                continue;
            }

            if (data.SpawnDifficulties.Empty())
            {
                Log.Logger.Error($"Table `creature` has creature (GUID: {guid}) that is not spawned in any difficulty, skipped.");

                continue;
            }

            var ok = true;

            for (uint diff = 0; diff < SharedConst.MaxCreatureDifficulties && ok; ++diff)
                if (_difficultyEntries[diff].Contains(data.Id))
                {
                    Log.Logger.Error("Table `creature` have creature (GUID: {0}) that listed as difficulty {1} template (entry: {2}) in `creaturetemplate`, skipped.", guid, diff + 1, data.Id);
                    ok = false;
                }

            if (!ok)
                continue;

            // -1 random, 0 no equipment,
            if (data.EquipmentId != 0)
                if (GetEquipmentInfo(data.Id, data.EquipmentId) == null)
                {
                    Log.Logger.Error("Table `creature` have creature (Entry: {0}) with equipmentid {1} not found in table `creatureequiptemplate`, set to no equipment.", data.Id, data.EquipmentId);
                    data.EquipmentId = 0;
                }

            if (cInfo.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.InstanceBind))
                if (!mapEntry.IsDungeon())
                    Log.Logger.Error("Table `creature` have creature (GUID: {0} Entry: {1}) with `creature_template`.`flagsextra` including CREATUREFLAGEXTRAINSTANCEBIND " +
                                     "but creature are not in instance.",
                                     guid,
                                     data.Id);

            if (data.WanderDistance < 0.0f)
            {
                Log.Logger.Error("Table `creature` have creature (GUID: {0} Entry: {1}) with `wander_distance`< 0, set to 0.", guid, data.Id);
                data.WanderDistance = 0.0f;
            }
            else if (data.MovementType == (byte)MovementGeneratorType.Random)
            {
                if (MathFunctions.fuzzyEq(data.WanderDistance, 0.0f))
                {
                    Log.Logger.Error("Table `creature` have creature (GUID: {0} Entry: {1}) with `MovementType`=1 (random movement) but with `wander_distance`=0, replace by idle movement type (0).", guid, data.Id);
                    data.MovementType = (byte)MovementGeneratorType.Idle;
                }
            }
            else if (data.MovementType == (byte)MovementGeneratorType.Idle)
            {
                if (data.WanderDistance != 0.0f)
                {
                    Log.Logger.Error("Table `creature` have creature (GUID: {0} Entry: {1}) with `MovementType`=0 (idle) have `wander_distance`<>0, set to 0.", guid, data.Id);
                    data.WanderDistance = 0.0f;
                }
            }

            if (Convert.ToBoolean(data.PhaseUseFlags & ~PhaseUseFlagsValues.All))
            {
                Log.Logger.Error("Table `creature` have creature (GUID: {0} Entry: {1}) has unknown `phaseUseFlags` set, removed unknown value.", guid, data.Id);
                data.PhaseUseFlags &= PhaseUseFlagsValues.All;
            }

            if (data.PhaseUseFlags.HasAnyFlag(PhaseUseFlagsValues.AlwaysVisible) && data.PhaseUseFlags.HasAnyFlag(PhaseUseFlagsValues.Inverse))
            {
                Log.Logger.Error("Table `creature` have creature (GUID: {0} Entry: {1}) has both `phaseUseFlags` PHASE_USE_FLAGS_ALWAYS_VISIBLE and PHASE_USE_FLAGS_INVERSE," +
                                 " removing PHASE_USE_FLAGS_INVERSE.",
                                 guid,
                                 data.Id);

                data.PhaseUseFlags &= ~PhaseUseFlagsValues.Inverse;
            }

            if (data.PhaseGroup != 0 && data.PhaseId != 0)
            {
                Log.Logger.Error("Table `creature` have creature (GUID: {0} Entry: {1}) with both `phaseid` and `phasegroup` set, `phasegroup` set to 0", guid, data.Id);
                data.PhaseGroup = 0;
            }

            if (data.PhaseId != 0)
                if (!_cliDB.PhaseStorage.ContainsKey(data.PhaseId))
                {
                    Log.Logger.Error("Table `creature` have creature (GUID: {0} Entry: {1}) with `phaseid` {2} does not exist, set to 0", guid, data.Id, data.PhaseId);
                    data.PhaseId = 0;
                }

            if (data.PhaseGroup != 0)
                if (Global.DB2Mgr.GetPhasesForGroup(data.PhaseGroup).Empty())
                {
                    Log.Logger.Error("Table `creature` have creature (GUID: {0} Entry: {1}) with `phasegroup` {2} does not exist, set to 0", guid, data.Id, data.PhaseGroup);
                    data.PhaseGroup = 0;
                }

            if (data.terrainSwapMap != -1)
            {
                var terrainSwapEntry = _cliDB.MapStorage.LookupByKey(data.terrainSwapMap);

                if (terrainSwapEntry == null)
                {
                    Log.Logger.Error("Table `creature` have creature (GUID: {0} Entry: {1}) with `terrainSwapMap` {2} does not exist, set to -1", guid, data.Id, data.terrainSwapMap);
                    data.terrainSwapMap = -1;
                }
                else if (terrainSwapEntry.ParentMapID != data.MapId)
                {
                    Log.Logger.Error("Table `creature` have creature (GUID: {0} Entry: {1}) with `terrainSwapMap` {2} which cannot be used on spawn map, set to -1", guid, data.Id, data.terrainSwapMap);
                    data.terrainSwapMap = -1;
                }
            }

            var disallowedUnitFlags = (uint)(cInfo.UnitFlags & ~UnitFlags.Allowed);

            if (disallowedUnitFlags != 0)
            {
                Log.Logger.Error($"Table `creature_template` lists creature (Entry: {cInfo.Entry}) with disallowed `unit_flags` {disallowedUnitFlags}, removing incorrect Id.");
                cInfo.UnitFlags &= UnitFlags.Allowed;
            }

            var disallowedUnitFlags2 = (cInfo.UnitFlags2 & ~(uint)UnitFlags2.Allowed);

            if (disallowedUnitFlags2 != 0)
            {
                Log.Logger.Error($"Table `creature_template` lists creature (Entry: {cInfo.Entry}) with disallowed `unit_flags2` {disallowedUnitFlags2}, removing incorrect Id.");
                cInfo.UnitFlags2 &= (uint)UnitFlags2.Allowed;
            }

            var disallowedUnitFlags3 = (cInfo.UnitFlags3 & ~(uint)UnitFlags3.Allowed);

            if (disallowedUnitFlags3 != 0)
            {
                Log.Logger.Error($"Table `creature_template` lists creature (Entry: {cInfo.Entry}) with disallowed `unit_flags2` {disallowedUnitFlags3}, removing incorrect Id.");
                cInfo.UnitFlags3 &= (uint)UnitFlags3.Allowed;
            }

            if (cInfo.DynamicFlags != 0)
            {
                Log.Logger.Error($"Table `creature_template` lists creature (Entry: {cInfo.Entry}) with `dynamicflags` > 0. Ignored and set to 0.");
                cInfo.DynamicFlags = 0;
            }

            if (_configuration.GetDefaultValue("Calculate.Creature.Zone.Area.Data", false))
            {
                PhasingHandler.InitDbVisibleMapId(phaseShift, data.terrainSwapMap);
                Global.TerrainMgr.GetZoneAndAreaId(phaseShift, out var zoneId, out var areaId, data.MapId, data.SpawnPoint);

                var stmt = _worldDatabase.GetPreparedStatement(WorldStatements.UPD_CREATURE_ZONE_AREA_DATA);
                stmt.AddValue(0, zoneId);
                stmt.AddValue(1, areaId);
                stmt.AddValue(2, guid);

                _worldDatabase.Execute(stmt);
            }

            // Add to grid if not managed by the GameInfo event
            if (gameEvent == 0)
                AddCreatureToGrid(data);

            _creatureDataStorage[guid] = data;
            count++;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} creatures in {1} ms", count, Time.GetMSTimeDiffToNow(time));
    }

    public void LoadCreatureScalingData()
    {
        var oldMSTime = Time.MSTime;

        //                                   0      1             2                     3                     4
        var result = _worldDatabase.Query("SELECT Entry, DifficultyID, LevelScalingDeltaMin, LevelScalingDeltaMax, ContentTuningID FROM creature_template_scaling ORDER BY Entry");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 creature template scaling definitions. DB table `creature_template_scaling` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var entry = result.Read<uint>(0);
            var difficulty = (Difficulty)result.Read<byte>(1);

            var template = _creatureTemplateStorage.LookupByKey(entry);

            if (template == null)
            {
                if (_configuration.GetDefaultValue("load.autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM creature_template_scaling WHERE entry = {entry}");
                else
                    Log.Logger.Error($"Creature template (Entry: {entry}) does not exist but has a record in `creature_template_scaling`");

                continue;
            }

            CreatureLevelScaling creatureLevelScaling = new()
            {
                DeltaLevelMin = result.Read<short>(2),
                DeltaLevelMax = result.Read<short>(3),
                ContentTuningId = result.Read<uint>(4)
            };

            template.ScalingStorage[difficulty] = creatureLevelScaling;

            ++count;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {count} creature template scaling data in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    public void LoadCreatureTemplate(SQLFields fields)
    {
        var entry = fields.Read<uint>(0);

        CreatureTemplate creature = new(_configuration, this)
        {
            Entry = entry
        };

        for (var i = 0; i < SharedConst.MaxCreatureDifficulties; ++i)
            creature.DifficultyEntry[i] = fields.Read<uint>(1 + i);

        for (var i = 0; i < 2; ++i)
            creature.KillCredit[i] = fields.Read<uint>(4 + i);

        creature.Name = fields.Read<string>(6);
        creature.FemaleName = fields.Read<string>(7);
        creature.SubName = fields.Read<string>(8);
        creature.TitleAlt = fields.Read<string>(9);
        creature.IconName = fields.Read<string>(10);
        creature.GossipMenuId = fields.Read<uint>(11);
        creature.Minlevel = fields.Read<short>(12);
        creature.Maxlevel = fields.Read<short>(13);
        creature.HealthScalingExpansion = fields.Read<int>(14);
        creature.RequiredExpansion = fields.Read<uint>(15);
        creature.VignetteID = fields.Read<uint>(16);
        creature.Faction = fields.Read<uint>(17);
        creature.Npcflag = fields.Read<ulong>(18);
        creature.SpeedWalk = fields.Read<float>(19);
        creature.SpeedRun = fields.Read<float>(20);
        creature.Scale = fields.Read<float>(21);
        creature.Rank = (CreatureEliteType)fields.Read<uint>(22);
        creature.DmgSchool = fields.Read<uint>(23);
        creature.BaseAttackTime = fields.Read<uint>(24);
        creature.RangeAttackTime = fields.Read<uint>(25);
        creature.BaseVariance = fields.Read<float>(26);
        creature.RangeVariance = fields.Read<float>(27);
        creature.UnitClass = fields.Read<uint>(28);
        creature.UnitFlags = (UnitFlags)fields.Read<uint>(29);
        creature.UnitFlags2 = fields.Read<uint>(30);
        creature.UnitFlags3 = fields.Read<uint>(31);
        creature.DynamicFlags = fields.Read<uint>(32);
        creature.Family = (CreatureFamily)fields.Read<uint>(33);
        creature.TrainerClass = (PlayerClass)fields.Read<byte>(34);
        creature.CreatureType = (CreatureType)fields.Read<byte>(35);
        creature.TypeFlags = (CreatureTypeFlags)fields.Read<uint>(36);
        creature.TypeFlags2 = fields.Read<uint>(37);
        creature.LootId = fields.Read<uint>(38);
        creature.PickPocketId = fields.Read<uint>(39);
        creature.SkinLootId = fields.Read<uint>(40);

        for (var i = (int)SpellSchools.Holy; i < (int)SpellSchools.Max; ++i)
            creature.Resistance[i] = 0;

        for (var i = 0; i < SharedConst.MaxCreatureSpells; ++i)
            creature.Spells[i] = 0;

        creature.VehicleId = fields.Read<uint>(41);
        creature.MinGold = fields.Read<uint>(42);
        creature.MaxGold = fields.Read<uint>(43);
        creature.AIName = fields.Read<string>(44);
        creature.MovementType = fields.Read<uint>(45);

        if (!fields.IsNull(46))
            creature.Movement.Ground = (CreatureGroundMovementType)fields.Read<byte>(46);

        if (!fields.IsNull(47))
            creature.Movement.Swim = fields.Read<bool>(47);

        if (!fields.IsNull(48))
            creature.Movement.Flight = (CreatureFlightMovementType)fields.Read<byte>(48);

        if (!fields.IsNull(49))
            creature.Movement.Rooted = fields.Read<bool>(49);

        if (!fields.IsNull(50))
            creature.Movement.Chase = (CreatureChaseMovementType)fields.Read<byte>(50);

        if (!fields.IsNull(51))
            creature.Movement.Random = (CreatureRandomMovementType)fields.Read<byte>(51);

        if (!fields.IsNull(52))
            creature.Movement.InteractionPauseTimer = fields.Read<uint>(52);

        creature.HoverHeight = fields.Read<float>(53);
        creature.ModHealth = fields.Read<float>(54);
        creature.ModHealthExtra = fields.Read<float>(55);
        creature.ModMana = fields.Read<float>(56);
        creature.ModManaExtra = fields.Read<float>(57);
        creature.ModArmor = fields.Read<float>(58);
        creature.ModDamage = fields.Read<float>(59);
        creature.ModExperience = fields.Read<float>(60);
        creature.RacialLeader = fields.Read<bool>(61);
        creature.MovementId = fields.Read<uint>(62);
        creature.CreatureDifficultyID = fields.Read<int>(63);
        creature.WidgetSetID = fields.Read<int>(64);
        creature.WidgetSetUnitConditionID = fields.Read<int>(65);
        creature.RegenHealth = fields.Read<bool>(66);
        creature.MechanicImmuneMask = fields.Read<ulong>(67);
        creature.SpellSchoolImmuneMask = fields.Read<uint>(68);
        creature.FlagsExtra = (CreatureFlagsExtra)fields.Read<uint>(69);
        creature.ScriptID = GetScriptId(fields.Read<string>(70));
        creature.StringId = fields.Read<string>(71);

        _creatureTemplateStorage[entry] = creature;
    }

    public void LoadCreatureTemplateAddons()
    {
        var time = Time.MSTime;
        //                                         0      1        2      3           4         5         6            7         8      9          10               11            12                      13
        var result = _worldDatabase.Query("SELECT entry, path_id, mount, StandState, AnimTier, VisFlags, SheathState, PvPFlags, emote, aiAnimKit, movementAnimKit, meleeAnimKit, visibilityDistanceType, auras FROM creature_template_addon");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 creature template addon definitions. DB table `creature_template_addon` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var entry = result.Read<uint>(0);

            if (GetCreatureTemplate(entry) == null)
            {
                if (_configuration.GetDefaultValue("load.autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM creature_template_addon WHERE entry = {entry}");
                else
                    Log.Logger.Error($"Creature template (Entry: {entry}) does not exist but has a record in `creature_template_addon`");

                continue;
            }

            CreatureAddon creatureAddon = new()
            {
                PathId = result.Read<uint>(1),
                Mount = result.Read<uint>(2),
                StandState = result.Read<byte>(3),
                AnimTier = result.Read<byte>(4),
                VisFlags = result.Read<byte>(5),
                SheathState = result.Read<byte>(6),
                PvpFlags = result.Read<byte>(7),
                Emote = result.Read<uint>(8),
                AiAnimKit = result.Read<ushort>(9),
                MovementAnimKit = result.Read<ushort>(10),
                MeleeAnimKit = result.Read<ushort>(11),
                VisibilityDistanceType = (VisibilityDistanceType)result.Read<byte>(12)
            };

            var tokens = new StringArray(result.Read<string>(13), ' ');

            for (var c = 0; c < tokens.Length; ++c)
            {
                var id = tokens[c].Trim().Replace(",", "");

                if (!uint.TryParse(id, out var spellId))
                    continue;

                var AdditionalSpellInfo = Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None);

                if (AdditionalSpellInfo == null)
                {
                    if (_configuration.GetDefaultValue("load.autoclean", false))
                        _worldDatabase.Execute($"DELETE FROM creature_template_addon WHERE entry = {entry}");
                    else
                        Log.Logger.Error($"Creature (Entry: {entry}) has wrong spell {spellId} defined in `auras` field in `creature_template_addon`.");

                    continue;
                }

                if (AdditionalSpellInfo.HasAura(AuraType.ControlVehicle))
                    Log.Logger.Debug($"Creature (Entry: {entry}) has SPELL_AURA_CONTROL_VEHICLE aura {spellId} defined in `auras` field in `creature_template_addon`.");

                if (creatureAddon.Auras.Contains(spellId))
                {
                    if (_configuration.GetDefaultValue("load.autoclean", false))
                        _worldDatabase.Execute($"DELETE FROM creature_template_addon WHERE entry = {entry}");
                    else
                        Log.Logger.Error($"Creature (Entry: {entry}) has duplicate aura (spell {spellId}) in `auras` field in `creature_template_addon`.");

                    continue;
                }

                if (AdditionalSpellInfo.Duration > 0)
                {
                    if (_configuration.GetDefaultValue("load.autoclean", false))
                        _worldDatabase.Execute($"DELETE FROM creature_template_addon WHERE entry = {entry}");
                    else
                        Log.Logger.Error($"Creature (Entry: {entry}) has temporary aura (spell {spellId}) in `auras` field in `creature_template_addon`.");

                    continue;
                }

                creatureAddon.Auras.Add(spellId);
            }

            if (creatureAddon.Mount != 0)
                if (_cliDB.CreatureDisplayInfoStorage.LookupByKey(creatureAddon.Mount) == null)
                {
                    Log.Logger.Debug($"Creature (Entry: {entry}) has invalid displayInfoId ({creatureAddon.Mount}) for mount defined in `creature_template_addon`");
                    creatureAddon.Mount = 0;
                }

            if (creatureAddon.StandState >= (int)UnitStandStateType.Max)
            {
                Log.Logger.Debug($"Creature (Entry: {entry}) has invalid unit stand state ({creatureAddon.StandState}) defined in `creature_template_addon`. Truncated to 0.");
                creatureAddon.StandState = 0;
            }

            if (creatureAddon.AnimTier >= (int)AnimTier.Max)
            {
                Log.Logger.Debug($"Creature (Entry: {entry}) has invalid animation tier ({creatureAddon.AnimTier}) defined in `creature_template_addon`. Truncated to 0.");
                creatureAddon.AnimTier = 0;
            }

            if (creatureAddon.SheathState >= (int)SheathState.Max)
            {
                Log.Logger.Debug($"Creature (Entry: {entry}) has invalid sheath state ({creatureAddon.SheathState}) defined in `creature_template_addon`. Truncated to 0.");
                creatureAddon.SheathState = 0;
            }

            // PvPFlags don't need any checking for the time being since they cover the entire range of a byte

            if (!_cliDB.EmotesStorage.ContainsKey(creatureAddon.Emote))
            {
                Log.Logger.Debug($"Creature (Entry: {entry}) has invalid emote ({creatureAddon.Emote}) defined in `creatureaddon`.");
                creatureAddon.Emote = 0;
            }

            if (creatureAddon.AiAnimKit != 0 && !_cliDB.AnimKitStorage.ContainsKey(creatureAddon.AiAnimKit))
            {
                Log.Logger.Debug($"Creature (Entry: {entry}) has invalid aiAnimKit ({creatureAddon.AiAnimKit}) defined in `creature_template_addon`.");
                creatureAddon.AiAnimKit = 0;
            }

            if (creatureAddon.MovementAnimKit != 0 && !_cliDB.AnimKitStorage.ContainsKey(creatureAddon.MovementAnimKit))
            {
                Log.Logger.Debug($"Creature (Entry: {entry}) has invalid movementAnimKit ({creatureAddon.MovementAnimKit}) defined in `creature_template_addon`.");
                creatureAddon.MovementAnimKit = 0;
            }

            if (creatureAddon.MeleeAnimKit != 0 && !_cliDB.AnimKitStorage.ContainsKey(creatureAddon.MeleeAnimKit))
            {
                Log.Logger.Debug($"Creature (Entry: {entry}) has invalid meleeAnimKit ({creatureAddon.MeleeAnimKit}) defined in `creature_template_addon`.");
                creatureAddon.MeleeAnimKit = 0;
            }

            if (creatureAddon.VisibilityDistanceType >= VisibilityDistanceType.Max)
            {
                Log.Logger.Debug($"Creature (Entry: {entry}) has invalid visibilityDistanceType ({creatureAddon.VisibilityDistanceType}) defined in `creature_template_addon`.");
                creatureAddon.VisibilityDistanceType = VisibilityDistanceType.Normal;
            }

            _creatureTemplateAddonStorage.Add(entry, creatureAddon);
            count++;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {count} creature template addons in {Time.GetMSTimeDiffToNow(time)} ms");
    }

    //Creatures
    public void LoadCreatureTemplates()
    {
        var time = Time.MSTime;

        var stmt = _worldDatabase.GetPreparedStatement(WorldStatements.SEL_CREATURE_TEMPLATE);
        stmt.AddValue(0, 0);
        stmt.AddValue(1, 1);

        var result = _worldDatabase.Query(stmt);

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 creatures. DB table `creature_template` is empty.");

            return;
        }

        do
        {
            LoadCreatureTemplate(result.GetFields());
        } while (result.NextRow());

        LoadCreatureTemplateResistances();
        LoadCreatureTemplateSpells();

        // We load the creature models after loading but before checking
        LoadCreatureTemplateModels();

        LoadCreatureSummonedData();

        // Checking needs to be done after loading because of the difficulty self referencing
        foreach (var template in _creatureTemplateStorage.Values)
            CheckCreatureTemplate(template);

        Log.Logger.Information("Loaded {0} creature definitions in {1} ms", _creatureTemplateStorage.Count, Time.GetMSTimeDiffToNow(time));
    }

    public void LoadCreatureTrainers()
    {
        var oldMSTime = Time.MSTime;

        _creatureDefaultTrainers.Clear();

        var result = _worldDatabase.Query("SELECT CreatureID, TrainerID, MenuID, OptionID FROM creature_trainer");

        if (!result.IsEmpty())
            do
            {
                var creatureId = result.Read<uint>(0);
                var trainerId = result.Read<uint>(1);
                var gossipMenuId = result.Read<uint>(2);
                var gossipOptionIndex = result.Read<uint>(3);

                if (GetCreatureTemplate(creatureId) == null)
                {
                    if (_configuration.GetDefaultValue("load.autoclean", false))
                        _worldDatabase.Execute($"DELETE FROM creature_trainer WHERE CreatureID = {creatureId}");
                    else
                        Log.Logger.Error($"Table `creature_trainer` references non-existing creature template (CreatureId: {creatureId}), ignoring");

                    continue;
                }

                if (GetTrainer(trainerId) == null)
                {
                    if (_configuration.GetDefaultValue("load.autoclean", false))
                        _worldDatabase.Execute($"DELETE FROM creature_trainer WHERE CreatureID = {creatureId}");
                    else
                        Log.Logger.Error($"Table `creature_trainer` references non-existing trainer (TrainerId: {trainerId}) for CreatureId {creatureId} MenuId {gossipMenuId} OptionIndex {gossipOptionIndex}, ignoring");

                    continue;
                }

                if (gossipMenuId != 0 || gossipOptionIndex != 0)
                {
                    var gossipMenuItems = GetGossipMenuItemsMapBounds(gossipMenuId);
                    var gossipOptionItr = gossipMenuItems.Find(entry => { return entry.OrderIndex == gossipOptionIndex; });

                    if (gossipOptionItr == null)
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM creature_trainer WHERE CreatureID = {creatureId}");
                        else
                            Log.Logger.Error($"Table `creature_trainer` references non-existing gossip menu option (MenuId {gossipMenuId} OptionIndex {gossipOptionIndex}) for CreatureId {creatureId} and TrainerId {trainerId}, ignoring");

                        continue;
                    }
                }

                _creatureDefaultTrainers[(creatureId, gossipMenuId, gossipOptionIndex)] = trainerId;
            } while (result.NextRow());

        Log.Logger.Information($"Loaded {_creatureDefaultTrainers.Count} default trainers in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    //General
    public bool LoadCypherStrings()
    {
        var time = Time.MSTime;
        _cypherStringStorage.Clear();

        var result = _worldDatabase.Query("SELECT entry, content_default, content_loc1, content_loc2, content_loc3, content_loc4, content_loc5, content_loc6, content_loc7, content_loc8 FROM trinity_string");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 CypherStrings. DB table `trinity_string` is empty.");

            return false;
        }

        uint count = 0;

        do
        {
            var entry = result.Read<uint>(0);

            _cypherStringStorage[entry] = new StringArray((int)SharedConst.DefaultLocale + 1);
            count++;

            for (var i = SharedConst.DefaultLocale; i >= 0; --i)
                AddLocaleString(result.Read<string>((int)i + 1).ConvertFormatSyntax(), i, _cypherStringStorage[entry]);
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} CypherStrings in {1} ms", count, Time.GetMSTimeDiffToNow(time));

        return true;
    }

    public void LoadEquipmentTemplates()
    {
        var time = Time.MSTime;

        //                                                0   1        2                 3            4
        var result = _worldDatabase.Query("SELECT CreatureID, ID, ItemID1, AppearanceModID1, ItemVisual1, " +
                                          //5                 6            7       8                 9           10
                                          "ItemID2, AppearanceModID2, ItemVisual2, ItemID3, AppearanceModID3, ItemVisual3 " +
                                          "FROM creature_equip_template");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 creature equipment templates. DB table `creature_equip_template` is empty!");

            return;
        }

        uint count = 0;

        do
        {
            var entry = result.Read<uint>(0);

            if (GetCreatureTemplate(entry) == null)
            {
                if (_configuration.GetDefaultValue("load.autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM creature_equip_template WHERE CreatureID = {entry}");
                else
                    Log.Logger.Error("Creature template (CreatureID: {0}) does not exist but has a record in `creature_equip_template`", entry);

                continue;
            }

            var id = result.Read<uint>(1);

            EquipmentInfo equipmentInfo = new();

            for (var i = 0; i < SharedConst.MaxEquipmentItems; ++i)
            {
                equipmentInfo.Items[i].ItemId = result.Read<uint>(2 + i * 3);
                equipmentInfo.Items[i].AppearanceModId = result.Read<ushort>(3 + i * 3);
                equipmentInfo.Items[i].ItemVisual = result.Read<ushort>(4 + i * 3);

                if (equipmentInfo.Items[i].ItemId == 0)
                    continue;

                var dbcItem = _cliDB.ItemStorage.LookupByKey(equipmentInfo.Items[i].ItemId);

                if (dbcItem == null)
                {
                    Log.Logger.Error("Unknown item (ID: {0}) in creature_equip_template.ItemID{1} for CreatureID  = {2}, forced to 0.",
                                     equipmentInfo.Items[i].ItemId,
                                     i + 1,
                                     entry);

                    equipmentInfo.Items[i].ItemId = 0;

                    continue;
                }

                if (Global.DB2Mgr.GetItemModifiedAppearance(equipmentInfo.Items[i].ItemId, equipmentInfo.Items[i].AppearanceModId) == null)
                {
                    Log.Logger.Error("Unknown item appearance for (ID: {0}, AppearanceModID: {1}) pair in creature_equip_template.ItemID{2} creature_equip_template.AppearanceModID{3} " +
                                     "for CreatureID: {4} and ID: {5}, forced to default.",
                                     equipmentInfo.Items[i].ItemId,
                                     equipmentInfo.Items[i].AppearanceModId,
                                     i + 1,
                                     i + 1,
                                     entry,
                                     id);

                    var defaultAppearance = Global.DB2Mgr.GetDefaultItemModifiedAppearance(equipmentInfo.Items[i].ItemId);

                    if (defaultAppearance != null)
                        equipmentInfo.Items[i].AppearanceModId = (ushort)defaultAppearance.ItemAppearanceModifierID;
                    else
                        equipmentInfo.Items[i].AppearanceModId = 0;

                    continue;
                }

                if (dbcItem.inventoryType != InventoryType.Weapon &&
                    dbcItem.inventoryType != InventoryType.Shield &&
                    dbcItem.inventoryType != InventoryType.Ranged &&
                    dbcItem.inventoryType != InventoryType.Weapon2Hand &&
                    dbcItem.inventoryType != InventoryType.WeaponMainhand &&
                    dbcItem.inventoryType != InventoryType.WeaponOffhand &&
                    dbcItem.inventoryType != InventoryType.Holdable &&
                    dbcItem.inventoryType != InventoryType.Thrown &&
                    dbcItem.inventoryType != InventoryType.RangedRight)
                {
                    Log.Logger.Error("Item (ID {0}) in creature_equip_template.ItemID{1} for CreatureID  = {2} is not equipable in a hand, forced to 0.",
                                     equipmentInfo.Items[i].ItemId,
                                     i + 1,
                                     entry);

                    equipmentInfo.Items[i].ItemId = 0;
                }
            }

            _equipmentInfoStorage.Add(entry, Tuple.Create(id, equipmentInfo));
            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} equipment templates in {1} ms", count, Time.GetMSTimeDiffToNow(time));
    }

    public void LoadEventScripts()
    {
        LoadScripts(ScriptsType.Event);

        List<uint> evt_scripts = new();

        // Load all possible script entries from gameobjects
        foreach (var go in _gameObjectTemplateStorage)
        {
            var eventId = go.Value.GetEventScriptId();

            if (eventId != 0)
                evt_scripts.Add(eventId);
        }

        // Load all possible script entries from spells
        foreach (var spellNameEntry in _cliDB.SpellNameStorage.Values)
        {
            var spell = Global.SpellMgr.GetSpellInfo(spellNameEntry.Id, Difficulty.None);

            if (spell != null)
                foreach (var spellEffectInfo in spell.Effects)
                    if (spellEffectInfo.IsEffect(SpellEffectName.SendEvent))
                        if (spellEffectInfo.MiscValue != 0)
                            evt_scripts.Add((uint)spellEffectInfo.MiscValue);
        }

        foreach (var path_idx in _cliDB.TaxiPathNodesByPath)
            for (uint node_idx = 0; node_idx < path_idx.Value.Length; ++node_idx)
            {
                var node = path_idx.Value[node_idx];

                if (node.ArrivalEventID != 0)
                    evt_scripts.Add(node.ArrivalEventID);

                if (node.DepartureEventID != 0)
                    evt_scripts.Add(node.DepartureEventID);
            }

        // Then check if all scripts are in above list of possible script entries
        foreach (var script in EventScripts)
        {
            var id = evt_scripts.Find(p => p == script.Key);

            if (id == 0)
                Log.Logger.Error("Table `event_scripts` has script (Id: {0}) not referring to any gameobject_template type 10 data2 field, type 3 data6 field, type 13 data 2 field or any spell effect {1}",
                                 script.Key,
                                 SpellEffectName.SendEvent);
        }
    }

    public void LoadExplorationBaseXP()
    {
        var oldMSTime = Time.MSTime;

        var result = _worldDatabase.Query("SELECT level, basexp FROM exploration_basexp");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 BaseXP definitions. DB table `exploration_basexp` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var level = result.Read<byte>(0);
            var basexp = result.Read<uint>(1);
            _baseXPTable[level] = basexp;
            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} BaseXP definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    //Faction Change
    public void LoadFactionChangeAchievements()
    {
        var oldMSTime = Time.MSTime;

        var result = _worldDatabase.Query("SELECT alliance_id, horde_id FROM player_factionchange_achievement");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 faction change achievement pairs. DB table `player_factionchange_achievement` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var alliance = result.Read<uint>(0);
            var horde = result.Read<uint>(1);

            if (!_cliDB.AchievementStorage.ContainsKey(alliance))
                Log.Logger.Error("Achievement {0} (alliance_id) referenced in `player_factionchange_achievement` does not exist, pair skipped!", alliance);
            else if (!_cliDB.AchievementStorage.ContainsKey(horde))
                Log.Logger.Error("Achievement {0} (horde_id) referenced in `player_factionchange_achievement` does not exist, pair skipped!", horde);
            else
                FactionChangeAchievements[alliance] = horde;

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} faction change achievement pairs in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadFactionChangeItems()
    {
        var oldMSTime = Time.MSTime;

        uint count = 0;

        foreach (var itemPair in _itemTemplateStorage)
        {
            if (itemPair.Value.OtherFactionItemId == 0)
                continue;

            if (itemPair.Value.HasFlag(ItemFlags2.FactionHorde))
                FactionChangeItemsHordeToAlliance[itemPair.Key] = itemPair.Value.OtherFactionItemId;

            if (itemPair.Value.HasFlag(ItemFlags2.FactionAlliance))
                FactionChangeItemsAllianceToHorde[itemPair.Key] = itemPair.Value.OtherFactionItemId;

            ++count;
        }

        Log.Logger.Information("Loaded {0} faction change item pairs in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadFactionChangeQuests()
    {
        var oldMSTime = Time.MSTime;

        var result = _worldDatabase.Query("SELECT alliance_id, horde_id FROM player_factionchange_quests");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 faction change quest pairs. DB table `player_factionchange_quests` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var alliance = result.Read<uint>(0);
            var horde = result.Read<uint>(1);

            if (GetQuestTemplate(alliance) == null)
                Log.Logger.Error("QuestId {0} (alliance_id) referenced in `player_factionchange_quests` does not exist, pair skipped!", alliance);
            else if (GetQuestTemplate(horde) == null)
                Log.Logger.Error("QuestId {0} (horde_id) referenced in `player_factionchange_quests` does not exist, pair skipped!", horde);
            else
                FactionChangeQuests[alliance] = horde;

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} faction change quest pairs in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadFactionChangeReputations()
    {
        var oldMSTime = Time.MSTime;

        var result = _worldDatabase.Query("SELECT alliance_id, horde_id FROM player_factionchange_reputations");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 faction change reputation pairs. DB table `player_factionchange_reputations` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var alliance = result.Read<uint>(0);
            var horde = result.Read<uint>(1);

            if (!_cliDB.FactionStorage.ContainsKey(alliance))
                Log.Logger.Error("Reputation {0} (alliance_id) referenced in `player_factionchange_reputations` does not exist, pair skipped!", alliance);
            else if (!_cliDB.FactionStorage.ContainsKey(horde))
                Log.Logger.Error("Reputation {0} (horde_id) referenced in `player_factionchange_reputations` does not exist, pair skipped!", horde);
            else
                FactionChangeReputation[alliance] = horde;

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} faction change reputation pairs in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadFactionChangeSpells()
    {
        var oldMSTime = Time.MSTime;

        var result = _worldDatabase.Query("SELECT alliance_id, horde_id FROM player_factionchange_spells");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 faction change spell pairs. DB table `player_factionchange_spells` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var alliance = result.Read<uint>(0);
            var horde = result.Read<uint>(1);

            if (!Global.SpellMgr.HasSpellInfo(alliance, Difficulty.None))
                Log.Logger.Error("Spell {0} (alliance_id) referenced in `player_factionchange_spells` does not exist, pair skipped!", alliance);
            else if (!Global.SpellMgr.HasSpellInfo(horde, Difficulty.None))
                Log.Logger.Error("Spell {0} (horde_id) referenced in `player_factionchange_spells` does not exist, pair skipped!", horde);
            else
                FactionChangeSpells[alliance] = horde;

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} faction change spell pairs in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadFactionChangeTitles()
    {
        var oldMSTime = Time.MSTime;

        var result = _worldDatabase.Query("SELECT alliance_id, horde_id FROM player_factionchange_titles");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 faction change title pairs. DB table `player_factionchange_title` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var alliance = result.Read<uint>(0);
            var horde = result.Read<uint>(1);

            if (!_cliDB.CharTitlesStorage.ContainsKey(alliance))
                Log.Logger.Error("Title {0} (alliance_id) referenced in `player_factionchange_title` does not exist, pair skipped!", alliance);
            else if (!_cliDB.CharTitlesStorage.ContainsKey(horde))
                Log.Logger.Error("Title {0} (horde_id) referenced in `player_factionchange_title` does not exist, pair skipped!", horde);
            else
                FactionChangeTitles[alliance] = horde;

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} faction change title pairs in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadFishingBaseSkillLevel()
    {
        var oldMSTime = Time.MSTime;

        _fishingBaseForAreaStorage.Clear(); // for reload case

        var result = _worldDatabase.Query("SELECT entry, skill FROM skill_fishing_base_level");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 areas for fishing base skill level. DB table `skill_fishing_base_level` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var entry = result.Read<uint>(0);
            var skill = result.Read<int>(1);

            var fArea = _cliDB.AreaTableStorage.LookupByKey(entry);

            if (fArea == null)
            {
                Log.Logger.Error("AreaId {0} defined in `skill_fishing_base_level` does not exist", entry);

                continue;
            }

            _fishingBaseForAreaStorage[entry] = skill;
            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} areas for fishing base skill level in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadGameObjectAddons()
    {
        var oldMSTime = Time.MSTime;

        _gameObjectAddonStorage.Clear();

        //                                         0     1                 2                 3                 4                 5                 6                  7              8
        var result = _worldDatabase.Query("SELECT guid, parent_rotation0, parent_rotation1, parent_rotation2, parent_rotation3, invisibilityType, invisibilityValue, WorldEffectID, AIAnimKitID FROM gameobject_addon");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 gameobject addon definitions. DB table `gameobject_addon` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var guid = result.Read<ulong>(0);

            var goData = GetGameObjectData(guid);

            if (goData == null)
            {
                if (_configuration.GetDefaultValue("load.autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM gameobject_addon WHERE guid = {guid}");
                else
                    Log.Logger.Error($"GameObject (GUID: {guid}) does not exist but has a record in `gameobject_addon`");

                continue;
            }

            GameObjectAddon gameObjectAddon = new()
            {
                ParentRotation = new Quaternion(result.Read<float>(1), result.Read<float>(2), result.Read<float>(3), result.Read<float>(4)),
                invisibilityType = (InvisibilityType)result.Read<byte>(5),
                invisibilityValue = result.Read<uint>(6),
                WorldEffectID = result.Read<uint>(7),
                AIAnimKitID = result.Read<uint>(8)
            };

            if (gameObjectAddon.invisibilityType >= InvisibilityType.Max)
            {
                Log.Logger.Error($"GameObject (GUID: {guid}) has invalid InvisibilityType in `gameobject_addon`, disabled invisibility");
                gameObjectAddon.invisibilityType = InvisibilityType.General;
                gameObjectAddon.invisibilityValue = 0;
            }

            if (gameObjectAddon.invisibilityType != 0 && gameObjectAddon.invisibilityValue == 0)
            {
                Log.Logger.Error($"GameObject (GUID: {guid}) has InvisibilityType set but has no InvisibilityValue in `gameobject_addon`, set to 1");
                gameObjectAddon.invisibilityValue = 1;
            }

            if (!(Math.Abs(Quaternion.Dot(gameObjectAddon.ParentRotation, gameObjectAddon.ParentRotation) - 1) < 1e-5))
            {
                Log.Logger.Error($"GameObject (GUID: {guid}) has invalid parent rotation in `gameobject_addon`, set to default");
                gameObjectAddon.ParentRotation = Quaternion.Identity;
            }

            if (gameObjectAddon.WorldEffectID != 0 && !_cliDB.WorldEffectStorage.ContainsKey(gameObjectAddon.WorldEffectID))
            {
                Log.Logger.Error($"GameObject (GUID: {guid}) has invalid WorldEffectID ({gameObjectAddon.WorldEffectID}) in `gameobject_addon`, set to 0.");
                gameObjectAddon.WorldEffectID = 0;
            }

            if (gameObjectAddon.AIAnimKitID != 0 && !_cliDB.AnimKitStorage.ContainsKey(gameObjectAddon.AIAnimKitID))
            {
                Log.Logger.Error($"GameObject (GUID: {guid}) has invalid AIAnimKitID ({gameObjectAddon.AIAnimKitID}) in `gameobject_addon`, set to 0.");
                gameObjectAddon.AIAnimKitID = 0;
            }

            _gameObjectAddonStorage[guid] = gameObjectAddon;
            ++count;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {count} gameobject addons in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    public void LoadGameObjectForQuests()
    {
        var oldMSTime = Time.MSTime;

        _gameObjectForQuestStorage.Clear(); // need for reload case

        if (GetGameObjectTemplates().Empty())
        {
            Log.Logger.Information("Loaded 0 GameObjects for quests");

            return;
        }

        uint count = 0;

        // collect GO entries for GO that must activated
        foreach (var pair in GetGameObjectTemplates())
        {
            switch (pair.Value.type)
            {
                case GameObjectTypes.QuestGiver:
                    break;
                case GameObjectTypes.Chest:
                {
                    // scan GO chest with loot including quest items
                    // find quest loot for GO
                    if (pair.Value.Chest.questID != 0 || LootStoreBox.Gameobject.HaveQuestLootFor(pair.Value.Chest.chestLoot) || LootStoreBox.Gameobject.HaveQuestLootFor(pair.Value.Chest.chestPersonalLoot) || LootStoreBox.Gameobject.HaveQuestLootFor(pair.Value.Chest.chestPushLoot))
                        break;

                    continue;
                }
                case GameObjectTypes.Generic:
                {
                    if (pair.Value.Generic.questID > 0) //quests objects
                        break;

                    continue;
                }
                case GameObjectTypes.Goober:
                {
                    if (pair.Value.Goober.questID > 0) //quests objects
                        break;

                    continue;
                }
                case GameObjectTypes.GatheringNode:
                {
                    // scan GO chest with loot including quest items
                    // find quest loot for GO
                    if (LootStoreBox.Gameobject.HaveQuestLootFor(pair.Value.GatheringNode.chestLoot))
                        break;

                    continue;
                }
                default:
                    continue;
            }

            _gameObjectForQuestStorage.Add(pair.Value.entry);
            ++count;
        }

        Log.Logger.Information("Loaded {0} GameObjects for quests in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadGameObjectLocales()
    {
        var oldMSTime = Time.MSTime;

        _gameObjectLocaleStorage.Clear(); // need for reload case

        //                                               0      1       2     3               4
        var result = _worldDatabase.Query("SELECT entry, locale, name, castBarCaption, unk1 FROM gameobject_template_locale");

        if (result.IsEmpty())
            return;

        do
        {
            var id = result.Read<uint>(0);
            var localeName = result.Read<string>(1);
            var locale = localeName.ToEnum<Locale>();

            if (!SharedConst.IsValidLocale(locale) || locale == Locale.enUS)
                continue;

            if (!_gameObjectLocaleStorage.ContainsKey(id))
                _gameObjectLocaleStorage[id] = new GameObjectLocale();

            var data = _gameObjectLocaleStorage[id];
            AddLocaleString(result.Read<string>(2), locale, data.Name);
            AddLocaleString(result.Read<string>(3), locale, data.CastBarCaption);
            AddLocaleString(result.Read<string>(4), locale, data.Unk1);
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} gameobject_template_locale locale strings in {1} ms", _gameObjectLocaleStorage.Count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadGameObjectOverrides()
    {
        var oldMSTime = Time.MSTime;

        //                                   0        1        2
        var result = _worldDatabase.Query("SELECT spawnId, faction, flags FROM gameobject_overrides");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 gameobject faction and flags overrides. DB table `gameobject_overrides` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var spawnId = result.Read<ulong>(0);
            var goData = GetGameObjectData(spawnId);

            if (goData == null)
            {
                if (_configuration.GetDefaultValue("load.autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM gameobject_overrides WHERE spawnId = {spawnId}");
                else
                    Log.Logger.Error($"GameObject (SpawnId: {spawnId}) does not exist but has a record in `gameobject_overrides`");

                continue;
            }

            GameObjectOverride gameObjectOverride = new()
            {
                Faction = result.Read<ushort>(1),
                Flags = (GameObjectFlags)result.Read<uint>(2)
            };

            _gameObjectOverrideStorage[spawnId] = gameObjectOverride;

            if (gameObjectOverride.Faction != 0 && !_cliDB.FactionTemplateStorage.ContainsKey(gameObjectOverride.Faction))
                Log.Logger.Error($"GameObject (SpawnId: {spawnId}) has invalid faction ({gameObjectOverride.Faction}) defined in `gameobject_overrides`.");

            ++count;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {count} gameobject faction and flags overrides in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    public void LoadGameobjectQuestEnders()
    {
        LoadQuestRelationsHelper(_goQuestInvolvedRelations, _goQuestInvolvedRelationsReverse, "gameobject_questender");

        foreach (var pair in _goQuestInvolvedRelations.KeyValueList)
        {
            var goInfo = GetGameObjectTemplate(pair.Key);

            if (goInfo == null)
                Log.Logger.Error("Table `gameobject_questender` have data for not existed gameobject entry ({0}) and existed quest {1}", pair.Key, pair.Value);
            else if (goInfo.type != GameObjectTypes.QuestGiver)
                Log.Logger.Error("Table `gameobject_questender` have data gameobject entry ({0}) for quest {1}, but GO is not GAMEOBJECT_TYPE_QUESTGIVER", pair.Key, pair.Value);
        }
    }

    public void LoadGameObjectQuestItems()
    {
        var oldMSTime = Time.MSTime;

        //                                           0                1
        var result = _worldDatabase.Query("SELECT GameObjectEntry, ItemId, Idx FROM gameobject_questitem ORDER BY Idx ASC");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 gameobject quest items. DB table `gameobject_questitem` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var entry = result.Read<uint>(0);
            var item = result.Read<uint>(1);
            var idx = result.Read<uint>(2);

            if (!_gameObjectTemplateStorage.ContainsKey(entry))
            {
                Log.Logger.Error("Table `gameobject_questitem` has data for nonexistent gameobject (entry: {0}, idx: {1}), skipped", entry, idx);

                continue;
            }

            if (!_cliDB.ItemStorage.ContainsKey(item))
            {
                Log.Logger.Error("Table `gameobject_questitem` has nonexistent item (ID: {0}) in gameobject (entry: {1}, idx: {2}), skipped", item, entry, idx);

                continue;
            }

            _gameObjectQuestItemStorage.Add(entry, item);

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} gameobject quest items in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadGameobjectQuestStarters()
    {
        LoadQuestRelationsHelper(_goQuestRelations, null, "gameobject_queststarter");

        foreach (var pair in _goQuestRelations.KeyValueList)
        {
            var goInfo = GetGameObjectTemplate(pair.Key);

            if (goInfo == null)
                Log.Logger.Error("Table `gameobject_queststarter` have data for not existed gameobject entry ({0}) and existed quest {1}", pair.Key, pair.Value);
            else if (goInfo.type != GameObjectTypes.QuestGiver)
                Log.Logger.Error("Table `gameobject_queststarter` have data gameobject entry ({0}) for quest {1}, but GO is not GAMEOBJECT_TYPE_QUESTGIVER", pair.Key, pair.Value);
        }
    }

    public void LoadGameObjects()
    {
        var time = Time.MSTime;

        //                                         0                1   2    3           4           5           6
        var result = _worldDatabase.Query("SELECT gameobject.guid, id, map, position_x, position_y, position_z, orientation, " +
                                          //7          8          9          10         11             12            13     14                 15          16
                                          "rotation0, rotation1, rotation2, rotation3, spawntimesecs, animprogress, state, spawnDifficulties, eventEntry, poolSpawnId, " +
                                          //17             18       19          20              21
                                          "phaseUseFlags, phaseid, phasegroup, terrainSwapMap, ScriptName " +
                                          "FROM gameobject LEFT OUTER JOIN game_event_gameobject ON gameobject.guid = game_event_gameobject.guid " +
                                          "LEFT OUTER JOIN pool_members ON pool_members.type = 1 AND gameobject.guid = pool_members.spawnId");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 gameobjects. DB table `gameobject` is empty.");

            return;
        }

        uint count = 0;

        // build single time for check spawnmask
        Dictionary<uint, List<Difficulty>> spawnMasks = new();

        foreach (var mapDifficultyPair in Global.DB2Mgr.GetMapDifficulties())
        {
            foreach (var difficultyPair in mapDifficultyPair.Value)
            {
                if (!spawnMasks.ContainsKey(mapDifficultyPair.Key))
                    spawnMasks[mapDifficultyPair.Key] = new List<Difficulty>();

                spawnMasks[mapDifficultyPair.Key].Add((Difficulty)difficultyPair.Key);
            }
        }

        PhaseShift phaseShift = new();

        do
        {
            var guid = result.Read<ulong>(0);
            var entry = result.Read<uint>(1);

            var gInfo = GetGameObjectTemplate(entry);

            if (gInfo == null)
            {
                Log.Logger.Error("Table `gameobject` has gameobject (GUID: {0}) with non existing gameobject entry {1}, skipped.", guid, entry);

                continue;
            }

            if (gInfo.displayId == 0)
                switch (gInfo.type)
                {
                    case GameObjectTypes.Trap:
                    case GameObjectTypes.SpellFocus:
                        break;
                    default:
                        Log.Logger.Error("Gameobject (GUID: {0} Entry {1} GoType: {2}) doesn't have a displayId ({3}), not loaded.", guid, entry, gInfo.type, gInfo.displayId);

                        break;
                }

            if (gInfo.displayId != 0 && !_cliDB.GameObjectDisplayInfoStorage.ContainsKey(gInfo.displayId))
            {
                Log.Logger.Error("Gameobject (GUID: {0} Entry {1} GoType: {2}) has an invalid displayId ({3}), not loaded.", guid, entry, gInfo.type, gInfo.displayId);

                continue;
            }

            GameObjectData data = new()
            {
                SpawnId = guid,
                Id = entry,
                MapId = result.Read<ushort>(2),
                SpawnPoint = new Position(result.Read<float>(3), result.Read<float>(4), result.Read<float>(5), result.Read<float>(6))
            };

            data.Rotation.X = result.Read<float>(7);
            data.Rotation.Y = result.Read<float>(8);
            data.Rotation.Z = result.Read<float>(9);
            data.Rotation.W = result.Read<float>(10);
            data.spawntimesecs = result.Read<int>(11);
            data.SpawnGroupData = IsTransportMap(data.MapId) ? GetLegacySpawnGroup() : GetDefaultSpawnGroup(); // transport spawns default to compatibility group

            var mapEntry = _cliDB.MapStorage.LookupByKey(data.MapId);

            if (mapEntry == null)
            {
                Log.Logger.Error("Table `gameobject` has gameobject (GUID: {0} Entry: {1}) spawned on a non-existed map (Id: {2}), skip", guid, data.Id, data.MapId);

                continue;
            }

            if (data.spawntimesecs == 0 && gInfo.IsDespawnAtAction())
                Log.Logger.Error("Table `gameobject` has gameobject (GUID: {0} Entry: {1}) with `spawntimesecs` (0) value, but the gameobejct is marked as despawnable at action.", guid, data.Id);

            data.Animprogress = result.Read<uint>(12);
            data.ArtKit = 0;

            var gostate = result.Read<uint>(13);

            if (gostate >= (uint)GameObjectState.Max)
                if (gInfo.type != GameObjectTypes.Transport || gostate > (int)GameObjectState.TransportActive + SharedConst.MaxTransportStopFrames)
                {
                    Log.Logger.Error("Table `gameobject` has gameobject (GUID: {0} Entry: {1}) with invalid `state` ({2}) value, skip", guid, data.Id, gostate);

                    continue;
                }

            data.GoState = (GameObjectState)gostate;

            data.SpawnDifficulties = ParseSpawnDifficulties(result.Read<string>(14), "gameobject", guid, data.MapId, spawnMasks.LookupByKey(data.MapId));

            if (data.SpawnDifficulties.Empty())
            {
                Log.Logger.Error($"Table `creature` has creature (GUID: {guid}) that is not spawned in any difficulty, skipped.");

                continue;
            }

            short gameEvent = result.Read<sbyte>(15);
            data.poolId = result.Read<uint>(16);
            data.PhaseUseFlags = (PhaseUseFlagsValues)result.Read<byte>(17);
            data.PhaseId = result.Read<uint>(18);
            data.PhaseGroup = result.Read<uint>(19);

            if (Convert.ToBoolean(data.PhaseUseFlags & ~PhaseUseFlagsValues.All))
            {
                Log.Logger.Error("Table `gameobject` have gameobject (GUID: {0} Entry: {1}) has unknown `phaseUseFlags` set, removed unknown value.", guid, data.Id);
                data.PhaseUseFlags &= PhaseUseFlagsValues.All;
            }

            if (data.PhaseUseFlags.HasAnyFlag(PhaseUseFlagsValues.AlwaysVisible) && data.PhaseUseFlags.HasAnyFlag(PhaseUseFlagsValues.Inverse))
            {
                Log.Logger.Error("Table `gameobject` have gameobject (GUID: {0} Entry: {1}) has both `phaseUseFlags` PHASE_USE_FLAGS_ALWAYS_VISIBLE and PHASE_USE_FLAGS_INVERSE," +
                                 " removing PHASE_USE_FLAGS_INVERSE.",
                                 guid,
                                 data.Id);

                data.PhaseUseFlags &= ~PhaseUseFlagsValues.Inverse;
            }

            if (data.PhaseGroup != 0 && data.PhaseId != 0)
            {
                Log.Logger.Error("Table `gameobject` have gameobject (GUID: {0} Entry: {1}) with both `phaseid` and `phasegroup` set, `phasegroup` set to 0", guid, data.Id);
                data.PhaseGroup = 0;
            }

            if (data.PhaseId != 0)
                if (!_cliDB.PhaseStorage.ContainsKey(data.PhaseId))
                {
                    Log.Logger.Error("Table `gameobject` have gameobject (GUID: {0} Entry: {1}) with `phaseid` {2} does not exist, set to 0", guid, data.Id, data.PhaseId);
                    data.PhaseId = 0;
                }

            if (data.PhaseGroup != 0)
                if (Global.DB2Mgr.GetPhasesForGroup(data.PhaseGroup).Empty())
                {
                    Log.Logger.Error("Table `gameobject` have gameobject (GUID: {0} Entry: {1}) with `phaseGroup` {2} does not exist, set to 0", guid, data.Id, data.PhaseGroup);
                    data.PhaseGroup = 0;
                }

            data.terrainSwapMap = result.Read<int>(20);

            if (data.terrainSwapMap != -1)
            {
                var terrainSwapEntry = _cliDB.MapStorage.LookupByKey(data.terrainSwapMap);

                if (terrainSwapEntry == null)
                {
                    Log.Logger.Error("Table `gameobject` have gameobject (GUID: {0} Entry: {1}) with `terrainSwapMap` {2} does not exist, set to -1", guid, data.Id, data.terrainSwapMap);
                    data.terrainSwapMap = -1;
                }
                else if (terrainSwapEntry.ParentMapID != data.MapId)
                {
                    Log.Logger.Error("Table `gameobject` have gameobject (GUID: {0} Entry: {1}) with `terrainSwapMap` {2} which cannot be used on spawn map, set to -1", guid, data.Id, data.terrainSwapMap);
                    data.terrainSwapMap = -1;
                }
            }

            data.ScriptId = GetScriptId(result.Read<string>(21));

            if (data.Rotation.X is < -1.0f or > 1.0f)
            {
                Log.Logger.Error("Table `gameobject` has gameobject (GUID: {0} Entry: {1}) with invalid rotationX ({2}) value, skip", guid, data.Id, data.Rotation.X);

                continue;
            }

            if (data.Rotation.Y is < -1.0f or > 1.0f)
            {
                Log.Logger.Error("Table `gameobject` has gameobject (GUID: {0} Entry: {1}) with invalid rotationY ({2}) value, skip", guid, data.Id, data.Rotation.Y);

                continue;
            }

            if (data.Rotation.Z is < -1.0f or > 1.0f)
            {
                Log.Logger.Error("Table `gameobject` has gameobject (GUID: {0} Entry: {1}) with invalid rotationZ ({2}) value, skip", guid, data.Id, data.Rotation.Z);

                continue;
            }

            if (data.Rotation.W is < -1.0f or > 1.0f)
            {
                Log.Logger.Error("Table `gameobject` has gameobject (GUID: {0} Entry: {1}) with invalid rotationW ({2}) value, skip", guid, data.Id, data.Rotation.W);

                continue;
            }

            if (!GridDefines.IsValidMapCoord(data.MapId, data.SpawnPoint))
            {
                Log.Logger.Error("Table `gameobject` has gameobject (GUID: {0} Entry: {1}) with invalid coordinates, skip", guid, data.Id);

                continue;
            }

            if (!(Math.Abs(Quaternion.Dot(data.Rotation, data.Rotation) - 1) < 1e-5))
            {
                Log.Logger.Error($"Table `gameobject` has gameobject (GUID: {guid} Entry: {data.Id}) with invalid rotation quaternion (non-unit), defaulting to orientation on Z axis only");
                data.Rotation = Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(data.SpawnPoint.Orientation, 0f, 0f));
            }

            if (_configuration.GetDefaultValue("Calculate.Gameoject.Zone.Area.Data", false))
            {
                PhasingHandler.InitDbVisibleMapId(phaseShift, data.terrainSwapMap);
                Global.TerrainMgr.GetZoneAndAreaId(phaseShift, out var zoneId, out var areaId, data.MapId, data.SpawnPoint);

                var stmt = _worldDatabase.GetPreparedStatement(WorldStatements.UPD_GAMEOBJECT_ZONE_AREA_DATA);
                stmt.AddValue(0, zoneId);
                stmt.AddValue(1, areaId);
                stmt.AddValue(2, guid);
                _worldDatabase.Execute(stmt);
            }

            // if not this is to be managed by GameEvent System
            if (gameEvent == 0)
                AddGameObjectToGrid(data);

            _gameObjectDataStorage[guid] = data;
            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} gameobjects in {1} ms", count, Time.GetMSTimeDiffToNow(time));
    }

    //GameObjects
    public void LoadGameObjectTemplate()
    {
        var time = Time.MSTime;

        foreach (var db2go in _cliDB.GameObjectsStorage.Values)
        {
            GameObjectTemplate go = new()
            {
                entry = db2go.Id,
                type = db2go.TypeID,
                displayId = db2go.DisplayID,
                name = db2go.Name[Global.WorldMgr.DefaultDbcLocale],
                size = db2go.Scale
            };

            unsafe
            {
                for (byte x = 0; x < db2go.PropValue.Length; ++x)
                    go.Raw.data[x] = db2go.PropValue[x];
            }

            go.ContentTuningId = 0;
            go.ScriptId = 0;

            _gameObjectTemplateStorage[db2go.Id] = go;
        }

        //                                          0      1     2          3     4         5               6     7
        var result = _worldDatabase.Query("SELECT entry, type, displayId, name, IconName, castBarCaption, unk1, size, " +
                                          //8      9      10     11     12     13     14     15     16     17     18      19      20
                                          "Data0, Data1, Data2, Data3, Data4, Data5, Data6, Data7, Data8, Data9, Data10, Data11, Data12, " +
                                          //21      22      23      24      25      26      27      28      29      30      31      32      33      34      35      36
                                          "Data13, Data14, Data15, Data16, Data17, Data18, Data19, Data20, Data21, Data22, Data23, Data24, Data25, Data26, Data27, Data28, " +
                                          //37      38       39     40      41      42      43               44      45
                                          "Data29, Data30, Data31, Data32, Data33, Data34, ContentTuningId, AIName, ScriptName FROM gameobject_template");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 gameobject definitions. DB table `gameobject_template` is empty.");
        }
        else
        {
            do
            {
                var entry = result.Read<uint>(0);

                GameObjectTemplate got = new()
                {
                    entry = entry,
                    type = (GameObjectTypes)result.Read<uint>(1),
                    displayId = result.Read<uint>(2),
                    name = result.Read<string>(3),
                    IconName = result.Read<string>(4),
                    castBarCaption = result.Read<string>(5),
                    unk1 = result.Read<string>(6),
                    size = result.Read<float>(7)
                };

                unsafe
                {
                    for (byte x = 0; x < SharedConst.MaxGOData; ++x)
                        got.Raw.data[x] = result.Read<int>(8 + x);
                }

                got.ContentTuningId = result.Read<uint>(43);
                got.AIName = result.Read<string>(44);
                got.ScriptId = GetScriptId(result.Read<string>(45));

                switch (got.type)
                {
                    case GameObjectTypes.Door: //0
                        if (got.Door.open != 0)
                            CheckGOLockId(got, got.Door.open, 1);

                        CheckGONoDamageImmuneId(got, got.Door.noDamageImmune, 3);

                        break;
                    case GameObjectTypes.Button: //1
                        if (got.Button.open != 0)
                            CheckGOLockId(got, got.Button.open, 1);

                        CheckGONoDamageImmuneId(got, got.Button.noDamageImmune, 4);

                        break;
                    case GameObjectTypes.QuestGiver: //2
                        if (got.QuestGiver.open != 0)
                            CheckGOLockId(got, got.QuestGiver.open, 0);

                        CheckGONoDamageImmuneId(got, got.QuestGiver.noDamageImmune, 5);

                        break;
                    case GameObjectTypes.Chest: //3
                        if (got.Chest.open != 0)
                            CheckGOLockId(got, got.Chest.open, 0);

                        CheckGOConsumable(got, got.Chest.consumable, 3);

                        if (got.Chest.linkedTrap != 0) // linked trap
                            CheckGOLinkedTrapId(got, got.Chest.linkedTrap, 7);

                        break;
                    case GameObjectTypes.Trap: //6
                        if (got.Trap.open != 0)
                            CheckGOLockId(got, got.Trap.open, 0);

                        break;
                    case GameObjectTypes.Chair: //7
                        CheckAndFixGOChairHeightId(got, ref got.Chair.chairheight, 1);

                        break;
                    case GameObjectTypes.SpellFocus: //8
                        if (got.SpellFocus.spellFocusType != 0)
                            if (!_cliDB.SpellFocusObjectStorage.ContainsKey(got.SpellFocus.spellFocusType))
                                Log.Logger.Error("GameObject (Entry: {0} GoType: {1}) have data0={2} but SpellFocus (Id: {3}) not exist.",
                                                 entry,
                                                 got.type,
                                                 got.SpellFocus.spellFocusType,
                                                 got.SpellFocus.spellFocusType);

                        if (got.SpellFocus.linkedTrap != 0) // linked trap
                            CheckGOLinkedTrapId(got, got.SpellFocus.linkedTrap, 2);

                        break;
                    case GameObjectTypes.Goober: //10
                        if (got.Goober.open != 0)
                            CheckGOLockId(got, got.Goober.open, 0);

                        CheckGOConsumable(got, got.Goober.consumable, 3);

                        if (got.Goober.pageID != 0) // pageId
                            if (GetPageText(got.Goober.pageID) == null)
                                Log.Logger.Error("GameObject (Entry: {0} GoType: {1}) have data7={2} but PageText (Entry {3}) not exist.", entry, got.type, got.Goober.pageID, got.Goober.pageID);

                        CheckGONoDamageImmuneId(got, got.Goober.noDamageImmune, 11);

                        if (got.Goober.linkedTrap != 0) // linked trap
                            CheckGOLinkedTrapId(got, got.Goober.linkedTrap, 12);

                        break;
                    case GameObjectTypes.AreaDamage: //12
                        if (got.AreaDamage.open != 0)
                            CheckGOLockId(got, got.AreaDamage.open, 0);

                        break;
                    case GameObjectTypes.Camera: //13
                        if (got.Camera.open != 0)
                            CheckGOLockId(got, got.Camera.open, 0);

                        break;
                    case GameObjectTypes.MapObjTransport: //15
                    {
                        if (got.MoTransport.taxiPathID != 0)
                            if (got.MoTransport.taxiPathID >= _cliDB.TaxiPathNodesByPath.Count || _cliDB.TaxiPathNodesByPath[got.MoTransport.taxiPathID].Empty())
                                Log.Logger.Error("GameObject (Entry: {0} GoType: {1}) have data0={2} but TaxiPath (Id: {3}) not exist.",
                                                 entry,
                                                 got.type,
                                                 got.MoTransport.taxiPathID,
                                                 got.MoTransport.taxiPathID);

                        var transportMap = got.MoTransport.SpawnMap;

                        if (transportMap != 0)
                            _transportMaps.Add((ushort)transportMap);

                        break;
                    }
                    case GameObjectTypes.SpellCaster: //22
                        // always must have spell
                        CheckGOSpellId(got, got.SpellCaster.spell, 0);

                        break;
                    case GameObjectTypes.FlagStand: //24
                        if (got.FlagStand.open != 0)
                            CheckGOLockId(got, got.FlagStand.open, 0);

                        CheckGONoDamageImmuneId(got, got.FlagStand.noDamageImmune, 5);

                        break;
                    case GameObjectTypes.FishingHole: //25
                        if (got.FishingHole.open != 0)
                            CheckGOLockId(got, got.FishingHole.open, 4);

                        break;
                    case GameObjectTypes.FlagDrop: //26
                        if (got.FlagDrop.open != 0)
                            CheckGOLockId(got, got.FlagDrop.open, 0);

                        CheckGONoDamageImmuneId(got, got.FlagDrop.noDamageImmune, 3);

                        break;
                    case GameObjectTypes.BarberChair: //32
                        CheckAndFixGOChairHeightId(got, ref got.BarberChair.chairheight, 0);

                        if (got.BarberChair.SitAnimKit != 0 && !_cliDB.AnimKitStorage.ContainsKey(got.BarberChair.SitAnimKit))
                        {
                            Log.Logger.Error("GameObject (Entry: {0} GoType: {1}) have data2 = {2} but AnimKit.dbc (Id: {3}) not exist, set to 0.",
                                             entry,
                                             got.type,
                                             got.BarberChair.SitAnimKit,
                                             got.BarberChair.SitAnimKit);

                            got.BarberChair.SitAnimKit = 0;
                        }

                        break;
                    case GameObjectTypes.GarrisonBuilding:
                    {
                        var transportMap = got.GarrisonBuilding.SpawnMap;

                        if (transportMap != 0)
                            _transportMaps.Add((ushort)transportMap);
                    }

                    break;
                    case GameObjectTypes.GatheringNode:
                        if (got.GatheringNode.open != 0)
                            CheckGOLockId(got, got.GatheringNode.open, 0);

                        if (got.GatheringNode.linkedTrap != 0)
                            CheckGOLinkedTrapId(got, got.GatheringNode.linkedTrap, 20);

                        break;
                }

                _gameObjectTemplateStorage[entry] = got;
            } while (result.NextRow());

            Log.Logger.Information("Loaded {0} GameInfo object templates in {1} ms", _gameObjectTemplateStorage.Count, Time.GetMSTimeDiffToNow(time));
        }
    }

    public void LoadGameObjectTemplateAddons()
    {
        var oldMSTime = Time.MSTime;

        //                                         0       1       2      3        4        5        6        7        8        9        10             11
        var result = _worldDatabase.Query("SELECT entry, faction, flags, mingold, maxgold, artkit0, artkit1, artkit2, artkit3, artkit4, WorldEffectID, AIAnimKitID FROM gameobject_template_addon");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 gameobject template addon definitions. DB table `gameobject_template_addon` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var entry = result.Read<uint>(0);

            var got = GetGameObjectTemplate(entry);

            if (got == null)
            {
                if (_configuration.GetDefaultValue("load.autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM gameobject_template_addon WHERE entry = {entry}");
                else
                    Log.Logger.Error($"GameObject template (Entry: {entry}) does not exist but has a record in `gameobject_template_addon`");

                continue;
            }

            GameObjectTemplateAddon gameObjectAddon = new()
            {
                Faction = result.Read<ushort>(1),
                Flags = (GameObjectFlags)result.Read<uint>(2),
                Mingold = result.Read<uint>(3),
                Maxgold = result.Read<uint>(4),
                WorldEffectId = result.Read<uint>(10),
                AiAnimKitId = result.Read<uint>(11)
            };

            for (var i = 0; i < gameObjectAddon.ArtKits.Length; ++i)
            {
                var artKitID = result.Read<uint>(5 + i);

                if (artKitID == 0)
                    continue;

                if (!_cliDB.GameObjectArtKitStorage.ContainsKey(artKitID))
                {
                    Log.Logger.Error($"GameObject (Entry: {entry}) has invalid `artkit{i}` ({artKitID}) defined, set to zero instead.");

                    continue;
                }

                gameObjectAddon.ArtKits[i] = artKitID;
            }

            // checks
            if (gameObjectAddon.Faction != 0 && !_cliDB.FactionTemplateStorage.ContainsKey(gameObjectAddon.Faction))
                Log.Logger.Error($"GameObject (Entry: {entry}) has invalid faction ({gameObjectAddon.Faction}) defined in `gameobject_template_addon`.");

            if (gameObjectAddon.Maxgold > 0)
                switch (got.type)
                {
                    case GameObjectTypes.Chest:
                    case GameObjectTypes.FishingHole:
                        break;
                    default:
                        Log.Logger.Error($"GameObject (Entry {entry} GoType: {got.type}) cannot be looted but has maxgold set in `gameobject_template_addon`.");

                        break;
                }

            if (gameObjectAddon.WorldEffectId != 0 && !_cliDB.WorldEffectStorage.ContainsKey(gameObjectAddon.WorldEffectId))
            {
                Log.Logger.Error($"GameObject (Entry: {entry}) has invalid WorldEffectID ({gameObjectAddon.WorldEffectId}) defined in `gameobject_template_addon`, set to 0.");
                gameObjectAddon.WorldEffectId = 0;
            }

            if (gameObjectAddon.AiAnimKitId != 0 && !_cliDB.AnimKitStorage.ContainsKey(gameObjectAddon.AiAnimKitId))
            {
                Log.Logger.Error($"GameObject (Entry: {entry}) has invalid AIAnimKitID ({gameObjectAddon.AiAnimKitId}) defined in `gameobject_template_addon`, set to 0.");
                gameObjectAddon.AiAnimKitId = 0;
            }

            _gameObjectTemplateAddonStorage[entry] = gameObjectAddon;
            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} GameInfo object template addons in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadGameTele()
    {
        var oldMSTime = Time.MSTime;

        GameTeleStorage.Clear();

        //                                          0       1           2           3           4        5     6
        var result = _worldDatabase.Query("SELECT id, position_x, position_y, position_z, orientation, map, name FROM game_tele");

        if (result.IsEmpty())
        {
            Log.Logger.Error("Loaded 0 GameTeleports. DB table `game_tele` is empty!");

            return;
        }

        uint count = 0;

        do
        {
            var id = result.Read<uint>(0);

            GameTele gt = new()
            {
                PosX = result.Read<float>(1),
                PosY = result.Read<float>(2),
                PosZ = result.Read<float>(3),
                Orientation = result.Read<float>(4),
                MapId = result.Read<uint>(5),
                Name = result.Read<string>(6)
            };

            gt.NameLow = gt.Name.ToLowerInvariant();

            if (!GridDefines.IsValidMapCoord(gt.MapId, gt.PosX, gt.PosY, gt.PosZ, gt.Orientation))
            {
                Log.Logger.Error("Wrong position for id {0} (name: {1}) in `game_tele` table, ignoring.", id, gt.Name);

                continue;
            }

            GameTeleStorage.Add(id, gt);
            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} GameTeleports in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    //Gossip
    public void LoadGossipMenu()
    {
        var oldMSTime = Time.MSTime;

        _gossipMenusStorage.Clear();

        var result = _worldDatabase.Query("SELECT MenuId, TextId FROM gossip_menu");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 gossip_menu entries. DB table `gossip_menu` is empty!");

            return;
        }

        do
        {
            GossipMenus gMenu = new()
            {
                MenuId = result.Read<uint>(0),
                TextId = result.Read<uint>(1)
            };

            if (GetNpcText(gMenu.TextId) == null)
            {
                if (_configuration.GetDefaultValue("load.autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM gossip_menu WHERE MenuID = {gMenu.MenuId}");
                else
                    Log.Logger.Error("Table gossip_menu: Id {0} is using non-existing TextId {1}", gMenu.MenuId, gMenu.TextId);

                continue;
            }

            _gossipMenusStorage.Add(gMenu.MenuId, gMenu);
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} gossip_menu Ids in {1} ms", _gossipMenusStorage.Count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadGossipMenuAddon()
    {
        var oldMSTime = Time.MSTime;

        _gossipMenuAddonStorage.Clear();

        //                                         0       1
        var result = _worldDatabase.Query("SELECT MenuID, FriendshipFactionID FROM gossip_menu_addon");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 gossip_menu_addon IDs. DB table `gossip_menu_addon` is empty!");

            return;
        }

        do
        {
            var menuID = result.Read<uint>(0);

            GossipMenuAddon addon = new()
            {
                FriendshipFactionId = result.Read<int>(1)
            };

            var faction = _cliDB.FactionStorage.LookupByKey(addon.FriendshipFactionId);

            if (faction != null)
            {
                if (!_cliDB.FriendshipReputationStorage.ContainsKey(faction.FriendshipRepID))
                {
                    Log.Logger.Error($"Table gossip_menu_addon: ID {menuID} is using FriendshipFactionID {addon.FriendshipFactionId} referencing non-existing FriendshipRepID {faction.FriendshipRepID}");
                    addon.FriendshipFactionId = 0;
                }
            }
            else
            {
                Log.Logger.Error($"Table gossip_menu_addon: ID {menuID} is using non-existing FriendshipFactionID {addon.FriendshipFactionId}");
                addon.FriendshipFactionId = 0;
            }

            _gossipMenuAddonStorage[menuID] = addon;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {_gossipMenuAddonStorage.Count} gossip_menu_addon IDs in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    public void LoadGossipMenuItems()
    {
        var oldMSTime = Time.MSTime;

        _gossipMenuItemsStorage.Clear();

        //                                         0       1               2         3          4           5                      6         7      8             9            10
        var result = _worldDatabase.Query("SELECT MenuID, GossipOptionID, OptionID, OptionNpc, OptionText, OptionBroadcastTextID, Language, Flags, ActionMenuID, ActionPoiID, GossipNpcOptionID, " +
                                          //11        12        13       14                  15       16
                                          "BoxCoded, BoxMoney, BoxText, BoxBroadcastTextID, SpellID, OverrideIconID FROM gossip_menu_option ORDER BY MenuID, OptionID");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 gossip_menu_option Ids. DB table `gossip_menu_option` is empty!");

            return;
        }

        Dictionary<int, uint> optionToNpcOption = new();

        foreach (var (_, npcOption) in _cliDB.GossipNPCOptionStorage)
            optionToNpcOption[npcOption.GossipOptionID] = npcOption.Id;

        do
        {
            GossipMenuItems gMenuItem = new()
            {
                MenuId = result.Read<uint>(0),
                GossipOptionId = result.Read<int>(1),
                OrderIndex = result.Read<uint>(2),
                OptionNpc = (GossipOptionNpc)result.Read<byte>(3),
                OptionText = result.Read<string>(4),
                OptionBroadcastTextId = result.Read<uint>(5),
                Language = result.Read<uint>(6),
                Flags = (GossipOptionFlags)result.Read<int>(7),
                ActionMenuId = result.Read<uint>(8),
                ActionPoiId = result.Read<uint>(9)
            };

            if (!result.IsNull(10))
                gMenuItem.GossipNpcOptionId = result.Read<int>(10);

            gMenuItem.BoxCoded = result.Read<bool>(11);
            gMenuItem.BoxMoney = result.Read<uint>(12);
            gMenuItem.BoxText = result.Read<string>(13);
            gMenuItem.BoxBroadcastTextId = result.Read<uint>(14);

            if (!result.IsNull(15))
                gMenuItem.SpellId = result.Read<int>(15);

            if (!result.IsNull(16))
                gMenuItem.OverrideIconId = result.Read<int>(16);

            if (gMenuItem.OptionNpc >= GossipOptionNpc.Max)
            {
                Log.Logger.Error($"Table `gossip_menu_option` for menu {gMenuItem.MenuId}, id {gMenuItem.OrderIndex} has unknown NPC option id {gMenuItem.OptionNpc}. Replacing with GossipOptionNpc.None");
                gMenuItem.OptionNpc = GossipOptionNpc.None;
            }

            if (gMenuItem.OptionBroadcastTextId != 0)
                if (!_cliDB.BroadcastTextStorage.ContainsKey(gMenuItem.OptionBroadcastTextId))
                {
                    if (_configuration.GetDefaultValue("load.autoclean", false))
                        _worldDatabase.Execute($"UPDATE gossip_menu_option SET OptionBroadcastTextID = 0 WHERE MenuID = {gMenuItem.MenuId}");
                    else
                        Log.Logger.Error($"Table `gossip_menu_option` for MenuId {gMenuItem.MenuId}, OptionIndex {gMenuItem.OrderIndex} has non-existing or incompatible OptionBroadcastTextId {gMenuItem.OptionBroadcastTextId}, ignoring.");

                    gMenuItem.OptionBroadcastTextId = 0;
                }

            if (gMenuItem.Language != 0 && !_cliDB.LanguagesStorage.ContainsKey(gMenuItem.Language))
            {
                if (_configuration.GetDefaultValue("load.autoclean", false))
                    _worldDatabase.Execute($"UPDATE gossip_menu_option SET OptionID = 0 WHERE MenuID = {gMenuItem.MenuId}");
                else
                    Log.Logger.Error($"Table `gossip_menu_option` for menu {gMenuItem.MenuId}, id {gMenuItem.OrderIndex} use non-existing Language {gMenuItem.Language}, ignoring");

                gMenuItem.Language = 0;
            }

            if (gMenuItem.ActionMenuId != 0 && gMenuItem.OptionNpc != GossipOptionNpc.None)
            {
                if (_configuration.GetDefaultValue("load.autoclean", false))
                    _worldDatabase.Execute($"UPDATE gossip_menu_option SET ActionMenuID = 0 WHERE MenuID = {gMenuItem.MenuId}");
                else
                    Log.Logger.Error($"Table `gossip_menu_option` for menu {gMenuItem.MenuId}, id {gMenuItem.OrderIndex} can not use ActionMenuID for GossipOptionNpc different from GossipOptionNpc.None, ignoring");

                gMenuItem.ActionMenuId = 0;
            }

            if (gMenuItem.ActionPoiId != 0)
            {
                if (gMenuItem.OptionNpc != GossipOptionNpc.None)
                {
                    if (_configuration.GetDefaultValue("load.autoclean", false))
                        _worldDatabase.Execute($"UPDATE gossip_menu_option SET ActionPoiID = 0 WHERE MenuID = {gMenuItem.MenuId}");
                    else
                        Log.Logger.Error($"Table `gossip_menu_option` for menu {gMenuItem.MenuId}, id {gMenuItem.OrderIndex} can not use ActionPoiID for GossipOptionNpc different from GossipOptionNpc.None, ignoring");

                    gMenuItem.ActionPoiId = 0;
                }
                else if (GetPointOfInterest(gMenuItem.ActionPoiId) == null)
                {
                    if (_configuration.GetDefaultValue("load.autoclean", false))
                        _worldDatabase.Execute($"UPDATE gossip_menu_option SET ActionPoiID = 0 WHERE MenuID = {gMenuItem.MenuId}");
                    else
                        Log.Logger.Error($"Table `gossip_menu_option` for menu {gMenuItem.MenuId}, id {gMenuItem.OrderIndex} use non-existing ActionPoiID {gMenuItem.ActionPoiId}, ignoring");

                    gMenuItem.ActionPoiId = 0;
                }
            }

            if (gMenuItem.GossipNpcOptionId.HasValue)
            {
                if (!_cliDB.GossipNPCOptionStorage.ContainsKey(gMenuItem.GossipNpcOptionId.Value))
                {
                    if (_configuration.GetDefaultValue("load.autoclean", false))
                        _worldDatabase.Execute($"UPDATE gossip_menu_option SET GossipNpcOptionID = 0 WHERE MenuID = {gMenuItem.MenuId}");
                    else
                        Log.Logger.Error($"Table `gossip_menu_option` for menu {gMenuItem.MenuId}, id {gMenuItem.OrderIndex} use non-existing GossipNPCOption {gMenuItem.GossipNpcOptionId}, ignoring");

                    gMenuItem.GossipNpcOptionId = null;
                }
            }
            else
            {
                var npcOptionId = optionToNpcOption.LookupByKey(gMenuItem.GossipOptionId);

                if (npcOptionId != 0)
                    gMenuItem.GossipNpcOptionId = (int)npcOptionId;
            }

            if (gMenuItem.BoxBroadcastTextId != 0)
                if (!_cliDB.BroadcastTextStorage.ContainsKey(gMenuItem.BoxBroadcastTextId))
                {
                    if (_configuration.GetDefaultValue("load.autoclean", false))
                        _worldDatabase.Execute($"UPDATE gossip_menu_option SET BoxBroadcastTextID = 0 WHERE MenuID = {gMenuItem.MenuId}");
                    else
                        Log.Logger.Error($"Table `gossip_menu_option` for MenuId {gMenuItem.MenuId}, OptionIndex {gMenuItem.OrderIndex} has non-existing or incompatible BoxBroadcastTextId {gMenuItem.BoxBroadcastTextId}, ignoring.");

                    gMenuItem.BoxBroadcastTextId = 0;
                }

            if (gMenuItem.SpellId.HasValue)
                if (!Global.SpellMgr.HasSpellInfo((uint)gMenuItem.SpellId.Value, Difficulty.None))
                {
                    if (_configuration.GetDefaultValue("load.autoclean", false))
                        _worldDatabase.Execute($"UPDATE gossip_menu_option SET SpellID = 0 WHERE MenuID = {gMenuItem.MenuId}");
                    else
                        Log.Logger.Error($"Table `gossip_menu_option` for menu {gMenuItem.MenuId}, id {gMenuItem.OrderIndex} use non-existing Spell {gMenuItem.SpellId}, ignoring");

                    gMenuItem.SpellId = null;
                }

            _gossipMenuItemsStorage.Add(gMenuItem.MenuId, gMenuItem);
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {_gossipMenuItemsStorage.Count} gossip_menu_option entries in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    public void LoadGossipMenuItemsLocales()
    {
        var oldMSTime = Time.MSTime;

        _gossipMenuItemsLocaleStorage.Clear(); // need for reload case

        //                                         0       1            2       3           4
        var result = _worldDatabase.Query("SELECT MenuId, OptionID, Locale, OptionText, BoxText FROM gossip_menu_option_locale");

        if (result.IsEmpty())
            return;

        do
        {
            var menuId = result.Read<uint>(0);
            var optionIndex = result.Read<uint>(1);
            var localeName = result.Read<string>(2);

            var locale = localeName.ToEnum<Locale>();

            if (!SharedConst.IsValidLocale(locale) || locale == Locale.enUS)
                continue;

            GossipMenuItemsLocale data = new();
            AddLocaleString(result.Read<string>(3), locale, data.OptionText);
            AddLocaleString(result.Read<string>(4), locale, data.BoxText);

            _gossipMenuItemsLocaleStorage[Tuple.Create(menuId, optionIndex)] = data;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} gossip_menu_option locale strings in {1} ms", _gossipMenuItemsLocaleStorage.Count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadGraveyardZones()
    {
        var oldMSTime = Time.MSTime;

        GraveYardStorage.Clear(); // need for reload case

        //                                         0       1         2
        var result = _worldDatabase.Query("SELECT ID, GhostZone, faction FROM graveyard_zone");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 graveyard-zone links. DB table `graveyard_zone` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            ++count;
            var safeLocId = result.Read<uint>(0);
            var zoneId = result.Read<uint>(1);
            var team = (TeamFaction)result.Read<uint>(2);

            var entry = GetWorldSafeLoc(safeLocId);

            if (entry == null)
            {
                Log.Logger.Error("Table `graveyard_zone` has a record for not existing graveyard (WorldSafeLocs.dbc id) {0}, skipped.", safeLocId);

                continue;
            }

            var areaEntry = _cliDB.AreaTableStorage.LookupByKey(zoneId);

            if (areaEntry == null)
            {
                Log.Logger.Error("Table `graveyard_zone` has a record for not existing zone id ({0}), skipped.", zoneId);

                continue;
            }

            if (team != 0 && team != TeamFaction.Horde && team != TeamFaction.Alliance)
            {
                Log.Logger.Error("Table `graveyard_zone` has a record for non player faction ({0}), skipped.", team);

                continue;
            }

            if (!AddGraveYardLink(safeLocId, zoneId, team, false))
                Log.Logger.Error("Table `graveyard_zone` has a duplicate record for Graveyard (ID: {0}) and Zone (ID: {1}), skipped.", safeLocId, zoneId);
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} graveyard-zone links in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadInstanceEncounters()
    {
        var oldMSTime = Time.MSTime;

        //                                           0         1            2                3
        var result = _worldDatabase.Query("SELECT entry, creditType, creditEntry, lastEncounterDungeon FROM instance_encounters");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 instance encounters, table is empty!");

            return;
        }

        uint count = 0;
        Dictionary<uint, Tuple<uint, DungeonEncounterRecord>> dungeonLastBosses = new();

        do
        {
            var entry = result.Read<uint>(0);
            var creditType = (EncounterCreditType)result.Read<byte>(1);
            var creditEntry = result.Read<uint>(2);
            var lastEncounterDungeon = result.Read<uint>(3);
            var dungeonEncounter = _cliDB.DungeonEncounterStorage.LookupByKey(entry);

            if (dungeonEncounter == null)
            {
                Log.Logger.Error("Table `instance_encounters` has an invalid encounter id {0}, skipped!", entry);

                continue;
            }

            if (lastEncounterDungeon != 0 && Global.LFGMgr.GetLFGDungeonEntry(lastEncounterDungeon) == 0)
            {
                Log.Logger.Error("Table `instance_encounters` has an encounter {0} ({1}) marked as final for invalid dungeon id {2}, skipped!",
                                 entry,
                                 dungeonEncounter.Name[Global.WorldMgr.DefaultDbcLocale],
                                 lastEncounterDungeon);

                continue;
            }

            var pair = dungeonLastBosses.LookupByKey(lastEncounterDungeon);

            if (lastEncounterDungeon != 0)
            {
                if (pair != null)
                {
                    Log.Logger.Error("Table `instance_encounters` specified encounter {0} ({1}) as last encounter but {2} ({3}) is already marked as one, skipped!",
                                     entry,
                                     dungeonEncounter.Name[Global.WorldMgr.DefaultDbcLocale],
                                     pair.Item1,
                                     pair.Item2.Name[Global.WorldMgr.DefaultDbcLocale]);

                    continue;
                }

                dungeonLastBosses[lastEncounterDungeon] = Tuple.Create(entry, dungeonEncounter);
            }

            switch (creditType)
            {
                case EncounterCreditType.KillCreature:
                {
                    var creatureInfo = GetCreatureTemplate(creditEntry);

                    if (creatureInfo == null)
                    {
                        Log.Logger.Error("Table `instance_encounters` has an invalid creature (entry {0}) linked to the encounter {1} ({2}), skipped!",
                                         creditEntry,
                                         entry,
                                         dungeonEncounter.Name[Global.WorldMgr.DefaultDbcLocale]);

                        continue;
                    }

                    creatureInfo.FlagsExtra |= CreatureFlagsExtra.DungeonBoss;

                    for (byte diff = 0; diff < SharedConst.MaxCreatureDifficulties; ++diff)
                    {
                        var diffEntry = creatureInfo.DifficultyEntry[diff];

                        if (diffEntry != 0)
                        {
                            var diffInfo = GetCreatureTemplate(diffEntry);

                            if (diffInfo != null)
                                diffInfo.FlagsExtra |= CreatureFlagsExtra.DungeonBoss;
                        }
                    }

                    break;
                }
                case EncounterCreditType.CastSpell:
                    if (!Global.SpellMgr.HasSpellInfo(creditEntry, Difficulty.None))
                    {
                        Log.Logger.Error("Table `instance_encounters` has an invalid spell (entry {0}) linked to the encounter {1} ({2}), skipped!",
                                         creditEntry,
                                         entry,
                                         dungeonEncounter.Name[Global.WorldMgr.DefaultDbcLocale]);

                        continue;
                    }

                    break;
                default:
                    Log.Logger.Error("Table `instance_encounters` has an invalid credit type ({0}) for encounter {1} ({2}), skipped!",
                                     creditType,
                                     entry,
                                     dungeonEncounter.Name[Global.WorldMgr.DefaultDbcLocale]);

                    continue;
            }

            if (dungeonEncounter.DifficultyID == 0)
            {
                foreach (var difficulty in _cliDB.DifficultyStorage.Values)
                    if (Global.DB2Mgr.GetMapDifficultyData((uint)dungeonEncounter.MapID, (Difficulty)difficulty.Id) != null)
                        _dungeonEncounterStorage.Add(MathFunctions.MakePair64((uint)dungeonEncounter.MapID, difficulty.Id), new DungeonEncounter(dungeonEncounter, creditType, creditEntry, lastEncounterDungeon));
            }
            else
            {
                _dungeonEncounterStorage.Add(MathFunctions.MakePair64((uint)dungeonEncounter.MapID, (uint)dungeonEncounter.DifficultyID), new DungeonEncounter(dungeonEncounter, creditType, creditEntry, lastEncounterDungeon));
            }

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} instance encounters in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadInstanceSpawnGroups()
    {
        var oldMSTime = Time.MSTime;

        //                                         0              1            2           3             4
        var result = _worldDatabase.Query("SELECT instanceMapId, bossStateId, bossStates, spawnGroupId, flags FROM instance_spawn_groups");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 instance spawn groups. DB table `instance_spawn_groups` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var instanceMapId = result.Read<ushort>(0);
            var spawnGroupId = result.Read<uint>(3);
            var spawnGroupTemplate = _spawnGroupDataStorage.LookupByKey(spawnGroupId);

            if (spawnGroupTemplate == null || spawnGroupTemplate.Flags.HasAnyFlag(SpawnGroupFlags.System))
            {
                if (_configuration.GetDefaultValue("load.autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM instance_spawn_groups WHERE instanceMapId = {instanceMapId} AND spawnGroupId = {spawnGroupId}");
                else
                    Log.Logger.Error($"Invalid spawn group {spawnGroupId} specified for instance {instanceMapId}. Skipped.");

                continue;
            }

            if (spawnGroupTemplate.MapId != instanceMapId)
            {
                if (_configuration.GetDefaultValue("load.autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM instance_spawn_groups WHERE instanceMapId = {instanceMapId} AND spawnGroupId = {spawnGroupId}");
                else
                    Log.Logger.Error($"Instance spawn group {spawnGroupId} specified for instance {instanceMapId} has spawns on a different map {spawnGroupTemplate.MapId}. Skipped.");

                continue;
            }

            InstanceSpawnGroupInfo info = new()
            {
                SpawnGroupId = spawnGroupId,
                BossStateId = result.Read<byte>(1)
            };

            byte ALL_STATES = (1 << (int)EncounterState.ToBeDecided) - 1;
            var states = result.Read<byte>(2);

            if ((states & ~ALL_STATES) != 0)
            {
                info.BossStates = (byte)(states & ALL_STATES);
                Log.Logger.Error($"Instance spawn group ({instanceMapId},{spawnGroupId}) had invalid boss state mask {states} - truncated to {info.BossStates}.");
            }
            else
            {
                info.BossStates = states;
            }

            var flags = (InstanceSpawnGroupFlags)result.Read<byte>(4);

            if ((flags & ~InstanceSpawnGroupFlags.All) != 0)
            {
                info.Flags = flags & InstanceSpawnGroupFlags.All;
                Log.Logger.Error($"Instance spawn group ({instanceMapId},{spawnGroupId}) had invalid flags {flags} - truncated to {info.Flags}.");
            }
            else
            {
                info.Flags = flags;
            }

            if (flags.HasFlag(InstanceSpawnGroupFlags.AllianceOnly) && flags.HasFlag(InstanceSpawnGroupFlags.HordeOnly))
            {
                info.Flags = flags & ~(InstanceSpawnGroupFlags.AllianceOnly | InstanceSpawnGroupFlags.HordeOnly);
                Log.Logger.Error($"Instance spawn group ({instanceMapId},{spawnGroupId}) FLAG_ALLIANCE_ONLY and FLAG_HORDE_ONLY may not be used together in a single entry - truncated to {info.Flags}.");
            }

            _instanceSpawnGroupStorage.Add(instanceMapId, info);

            ++count;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {count} instance spawn groups in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    //Maps
    public void LoadInstanceTemplate()
    {
        var time = Time.MSTime;

        //                                          0     1       2
        var result = _worldDatabase.Query("SELECT map, parent, script FROM instance_template");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 instance templates. DB table `instance_template` is empty!");

            return;
        }

        uint count = 0;

        do
        {
            var mapID = result.Read<uint>(0);

            if (!Global.MapMgr.IsValidMAP(mapID))
            {
                Log.Logger.Error("ObjectMgr.LoadInstanceTemplate: bad mapid {0} for template!", mapID);

                continue;
            }

            var instanceTemplate = new InstanceTemplate
            {
                Parent = result.Read<uint>(1),
                ScriptId = GetScriptId(result.Read<string>(2))
            };

            _instanceTemplateStorage.Add(mapID, instanceTemplate);

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} instance templates in {1} ms", count, Time.GetMSTimeDiffToNow(time));
    }

    public void LoadItemScriptNames()
    {
        var oldMSTime = Time.MSTime;
        uint count = 0;

        var result = _worldDatabase.Query("SELECT Id, ScriptName FROM item_script_names");

        if (!result.IsEmpty())
            do
            {
                var itemId = result.Read<uint>(0);

                if (GetItemTemplate(itemId) == null)
                {
                    Log.Logger.Error("Item {0} specified in `item_script_names` does not exist, skipped.", itemId);

                    continue;
                }

                _itemTemplateStorage[itemId].ScriptId = GetScriptId(result.Read<string>(1));
                ++count;
            } while (result.NextRow());

        Log.Logger.Information("Loaded {0} item script names in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadItemTemplateAddon()
    {
        var time = Time.MSTime;

        uint count = 0;
        var result = _worldDatabase.Query("SELECT Id, FlagsCu, FoodType, MinMoneyLoot, MaxMoneyLoot, SpellPPMChance, RandomBonusListTemplateId FROM item_template_addon");

        if (!result.IsEmpty())
            do
            {
                var itemId = result.Read<uint>(0);
                var itemTemplate = GetItemTemplate(itemId);

                if (itemTemplate == null)
                {
                    Log.Logger.Error("Item {0} specified in `itemtemplateaddon` does not exist, skipped.", itemId);

                    continue;
                }

                var minMoneyLoot = result.Read<uint>(3);
                var maxMoneyLoot = result.Read<uint>(4);

                if (minMoneyLoot > maxMoneyLoot)
                {
                    Log.Logger.Error("Minimum money loot specified in `itemtemplateaddon` for item {0} was greater than maximum amount, swapping.", itemId);
                    var temp = minMoneyLoot;
                    minMoneyLoot = maxMoneyLoot;
                    maxMoneyLoot = temp;
                }

                itemTemplate.FlagsCu = (ItemFlagsCustom)result.Read<uint>(1);
                itemTemplate.FoodType = result.Read<uint>(2);
                itemTemplate.MinMoneyLoot = minMoneyLoot;
                itemTemplate.MaxMoneyLoot = maxMoneyLoot;
                itemTemplate.SpellPPMRate = result.Read<float>(5);
                itemTemplate.RandomBonusListTemplateId = result.Read<uint>(6);
                ++count;
            } while (result.NextRow());

        Log.Logger.Information("Loaded {0} item addon templates in {1} ms", count, Time.GetMSTimeDiffToNow(time));
    }

    //Items
    public void LoadItemTemplates()
    {
        var oldMSTime = Time.MSTime;
        uint sparseCount = 0;

        foreach (var sparse in _cliDB.ItemSparseStorage.Values)
        {
            var db2Data = _cliDB.ItemStorage.LookupByKey(sparse.Id);

            if (db2Data == null)
                continue;

            var itemTemplate = new ItemTemplate(db2Data, sparse)
            {
                MaxDurability = FillMaxDurability(db2Data.ClassID, db2Data.SubclassID, sparse.inventoryType, (ItemQuality)sparse.OverallQualityID, sparse.ItemLevel)
            };

            var itemSpecOverrides = Global.DB2Mgr.GetItemSpecOverrides(sparse.Id);

            if (itemSpecOverrides != null)
            {
                foreach (var itemSpecOverride in itemSpecOverrides)
                {
                    var specialization = _cliDB.ChrSpecializationStorage.LookupByKey(itemSpecOverride.SpecID);

                    if (specialization != null)
                    {
                        itemTemplate.ItemSpecClassMask |= 1u << (specialization.ClassID - 1);
                        itemTemplate.Specializations[0].Set(ItemTemplate.CalculateItemSpecBit(specialization), true);

                        itemTemplate.Specializations[1] = itemTemplate.Specializations[1].Or(itemTemplate.Specializations[0]);
                        itemTemplate.Specializations[2] = itemTemplate.Specializations[2].Or(itemTemplate.Specializations[0]);
                    }
                }
            }
            else
            {
                ItemSpecStats itemSpecStats = new(db2Data, sparse);

                foreach (var itemSpec in _cliDB.ItemSpecStorage.Values)
                {
                    if (itemSpecStats.ItemType != itemSpec.ItemType)
                        continue;

                    var hasPrimary = itemSpec.PrimaryStat == ItemSpecStat.None;
                    var hasSecondary = itemSpec.SecondaryStat == ItemSpecStat.None;

                    for (uint i = 0; i < itemSpecStats.ItemSpecStatCount; ++i)
                    {
                        if (itemSpecStats.ItemSpecStatTypes[i] == itemSpec.PrimaryStat)
                            hasPrimary = true;

                        if (itemSpecStats.ItemSpecStatTypes[i] == itemSpec.SecondaryStat)
                            hasSecondary = true;
                    }

                    if (!hasPrimary || !hasSecondary)
                        continue;

                    var specialization = _cliDB.ChrSpecializationStorage.LookupByKey(itemSpec.SpecializationID);

                    if (specialization != null)
                        if (Convert.ToBoolean((1 << (specialization.ClassID - 1)) & sparse.AllowableClass))
                        {
                            itemTemplate.ItemSpecClassMask |= 1u << (specialization.ClassID - 1);
                            var specBit = ItemTemplate.CalculateItemSpecBit(specialization);
                            itemTemplate.Specializations[0].Set(specBit, true);

                            if (itemSpec.MaxLevel > 40)
                                itemTemplate.Specializations[1].Set(specBit, true);

                            if (itemSpec.MaxLevel >= 110)
                                itemTemplate.Specializations[2].Set(specBit, true);
                        }
                }
            }

            // Items that have no specializations set can be used by everyone
            foreach (var specs in itemTemplate.Specializations)
                if (specs.Count == 0)
                    specs.SetAll(true);

            ++sparseCount;
            _itemTemplateStorage.Add(sparse.Id, itemTemplate);
        }

        // Load item effects (spells)
        foreach (var effectEntry in _cliDB.ItemXItemEffectStorage.Values)
        {
            var item = _itemTemplateStorage.LookupByKey(effectEntry.ItemID);

            if (item != null)
            {
                var effect = _cliDB.ItemEffectStorage.LookupByKey(effectEntry.ItemEffectID);

                if (effect != null)
                    item.Effects.Add(effect);
            }
        }

        Log.Logger.Information("Loaded {0} item templates in {1} ms", sparseCount, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadJumpChargeParams()
    {
        var oldMSTime = Time.MSTime;

        // need for reload case
        _jumpChargeParams.Clear();

        //                                         0   1      2                            3            4              5                6
        var result = _worldDatabase.Query("SELECT id, speed, treatSpeedAsMoveTimeSeconds, jumpGravity, spellVisualId, progressCurveId, parabolicCurveId FROM jump_charge_params");

        if (result.IsEmpty())
            return;

        do
        {
            var id = result.Read<int>(0);
            var speed = result.Read<float>(1);
            var treatSpeedAsMoveTimeSeconds = result.Read<bool>(2);
            var jumpGravity = result.Read<float>(3);
            uint? spellVisualId = null;
            uint? progressCurveId = null;
            uint? parabolicCurveId = null;

            if (speed <= 0.0f)
            {
                Log.Logger.Error($"Table `jump_charge_params` uses invalid speed {speed} for id {id}, set to default charge speed {MotionMaster.SPEED_CHARGE}.");
                speed = MotionMaster.SPEED_CHARGE;
            }

            if (jumpGravity <= 0.0f)
            {
                Log.Logger.Error($"Table `jump_charge_params` uses invalid jump gravity {jumpGravity} for id {id}, set to default {MotionMaster.GRAVITY}.");
                jumpGravity = (float)MotionMaster.GRAVITY;
            }

            if (!result.IsNull(4))
            {
                if (_cliDB.SpellVisualStorage.ContainsKey(result.Read<uint>(4)))
                    spellVisualId = result.Read<uint>(4);
                else
                    Log.Logger.Error($"Table `jump_charge_params` references non-existing SpellVisual: {result.Read<uint>(4)} for id {id}, ignored.");
            }

            if (!result.IsNull(5))
            {
                if (_cliDB.CurveStorage.ContainsKey(result.Read<uint>(5)))
                    progressCurveId = result.Read<uint>(5);
                else
                    Log.Logger.Error($"Table `jump_charge_params` references non-existing progress Curve: {result.Read<uint>(5)} for id {id}, ignored.");
            }

            if (!result.IsNull(6))
            {
                if (_cliDB.CurveStorage.ContainsKey(result.Read<uint>(6)))
                    parabolicCurveId = result.Read<uint>(6);
                else
                    Log.Logger.Error($"Table `jump_charge_params` references non-existing parabolic Curve: {result.Read<uint>(6)} for id {id}, ignored.");
            }

            JumpChargeParams jumpParams = new()
            {
                Speed = speed,
                TreatSpeedAsMoveTimeSeconds = treatSpeedAsMoveTimeSeconds,
                JumpGravity = jumpGravity,
                SpellVisualId = spellVisualId,
                ProgressCurveId = progressCurveId,
                ParabolicCurveId = parabolicCurveId
            };

            _jumpChargeParams[id] = jumpParams;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {_jumpChargeParams.Count} Jump Charge Params in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    public void LoadLinkedRespawn()
    {
        var oldMSTime = Time.MSTime;

        _linkedRespawnStorage.Clear();
        //                                                 0        1          2
        var result = _worldDatabase.Query("SELECT guid, linkedGuid, linkType FROM linked_respawn ORDER BY guid ASC");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 linked respawns. DB table `linked_respawn` is empty.");

            return;
        }

        do
        {
            var guidLow = result.Read<ulong>(0);
            var linkedGuidLow = result.Read<ulong>(1);
            var linkType = result.Read<byte>(2);

            var guid = ObjectGuid.Empty;
            var linkedGuid = ObjectGuid.Empty;
            var error = false;

            switch ((CreatureLinkedRespawnType)linkType)
            {
                case CreatureLinkedRespawnType.CreatureToCreature:
                {
                    var slave = GetCreatureData(guidLow);

                    if (slave == null)
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM linked_respawn WHERE guid = {guidLow}");
                        else
                            Log.Logger.Error("Couldn't get creature data for GUIDLow {0}", guidLow);

                        error = true;

                        break;
                    }

                    var master = GetCreatureData(linkedGuidLow);

                    if (master == null)
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM linked_respawn WHERE guid = {guidLow}");
                        else
                            Log.Logger.Error("Couldn't get creature data for GUIDLow {0}", linkedGuidLow);

                        error = true;

                        break;
                    }

                    var map = _cliDB.MapStorage.LookupByKey(master.MapId);

                    if (map == null || !map.Instanceable() || (master.MapId != slave.MapId))
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM linked_respawn WHERE guid = {guidLow}");
                        else
                            Log.Logger.Error("Creature '{0}' linking to '{1}' on an unpermitted map.", guidLow, linkedGuidLow);

                        error = true;

                        break;
                    }

                    // they must have a possibility to meet (normal/heroic difficulty)
                    if (!master.SpawnDifficulties.Intersect(slave.SpawnDifficulties).Any())
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM linked_respawn WHERE guid = {guidLow}");
                        else
                            Log.Logger.Error("LinkedRespawn: Creature '{0}' linking to '{1}' with not corresponding spawnMask", guidLow, linkedGuidLow);

                        error = true;

                        break;
                    }

                    guid = ObjectGuid.Create(HighGuid.Creature, slave.MapId, slave.Id, guidLow);
                    linkedGuid = ObjectGuid.Create(HighGuid.Creature, master.MapId, master.Id, linkedGuidLow);

                    break;
                }
                case CreatureLinkedRespawnType.CreatureToGO:
                {
                    var slave = GetCreatureData(guidLow);

                    if (slave == null)
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM linked_respawn WHERE guid = {guidLow}");
                        else
                            Log.Logger.Error("Couldn't get creature data for GUIDLow {0}", guidLow);

                        error = true;

                        break;
                    }

                    var master = GetGameObjectData(linkedGuidLow);

                    if (master == null)
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM linked_respawn WHERE guid = {guidLow}");
                        else
                            Log.Logger.Error("Couldn't get gameobject data for GUIDLow {0}", linkedGuidLow);

                        error = true;

                        break;
                    }

                    var map = _cliDB.MapStorage.LookupByKey(master.MapId);

                    if (map == null || !map.Instanceable() || (master.MapId != slave.MapId))
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM linked_respawn WHERE guid = {guidLow}");
                        else
                            Log.Logger.Error("Creature '{0}' linking to '{1}' on an unpermitted map.", guidLow, linkedGuidLow);

                        error = true;

                        break;
                    }

                    // they must have a possibility to meet (normal/heroic difficulty)
                    if (!master.SpawnDifficulties.Intersect(slave.SpawnDifficulties).Any())
                    {
                        Log.Logger.Error("LinkedRespawn: Creature '{0}' linking to '{1}' with not corresponding spawnMask", guidLow, linkedGuidLow);
                        error = true;

                        break;
                    }

                    guid = ObjectGuid.Create(HighGuid.Creature, slave.MapId, slave.Id, guidLow);
                    linkedGuid = ObjectGuid.Create(HighGuid.GameObject, master.MapId, master.Id, linkedGuidLow);

                    break;
                }
                case CreatureLinkedRespawnType.GOToGO:
                {
                    var slave = GetGameObjectData(guidLow);

                    if (slave == null)
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM linked_respawn WHERE guid = {guidLow}");
                        else
                            Log.Logger.Error("Couldn't get gameobject data for GUIDLow {0}", guidLow);

                        error = true;

                        break;
                    }

                    var master = GetGameObjectData(linkedGuidLow);

                    if (master == null)
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM linked_respawn WHERE guid = {guidLow}");
                        else
                            Log.Logger.Error("Couldn't get gameobject data for GUIDLow {0}", linkedGuidLow);

                        error = true;

                        break;
                    }

                    var map = _cliDB.MapStorage.LookupByKey(master.MapId);

                    if (map == null || !map.Instanceable() || (master.MapId != slave.MapId))
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM linked_respawn WHERE guid = {guidLow}");
                        else
                            Log.Logger.Error("Creature '{0}' linking to '{1}' on an unpermitted map.", guidLow, linkedGuidLow);

                        error = true;

                        break;
                    }

                    // they must have a possibility to meet (normal/heroic difficulty)
                    if (!master.SpawnDifficulties.Intersect(slave.SpawnDifficulties).Any())
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM linked_respawn WHERE guid = {guidLow}");
                        else
                            Log.Logger.Error("LinkedRespawn: Creature '{0}' linking to '{1}' with not corresponding spawnMask", guidLow, linkedGuidLow);

                        error = true;

                        break;
                    }

                    guid = ObjectGuid.Create(HighGuid.GameObject, slave.MapId, slave.Id, guidLow);
                    linkedGuid = ObjectGuid.Create(HighGuid.GameObject, master.MapId, master.Id, linkedGuidLow);

                    break;
                }
                case CreatureLinkedRespawnType.GOToCreature:
                {
                    var slave = GetGameObjectData(guidLow);

                    if (slave == null)
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM linked_respawn WHERE guid = {guidLow}");
                        else
                            Log.Logger.Error("Couldn't get gameobject data for GUIDLow {0}", guidLow);

                        error = true;

                        break;
                    }

                    var master = GetCreatureData(linkedGuidLow);

                    if (master == null)
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM linked_respawn WHERE guid = {guidLow}");
                        else
                            Log.Logger.Error("Couldn't get creature data for GUIDLow {0}", linkedGuidLow);

                        error = true;

                        break;
                    }

                    var map = _cliDB.MapStorage.LookupByKey(master.MapId);

                    if (map == null || !map.Instanceable() || (master.MapId != slave.MapId))
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM linked_respawn WHERE guid = {guidLow}");
                        else
                            Log.Logger.Error("Creature '{0}' linking to '{1}' on an unpermitted map.", guidLow, linkedGuidLow);

                        error = true;

                        break;
                    }

                    // they must have a possibility to meet (normal/heroic difficulty)
                    if (!master.SpawnDifficulties.Intersect(slave.SpawnDifficulties).Any())
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM linked_respawn WHERE guid = {guidLow}");
                        else
                            Log.Logger.Error("LinkedRespawn: Creature '{0}' linking to '{1}' with not corresponding spawnMask", guidLow, linkedGuidLow);

                        error = true;

                        break;
                    }

                    guid = ObjectGuid.Create(HighGuid.GameObject, slave.MapId, slave.Id, guidLow);
                    linkedGuid = ObjectGuid.Create(HighGuid.Creature, master.MapId, master.Id, linkedGuidLow);

                    break;
                }
            }

            if (!error)
                _linkedRespawnStorage[guid] = linkedGuid;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} linked respawns in {1} ms", _linkedRespawnStorage.Count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadMailLevelRewards()
    {
        var oldMSTime = Time.MSTime;

        _mailLevelRewardStorage.Clear(); // for reload case

        //                                           0        1             2            3
        var result = _worldDatabase.Query("SELECT level, raceMask, mailTemplateId, senderEntry FROM mail_level_reward");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 level dependent mail rewards. DB table `mail_level_reward` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var level = result.Read<byte>(0);
            var raceMask = result.Read<ulong>(1);
            var mailTemplateId = result.Read<uint>(2);
            var senderEntry = result.Read<uint>(3);

            if (level > _configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel))
            {
                Log.Logger.Error("Table `mail_level_reward` have data for level {0} that more supported by client ({1}), ignoring.", level, _configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel));

                continue;
            }

            if (!Convert.ToBoolean(raceMask & SharedConst.RaceMaskAllPlayable))
            {
                Log.Logger.Error("Table `mail_level_reward` have raceMask ({0}) for level {1} that not include any player races, ignoring.", raceMask, level);

                continue;
            }

            if (!_cliDB.MailTemplateStorage.ContainsKey(mailTemplateId))
            {
                Log.Logger.Error("Table `mail_level_reward` have invalid mailTemplateId ({0}) for level {1} that invalid not include any player races, ignoring.", mailTemplateId, level);

                continue;
            }

            if (GetCreatureTemplate(senderEntry) == null)
            {
                Log.Logger.Error("Table `mail_level_reward` have not existed sender creature entry ({0}) for level {1} that invalid not include any player races, ignoring.", senderEntry, level);

                continue;
            }

            _mailLevelRewardStorage.Add(level, new MailLevelReward(raceMask, mailTemplateId, senderEntry));

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} level dependent mail rewards in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadNPCSpellClickSpells()
    {
        var oldMSTime = Time.MSTime;

        _spellClickInfoStorage.Clear();
        //                                           0          1         2            3
        var result = _worldDatabase.Query("SELECT npc_entry, spell_id, cast_flags, user_type FROM npc_spellclick_spells");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 spellclick spells. DB table `npc_spellclick_spells` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var npc_entry = result.Read<uint>(0);
            var cInfo = GetCreatureTemplate(npc_entry);

            if (cInfo == null)
            {
                Log.Logger.Error("Table npc_spellclick_spells references unknown creature_template {0}. Skipping entry.", npc_entry);

                continue;
            }

            var spellid = result.Read<uint>(1);
            var spellinfo = Global.SpellMgr.GetSpellInfo(spellid, Difficulty.None);

            if (spellinfo == null)
            {
                Log.Logger.Error("Table npc_spellclick_spells creature: {0} references unknown spellid {1}. Skipping entry.", npc_entry, spellid);

                continue;
            }

            var userType = (SpellClickUserTypes)result.Read<byte>(3);

            if (userType >= SpellClickUserTypes.Max)
                Log.Logger.Error("Table npc_spellclick_spells creature: {0} references unknown user type {1}. Skipping entry.", npc_entry, userType);

            var castFlags = result.Read<byte>(2);

            SpellClickInfo info = new()
            {
                SpellId = spellid,
                CastFlags = castFlags,
                UserType = userType
            };

            _spellClickInfoStorage.Add(npc_entry, info);

            ++count;
        } while (result.NextRow());

        // all spellclick data loaded, now we check if there are creatures with NPC_FLAG_SPELLCLICK but with no data
        // NOTE: It *CAN* be the other way around: no spellclick Id but with spellclick data, in case of creature-only vehicle accessories
        var ctc = GetCreatureTemplates();

        foreach (var creature in ctc.Values)
            if (creature.Npcflag.HasAnyFlag((uint)NPCFlags.SpellClick) && !_spellClickInfoStorage.ContainsKey(creature.Entry))
            {
                Log.Logger.Warning("npc_spellclick_spells: Creature template {0} has UNIT_NPC_FLAG_SPELLCLICK but no data in spellclick table! Removing Id", creature.Entry);
                creature.Npcflag &= ~(uint)NPCFlags.SpellClick;
            }

        Log.Logger.Information("Loaded {0} spellclick definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadNPCText()
    {
        var oldMSTime = Time.MSTime;

        _npcTextStorage.Clear();

        var result = _worldDatabase.Query("SELECT ID, Probability0, Probability1, Probability2, Probability3, Probability4, Probability5, Probability6, Probability7, " +
                                          "BroadcastTextID0, BroadcastTextID1, BroadcastTextID2, BroadcastTextID3, BroadcastTextID4, BroadcastTextID5, BroadcastTextID6, BroadcastTextID7 FROM npc_text");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 npc texts, table is empty!");

            return;
        }

        do
        {
            var textID = result.Read<uint>(0);

            if (textID == 0)
            {
                Log.Logger.Error("Table `npc_text` has record wit reserved id 0, ignore.");

                continue;
            }

            NpcText npcText = new();

            for (var i = 0; i < SharedConst.MaxNpcTextOptions; i++)
            {
                npcText.Data[i].Probability = result.Read<float>(1 + i);
                npcText.Data[i].BroadcastTextID = result.Read<uint>(9 + i);
            }

            for (var i = 0; i < SharedConst.MaxNpcTextOptions; i++)
                if (npcText.Data[i].BroadcastTextID != 0)
                    if (!_cliDB.BroadcastTextStorage.ContainsKey(npcText.Data[i].BroadcastTextID))
                    {
                        Log.Logger.Debug("NPCText (Id: {0}) has a non-existing BroadcastText (ID: {1}, Index: {2})", textID, npcText.Data[i].BroadcastTextID, i);
                        npcText.Data[i].Probability = 0.0f;
                        npcText.Data[i].BroadcastTextID = 0;
                    }

            for (byte i = 0; i < SharedConst.MaxNpcTextOptions; i++)
                if (npcText.Data[i].Probability > 0 && npcText.Data[i].BroadcastTextID == 0)
                {
                    Log.Logger.Debug("NPCText (ID: {0}) has a probability (Index: {1}) set, but no BroadcastTextID to go with it", textID, i);
                    npcText.Data[i].Probability = 0;
                }

            var probabilitySum = npcText.Data.Aggregate(0f, (float sum, NpcTextData data) => { return sum + data.Probability; });

            if (probabilitySum <= 0.0f)
            {
                Log.Logger.Debug($"NPCText (ID: {textID}) has a probability sum 0, no text can be selected from it, skipped.");

                continue;
            }

            _npcTextStorage[textID] = npcText;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} npc texts in {1} ms", _npcTextStorage.Count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadPageTextLocales()
    {
        var oldMSTime = Time.MSTime;

        _pageTextLocaleStorage.Clear(); // needed for reload case

        //                                               0      1     2
        var result = _worldDatabase.Query("SELECT ID, locale, `Text` FROM page_text_locale");

        if (result.IsEmpty())
            return;

        do
        {
            var id = result.Read<uint>(0);
            var localeName = result.Read<string>(1);
            var locale = localeName.ToEnum<Locale>();

            if (!SharedConst.IsValidLocale(locale) || locale == Locale.enUS)
                continue;

            if (!_pageTextLocaleStorage.ContainsKey(id))
                _pageTextLocaleStorage[id] = new PageTextLocale();

            var data = _pageTextLocaleStorage[id];
            AddLocaleString(result.Read<string>(2), locale, data.Text);
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} PageText locale strings in {1} ms", _pageTextLocaleStorage.Count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadPageTexts()
    {
        var oldMSTime = Time.MSTime;

        //                                         0   1     2           3                 4
        var result = _worldDatabase.Query("SELECT ID, `text`, NextPageID, PlayerConditionID, Flags FROM page_text");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 page texts. DB table `page_text` is empty!");

            return;
        }

        uint count = 0;

        do
        {
            var id = result.Read<uint>(0);

            PageText pageText = new()
            {
                Text = result.Read<string>(1),
                NextPageID = result.Read<uint>(2),
                PlayerConditionID = result.Read<int>(3),
                Flags = result.Read<byte>(4)
            };

            _pageTextStorage[id] = pageText;
            ++count;
        } while (result.NextRow());

        foreach (var pair in _pageTextStorage)
            if (pair.Value.NextPageID != 0)
                if (!_pageTextStorage.ContainsKey(pair.Value.NextPageID))
                    Log.Logger.Error("Page text (ID: {0}) has non-existing `NextPageID` ({1})", pair.Key, pair.Value.NextPageID);

        Log.Logger.Information("Loaded {0} page texts in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    //Pets
    public void LoadPetLevelInfo()
    {
        var oldMSTime = Time.MSTime;

        //                                         0               1      2   3     4    5    6    7     8    9
        var result = _worldDatabase.Query("SELECT creature_entry, level, hp, mana, str, agi, sta, inte, spi, armor FROM pet_levelstats");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 level pet stats definitions. DB table `pet_levelstats` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var creatureid = result.Read<uint>(0);

            if (GetCreatureTemplate(creatureid) == null)
            {
                if (_configuration.GetDefaultValue("load.autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM pet_levelstats WHERE creature_entry = {creatureid}");
                else
                    Log.Logger.Error("Wrong creature id {0} in `pet_levelstats` table, ignoring.", creatureid);

                continue;
            }

            var currentlevel = result.Read<uint>(1);

            if (currentlevel > _configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel))
            {
                if (currentlevel > SharedConst.StrongMaxLevel) // hardcoded level maximum
                {
                    Log.Logger.Error("Wrong (> {0}) level {1} in `pet_levelstats` table, ignoring.", SharedConst.StrongMaxLevel, currentlevel);
                }
                else
                {
                    Log.Logger.Warning("Unused (> MaxPlayerLevel in worldserver.conf) level {0} in `pet_levelstats` table, ignoring.", currentlevel);
                    ++count; // make result loading percent "expected" correct in case disabled detail mode for example.
                }

                continue;
            }
            else if (currentlevel < 1)
            {
                Log.Logger.Error("Wrong (<1) level {0} in `pet_levelstats` table, ignoring.", currentlevel);

                continue;
            }

            var pInfoMapEntry = _petInfoStore.LookupByKey(creatureid);

            if (pInfoMapEntry == null)
                pInfoMapEntry = new PetLevelInfo[_configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel)];

            PetLevelInfo pLevelInfo = new()
            {
                Health = result.Read<uint>(2),
                Mana = result.Read<uint>(3),
                Armor = result.Read<uint>(9)
            };

            for (var i = 0; i < (int)Stats.Max; i++)
                pLevelInfo.Stats[i] = result.Read<uint>(i + 4);

            pInfoMapEntry[currentlevel - 1] = pLevelInfo;

            ++count;
        } while (result.NextRow());

        // Fill gaps and check integrity
        foreach (var map in _petInfoStore)
        {
            var pInfo = map.Value;

            // fatal error if no level 1 data
            if (pInfo == null || pInfo[0].Health == 0)
            {
                Log.Logger.Error("Creature {0} does not have pet stats data for Level 1!", map.Key);
                Global.WorldMgr.StopNow();
            }

            // fill level gaps
            for (byte level = 1; level < _configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel); ++level)
                if (pInfo[level].Health == 0)
                {
                    Log.Logger.Error("Creature {0} has no data for Level {1} pet stats data, using data of Level {2}.", map.Key, level + 1, level);
                    pInfo[level] = pInfo[level - 1];
                }
        }

        Log.Logger.Information("Loaded {0} level pet stats definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadPetNames()
    {
        var oldMSTime = Time.MSTime;
        //                                          0     1      2
        var result = _worldDatabase.Query("SELECT word, entry, half FROM pet_name_generation");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 pet name parts. DB table `pet_name_generation` is empty!");

            return;
        }

        uint count = 0;

        do
        {
            var word = result.Read<string>(0);
            var entry = result.Read<uint>(1);
            var half = result.Read<bool>(2);

            if (half)
                _petHalfName1.Add(entry, word);
            else
                _petHalfName0.Add(entry, word);

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} pet name parts in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadPetNumber()
    {
        var oldMSTime = Time.MSTime;

        var result = DB.Characters.Query("SELECT MAX(id) FROM character_pet");

        if (!result.IsEmpty())
            _hiPetNumber = result.Read<uint>(0) + 1;

        Log.Logger.Information("Loaded the max pet number: {0} in {1} ms", _hiPetNumber - 1, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadPhaseNames()
    {
        var oldMSTime = Time.MSTime;
        _phaseNameStorage.Clear();

        //                                          0     1
        var result = _worldDatabase.Query("SELECT `ID`, `Name` FROM `phase_name`");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 phase names. DB table `phase_name` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var phaseId = result.Read<uint>(0);
            var name = result.Read<string>(1);

            _phaseNameStorage[phaseId] = name;

            ++count;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {count} phase names in {Time.GetMSTimeDiffToNow(oldMSTime)} ms.");
    }

    //Spells /Skills / Phases
    public void LoadPhases()
    {
        foreach (var phase in _cliDB.PhaseStorage.Values)
            _phaseInfoById.Add(phase.Id, new PhaseInfoStruct(phase.Id));

        foreach (var map in _cliDB.MapStorage.Values)
            if (map.ParentMapID != -1)
                _terrainSwapInfoById.Add(map.Id, new TerrainSwapInfo(map.Id));

        Log.Logger.Information("Loading Terrain World Map definitions...");
        LoadTerrainWorldMaps();

        Log.Logger.Information("Loading Terrain Swap Default definitions...");
        LoadTerrainSwapDefaults();

        Log.Logger.Information("Loading Phase Area definitions...");
        LoadAreaPhases();
    }

    public void LoadPlayerChoices()
    {
        var oldMSTime = Time.MSTime;
        _playerChoices.Clear();

        var choiceResult = _worldDatabase.Query("SELECT ChoiceId, UiTextureKitId, SoundKitId, CloseSoundKitId, Duration, Question, PendingChoiceText, HideWarboardHeader, KeepOpenAfterChoice FROM playerchoice");

        if (choiceResult.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 player choices. DB table `playerchoice` is empty.");

            return;
        }

        uint responseCount = 0;
        uint rewardCount = 0;
        uint itemRewardCount = 0;
        uint currencyRewardCount = 0;
        uint factionRewardCount = 0;
        uint itemChoiceRewardCount = 0;
        uint mawPowersCount = 0;

        do
        {
            PlayerChoice choice = new()
            {
                ChoiceId = choiceResult.Read<int>(0),
                UiTextureKitId = choiceResult.Read<int>(1),
                SoundKitId = choiceResult.Read<uint>(2),
                CloseSoundKitId = choiceResult.Read<uint>(3),
                Duration = choiceResult.Read<long>(4),
                Question = choiceResult.Read<string>(5),
                PendingChoiceText = choiceResult.Read<string>(6),
                HideWarboardHeader = choiceResult.Read<bool>(7),
                KeepOpenAfterChoice = choiceResult.Read<bool>(8)
            };

            _playerChoices[choice.ChoiceId] = choice;
        } while (choiceResult.NextRow());

        //                                            0         1           2                   3                4      5
        var responses = _worldDatabase.Query("SELECT ChoiceId, ResponseId, ResponseIdentifier, ChoiceArtFileId, Flags, WidgetSetID, " +
                                             //6                        7           8        9               10      11      12         13              14           15            16
                                             "UiTextureAtlasElementID, SoundKitID, GroupID, UiTextureKitID, Answer, Header, SubHeader, ButtonTooltip, Description, Confirmation, RewardQuestID " +
                                             "FROM playerchoice_response ORDER BY `Index` ASC");

        if (!responses.IsEmpty())
            do
            {
                var choiceId = responses.Read<int>(0);
                var responseId = responses.Read<int>(1);

                if (!_playerChoices.ContainsKey(choiceId))
                {
                    Log.Logger.Error($"Table `playerchoice_response` references non-existing ChoiceId: {choiceId} (ResponseId: {responseId}), skipped");

                    continue;
                }

                var choice = _playerChoices[choiceId];

                PlayerChoiceResponse response = new()
                {
                    ResponseId = responseId,
                    ResponseIdentifier = responses.Read<ushort>(2),
                    ChoiceArtFileId = responses.Read<int>(3),
                    Flags = responses.Read<int>(4),
                    WidgetSetID = responses.Read<uint>(5),
                    UiTextureAtlasElementID = responses.Read<uint>(6),
                    SoundKitID = responses.Read<uint>(7),
                    GroupID = responses.Read<byte>(8),
                    UiTextureKitID = responses.Read<int>(9),
                    Answer = responses.Read<string>(10),
                    Header = responses.Read<string>(11),
                    SubHeader = responses.Read<string>(12),
                    ButtonTooltip = responses.Read<string>(13),
                    Description = responses.Read<string>(14),
                    Confirmation = responses.Read<string>(15)
                };

                if (!responses.IsNull(16))
                    response.RewardQuestID = responses.Read<uint>(16);

                choice.Responses.Add(response);

                ++responseCount;
            } while (responses.NextRow());

        var rewards = _worldDatabase.Query("SELECT ChoiceId, ResponseId, TitleId, PackageId, SkillLineId, SkillPointCount, ArenaPointCount, HonorPointCount, Money, Xp FROM playerchoice_response_reward");

        if (!rewards.IsEmpty())
            do
            {
                var choiceId = rewards.Read<int>(0);
                var responseId = rewards.Read<int>(1);

                if (!_playerChoices.ContainsKey(choiceId))
                {
                    Log.Logger.Error($"Table `playerchoice_response_reward` references non-existing ChoiceId: {choiceId} (ResponseId: {responseId}), skipped");

                    continue;
                }

                var choice = _playerChoices[choiceId];
                var response = choice.Responses.Find(playerChoiceResponse => playerChoiceResponse.ResponseId == responseId);

                if (response == null)
                {
                    Log.Logger.Error($"Table `playerchoice_response_reward` references non-existing ResponseId: {responseId} for ChoiceId {choiceId}, skipped");

                    continue;
                }

                PlayerChoiceResponseReward reward = new()
                {
                    TitleId = rewards.Read<int>(2),
                    PackageId = rewards.Read<int>(3),
                    SkillLineId = rewards.Read<int>(4),
                    SkillPointCount = rewards.Read<uint>(5),
                    ArenaPointCount = rewards.Read<uint>(6),
                    HonorPointCount = rewards.Read<uint>(7),
                    Money = rewards.Read<ulong>(8),
                    Xp = rewards.Read<uint>(9)
                };

                if (reward.TitleId != 0 && !_cliDB.CharTitlesStorage.ContainsKey(reward.TitleId))
                {
                    Log.Logger.Error($"Table `playerchoice_response_reward` references non-existing Title {reward.TitleId} for ChoiceId {choiceId}, ResponseId: {responseId}, set to 0");
                    reward.TitleId = 0;
                }

                if (reward.PackageId != 0 && Global.DB2Mgr.GetQuestPackageItems((uint)reward.PackageId) == null)
                {
                    Log.Logger.Error($"Table `playerchoice_response_reward` references non-existing QuestPackage {reward.TitleId} for ChoiceId {choiceId}, ResponseId: {responseId}, set to 0");
                    reward.PackageId = 0;
                }

                if (reward.SkillLineId != 0 && !_cliDB.SkillLineStorage.ContainsKey(reward.SkillLineId))
                {
                    Log.Logger.Error($"Table `playerchoice_response_reward` references non-existing SkillLine {reward.TitleId} for ChoiceId {choiceId}, ResponseId: {responseId}, set to 0");
                    reward.SkillLineId = 0;
                    reward.SkillPointCount = 0;
                }

                response.Reward = reward;
                ++rewardCount;
            } while (rewards.NextRow());

        var rewardItem = _worldDatabase.Query("SELECT ChoiceId, ResponseId, ItemId, BonusListIDs, Quantity FROM playerchoice_response_reward_item ORDER BY `Index` ASC");

        if (!rewardItem.IsEmpty())
            do
            {
                var choiceId = rewardItem.Read<int>(0);
                var responseId = rewardItem.Read<int>(1);
                var itemId = rewardItem.Read<uint>(2);
                StringArray bonusListIDsTok = new(rewardItem.Read<string>(3), ' ');
                List<uint> bonusListIds = new();

                if (!bonusListIDsTok.IsEmpty())
                    foreach (uint token in bonusListIDsTok)
                        bonusListIds.Add(token);

                var quantity = rewardItem.Read<int>(4);

                if (!_playerChoices.TryGetValue(choiceId, out var choice) || choice == null)
                {
                    Log.Logger.Error($"Table `playerchoice_response_reward_item` references non-existing ChoiceId: {choiceId} (ResponseId: {responseId}), skipped");

                    continue;
                }

                var response = choice.Responses.Find(playerChoiceResponse => playerChoiceResponse.ResponseId == responseId);

                if (response == null)
                {
                    Log.Logger.Error($"Table `playerchoice_response_reward_item` references non-existing ResponseId: {responseId} for ChoiceId {choiceId}, skipped");

                    continue;
                }

                if (response.Reward == null)
                {
                    Log.Logger.Error($"Table `playerchoice_response_reward_item` references non-existing player choice reward for ChoiceId {choiceId}, ResponseId: {responseId}, skipped");

                    continue;
                }

                if (GetItemTemplate(itemId) == null)
                {
                    Log.Logger.Error($"Table `playerchoice_response_reward_item` references non-existing item {itemId} for ChoiceId {choiceId}, ResponseId: {responseId}, skipped");

                    continue;
                }

                response.Reward.Items.Add(new PlayerChoiceResponseRewardItem(itemId, bonusListIds, quantity));
                itemRewardCount++;
            } while (rewardItem.NextRow());

        var rewardCurrency = _worldDatabase.Query("SELECT ChoiceId, ResponseId, CurrencyId, Quantity FROM playerchoice_response_reward_currency ORDER BY `Index` ASC");

        if (!rewardCurrency.IsEmpty())
            do
            {
                var choiceId = rewardCurrency.Read<int>(0);
                var responseId = rewardCurrency.Read<int>(1);
                var currencyId = rewardCurrency.Read<uint>(2);
                var quantity = rewardCurrency.Read<int>(3);

                var choice = _playerChoices.LookupByKey(choiceId);

                if (choice == null)
                {
                    Log.Logger.Error($"Table `playerchoice_response_reward_currency` references non-existing ChoiceId: {choiceId} (ResponseId: {responseId}), skipped");

                    continue;
                }

                var response = choice.Responses.Find(playerChoiceResponse => playerChoiceResponse.ResponseId == responseId);

                if (response == null)
                {
                    Log.Logger.Error($"Table `playerchoice_response_reward_currency` references non-existing ResponseId: {responseId} for ChoiceId {choiceId}, skipped");

                    continue;
                }

                if (response.Reward == null)
                {
                    Log.Logger.Error($"Table `playerchoice_response_reward_currency` references non-existing player choice reward for ChoiceId {choiceId}, ResponseId: {responseId}, skipped");

                    continue;
                }

                if (!_cliDB.CurrencyTypesStorage.ContainsKey(currencyId))
                {
                    Log.Logger.Error($"Table `playerchoice_response_reward_currency` references non-existing currency {currencyId} for ChoiceId {choiceId}, ResponseId: {responseId}, skipped");

                    continue;
                }

                response.Reward.Currency.Add(new PlayerChoiceResponseRewardEntry(currencyId, quantity));
                currencyRewardCount++;
            } while (rewardCurrency.NextRow());

        var rewardFaction = _worldDatabase.Query("SELECT ChoiceId, ResponseId, FactionId, Quantity FROM playerchoice_response_reward_faction ORDER BY `Index` ASC");

        if (!rewardFaction.IsEmpty())
            do
            {
                var choiceId = rewardFaction.Read<int>(0);
                var responseId = rewardFaction.Read<int>(1);
                var factionId = rewardFaction.Read<uint>(2);
                var quantity = rewardFaction.Read<int>(3);

                var choice = _playerChoices.LookupByKey(choiceId);

                if (choice == null)
                {
                    Log.Logger.Error($"Table `playerchoice_response_reward_faction` references non-existing ChoiceId: {choiceId} (ResponseId: {responseId}), skipped");

                    continue;
                }

                var response = choice.Responses.Find(playerChoiceResponse => playerChoiceResponse.ResponseId == responseId);

                if (response == null)
                {
                    Log.Logger.Error($"Table `playerchoice_response_reward_faction` references non-existing ResponseId: {responseId} for ChoiceId {choiceId}, skipped");

                    continue;
                }

                if (response.Reward == null)
                {
                    Log.Logger.Error($"Table `playerchoice_response_reward_faction` references non-existing player choice reward for ChoiceId {choiceId}, ResponseId: {responseId}, skipped");

                    continue;
                }

                if (!_cliDB.FactionStorage.ContainsKey(factionId))
                {
                    Log.Logger.Error($"Table `playerchoice_response_reward_faction` references non-existing faction {factionId} for ChoiceId {choiceId}, ResponseId: {responseId}, skipped");

                    continue;
                }

                response.Reward.Faction.Add(new PlayerChoiceResponseRewardEntry(factionId, quantity));
                factionRewardCount++;
            } while (rewardFaction.NextRow());

        var rewardItems = _worldDatabase.Query("SELECT ChoiceId, ResponseId, ItemId, BonusListIDs, Quantity FROM playerchoice_response_reward_item_choice ORDER BY `Index` ASC");

        if (!rewardItems.IsEmpty())
            do
            {
                var choiceId = rewardItems.Read<int>(0);
                var responseId = rewardItems.Read<int>(1);
                var itemId = rewardItems.Read<uint>(2);
                StringArray bonusListIDsTok = new(rewardItems.Read<string>(3), ' ');
                List<uint> bonusListIds = new();

                foreach (string token in bonusListIDsTok)
                    bonusListIds.Add(uint.Parse(token));

                var quantity = rewardItems.Read<int>(4);

                var choice = _playerChoices.LookupByKey(choiceId);

                if (choice == null)
                {
                    Log.Logger.Error($"Table `playerchoice_response_reward_item_choice` references non-existing ChoiceId: {choiceId} (ResponseId: {responseId}), skipped");

                    continue;
                }

                var response = choice.Responses.Find(playerChoiceResponse => playerChoiceResponse.ResponseId == responseId);

                if (response == null)
                {
                    Log.Logger.Error($"Table `playerchoice_response_reward_item_choice` references non-existing ResponseId: {responseId} for ChoiceId {choiceId}, skipped");

                    continue;
                }

                if (response.Reward == null)
                {
                    Log.Logger.Error($"Table `playerchoice_response_reward_item_choice` references non-existing player choice reward for ChoiceId {choiceId}, ResponseId: {responseId}, skipped");

                    continue;
                }

                if (GetItemTemplate(itemId) == null)
                {
                    Log.Logger.Error($"Table `playerchoice_response_reward_item_choice` references non-existing item {itemId} for ChoiceId {choiceId}, ResponseId: {responseId}, skipped");

                    continue;
                }

                response.Reward.ItemChoices.Add(new PlayerChoiceResponseRewardItem(itemId, bonusListIds, quantity));
                itemChoiceRewardCount++;
            } while (rewards.NextRow());

        var mawPowersResult = _worldDatabase.Query("SELECT ChoiceId, ResponseId, TypeArtFileID, Rarity, RarityColor, SpellID, MaxStacks FROM playerchoice_response_maw_power");

        if (!mawPowersResult.IsEmpty())
            do
            {
                var choiceId = mawPowersResult.Read<int>(0);
                var responseId = mawPowersResult.Read<int>(1);

                var choice = _playerChoices.LookupByKey(choiceId);

                if (choice == null)
                {
                    Log.Logger.Error($"Table `playerchoice_response_maw_power` references non-existing ChoiceId: {choiceId} (ResponseId: {responseId}), skipped");

                    continue;
                }

                var response = choice.Responses.Find(playerChoiceResponse => { return playerChoiceResponse.ResponseId == responseId; });

                if (response == null)
                {
                    Log.Logger.Error($"Table `playerchoice_response_maw_power` references non-existing ResponseId: {responseId} for ChoiceId {choiceId}, skipped");

                    continue;
                }

                PlayerChoiceResponseMawPower mawPower = new()
                {
                    TypeArtFileID = mawPowersResult.Read<int>(2)
                };

                if (!mawPowersResult.IsNull(3))
                    mawPower.Rarity = mawPowersResult.Read<int>(3);

                if (!mawPowersResult.IsNull(4))
                    mawPower.RarityColor = mawPowersResult.Read<uint>(4);

                mawPower.SpellID = mawPowersResult.Read<int>(5);
                mawPower.MaxStacks = mawPowersResult.Read<int>(6);
                response.MawPower = mawPower;

                ++mawPowersCount;
            } while (mawPowersResult.NextRow());

        Log.Logger.Information($"Loaded {_playerChoices.Count} player choices, {responseCount} responses, {rewardCount} rewards, {itemRewardCount} item rewards, " +
                               $"{currencyRewardCount} currency rewards, {factionRewardCount} faction rewards, {itemChoiceRewardCount} item choice rewards and {mawPowersCount} maw powers in {Time.GetMSTimeDiffToNow(oldMSTime)} ms.");
    }

    public void LoadPlayerChoicesLocale()
    {
        var oldMSTime = Time.MSTime;

        // need for reload case
        _playerChoiceLocales.Clear();

        //                                               0         1       2
        var result = _worldDatabase.Query("SELECT ChoiceId, locale, Question FROM playerchoice_locale");

        if (!result.IsEmpty())
        {
            do
            {
                var choiceId = result.Read<int>(0);
                var localeName = result.Read<string>(1);
                var locale = localeName.ToEnum<Locale>();

                if (!SharedConst.IsValidLocale(locale) || locale == Locale.enUS)
                    continue;

                if (GetPlayerChoice(choiceId) == null)
                {
                    Log.Logger.Error($"Table `playerchoice_locale` references non-existing ChoiceId: {choiceId} for locale {localeName}, skipped");

                    continue;
                }

                if (!_playerChoiceLocales.ContainsKey(choiceId))
                    _playerChoiceLocales[choiceId] = new PlayerChoiceLocale();

                var data = _playerChoiceLocales[choiceId];
                AddLocaleString(result.Read<string>(2), locale, data.Question);
            } while (result.NextRow());

            Log.Logger.Information($"Loaded {_playerChoiceLocales.Count} Player Choice locale strings in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
        }

        oldMSTime = Time.MSTime;

        //                               0         1           2       3       4       5          6               7            8
        result = _worldDatabase.Query("SELECT ChoiceID, ResponseID, locale, Answer, Header, SubHeader, ButtonTooltip, Description, Confirmation FROM playerchoice_response_locale");

        if (!result.IsEmpty())
        {
            uint count = 0;

            do
            {
                var choiceId = result.Read<int>(0);
                var responseId = result.Read<int>(1);
                var localeName = result.Read<string>(2);
                var locale = localeName.ToEnum<Locale>();

                if (!SharedConst.IsValidLocale(locale) || locale == Locale.enUS)
                    continue;

                var playerChoiceLocale = _playerChoiceLocales.LookupByKey(choiceId);

                if (playerChoiceLocale == null)
                {
                    if (_configuration.GetDefaultValue("load.autoclean", false))
                        _worldDatabase.Execute($"DELETE FROM playerchoice_response_locale WHERE ChoiceID = {choiceId} AND ResponseID = {responseId} AND locale = \"{localeName}\"");
                    else
                        Log.Logger.Error($"Table `playerchoice_locale` references non-existing ChoiceId: {choiceId} for ResponseId {responseId} locale {localeName}, skipped");

                    continue;
                }

                var playerChoice = GetPlayerChoice(choiceId);

                if (playerChoice.GetResponse(responseId) == null)
                {
                    if (_configuration.GetDefaultValue("load.autoclean", false))
                        _worldDatabase.Execute($"DELETE FROM playerchoice_response_locale WHERE ChoiceID = {choiceId} AND ResponseID = {responseId} AND locale = \"{localeName}\"");
                    else
                        Log.Logger.Error($"Table `playerchoice_locale` references non-existing ResponseId: {responseId} for ChoiceId {choiceId} locale {localeName}, skipped");

                    continue;
                }

                if (playerChoiceLocale.Responses.TryGetValue(responseId, out var data))
                {
                    AddLocaleString(result.Read<string>(3), locale, data.Answer);
                    AddLocaleString(result.Read<string>(4), locale, data.Header);
                    AddLocaleString(result.Read<string>(5), locale, data.SubHeader);
                    AddLocaleString(result.Read<string>(6), locale, data.ButtonTooltip);
                    AddLocaleString(result.Read<string>(7), locale, data.Description);
                    AddLocaleString(result.Read<string>(8), locale, data.Confirmation);
                    count++;
                }
                else
                {
                    if (_configuration.GetDefaultValue("load.autoclean", false))
                        _worldDatabase.Execute($"DELETE FROM playerchoice_response_locale WHERE ChoiceID = {choiceId} AND ResponseID = {responseId} AND locale = \"{localeName}\"");
                    else
                        Log.Logger.Error($"Table `playerchoice_locale` references non-existing locale for ResponseId: {responseId} for ChoiceId {choiceId} locale {localeName}, skipped");
                }
            } while (result.NextRow());

            Log.Logger.Information($"Loaded {count} Player Choice Response locale strings in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
        }
    }

    //Player
    public void LoadPlayerInfo()
    {
        var time = Time.MSTime;

        // Load playercreate
        {
            //                                         0     1      2    3           4           5           6            7        8               9               10              11               12                  13              14              15
            var result = _worldDatabase.Query("SELECT race, class, map, position_x, position_y, position_z, orientation, npe_map, npe_position_x, npe_position_y, npe_position_z, npe_orientation, npe_transport_guid, intro_movie_id, intro_scene_id, npe_intro_scene_id FROM playercreateinfo");

            if (result.IsEmpty())
            {
                Log.Logger.Information("Loaded 0 player create definitions. DB table `playercreateinfo` is empty.");

                return;
            }

            uint count = 0;

            do
            {
                var currentrace = result.Read<uint>(0);
                var currentclass = result.Read<uint>(1);
                var mapId = result.Read<uint>(2);
                var positionX = result.Read<float>(3);
                var positionY = result.Read<float>(4);
                var positionZ = result.Read<float>(5);
                var orientation = result.Read<float>(6);

                if (!_cliDB.ChrRacesStorage.ContainsKey(currentrace))
                {
                    Log.Logger.Error($"Wrong race {currentrace} in `playercreateinfo` table, ignoring.");

                    continue;
                }

                if (!_cliDB.ChrClassesStorage.ContainsKey(currentclass))
                {
                    Log.Logger.Error($"Wrong class {currentclass} in `playercreateinfo` table, ignoring.");

                    continue;
                }

                // accept DB data only for valid position (and non instanceable)
                if (!GridDefines.IsValidMapCoord(mapId, positionX, positionY, positionZ, orientation))
                {
                    Log.Logger.Error($"Wrong home position for class {currentclass} race {currentrace} pair in `playercreateinfo` table, ignoring.");

                    continue;
                }

                if (_cliDB.MapStorage.LookupByKey(mapId).Instanceable())
                {
                    Log.Logger.Error($"Home position in instanceable map for class {currentclass} race {currentrace} pair in `playercreateinfo` table, ignoring.");

                    continue;
                }

                if (Global.DB2Mgr.GetChrModel((Race)currentrace, Gender.Male) == null)
                {
                    Log.Logger.Error($"Missing male model for race {currentrace}, ignoring.");

                    continue;
                }

                if (Global.DB2Mgr.GetChrModel((Race)currentrace, Gender.Female) == null)
                {
                    Log.Logger.Error($"Missing female model for race {currentrace}, ignoring.");

                    continue;
                }

                PlayerInfo info = new();
                info.CreatePosition.Loc = new WorldLocation(mapId, positionX, positionY, positionZ, orientation);

                if (!result.IsNull(7))
                {
                    PlayerInfo.CreatePositionModel createPosition = new()
                    {
                        Loc = new WorldLocation(result.Read<uint>(7), result.Read<float>(8), result.Read<float>(9), result.Read<float>(10), result.Read<float>(11))
                    };

                    if (!result.IsNull(12))
                        createPosition.TransportGuid = result.Read<ulong>(12);

                    info.CreatePositionNpe = createPosition;

                    if (!_cliDB.MapStorage.ContainsKey(info.CreatePositionNpe.Value.Loc.MapId))
                    {
                        Log.Logger.Error($"Invalid NPE map id {info.CreatePositionNpe.Value.Loc.MapId} for class {currentclass} race {currentrace} pair in `playercreateinfo` table, ignoring.");
                        info.CreatePositionNpe = null;
                    }

                    if (info.CreatePositionNpe is { TransportGuid: { } } && Global.TransportMgr.GetTransportSpawn(info.CreatePositionNpe.Value.TransportGuid.Value) == null)
                    {
                        Log.Logger.Error($"Invalid NPE transport spawn id {info.CreatePositionNpe.Value.TransportGuid.Value} for class {currentclass} race {currentrace} pair in `playercreateinfo` table, ignoring.");
                        info.CreatePositionNpe = null; // remove entire NPE data - assume user put transport offsets into npe_position fields
                    }
                }

                if (!result.IsNull(13))
                {
                    var introMovieId = result.Read<uint>(13);

                    if (_cliDB.MovieStorage.ContainsKey(introMovieId))
                        info.IntroMovieId = introMovieId;
                    else
                        Log.Logger.Debug($"Invalid intro movie id {introMovieId} for class {currentclass} race {currentrace} pair in `playercreateinfo` table, ignoring.");
                }

                if (!result.IsNull(14))
                {
                    var introSceneId = result.Read<uint>(14);

                    if (GetSceneTemplate(introSceneId) != null)
                        info.IntroSceneId = introSceneId;
                    else
                        Log.Logger.Debug($"Invalid intro scene id {introSceneId} for class {currentclass} race {currentrace} pair in `playercreateinfo` table, ignoring.");
                }

                if (!result.IsNull(15))
                {
                    var introSceneId = result.Read<uint>(15);

                    if (GetSceneTemplate(introSceneId) != null)
                        info.IntroSceneIdNpe = introSceneId;
                    else
                        Log.Logger.Debug($"Invalid NPE intro scene id {introSceneId} for class {currentclass} race {currentrace} pair in `playercreateinfo` table, ignoring.");
                }

                PlayerInfos.Add((Race)currentrace, (PlayerClass)currentclass, info);

                ++count;
            } while (result.NextRow());

            Log.Logger.Information("Loaded {0} player create definitions in {1} ms", count, Time.GetMSTimeDiffToNow(time));
        }

        time = Time.MSTime;
        // Load playercreate items
        Log.Logger.Information("Loading Player Create Items Data...");

        {
            MultiMap<uint, ItemTemplate> itemsByCharacterLoadout = new();

            foreach (var characterLoadoutItem in _cliDB.CharacterLoadoutItemStorage.Values)
            {
                var itemTemplate = GetItemTemplate(characterLoadoutItem.ItemID);

                if (itemTemplate != null)
                    itemsByCharacterLoadout.Add(characterLoadoutItem.CharacterLoadoutID, itemTemplate);
            }

            foreach (var characterLoadout in _cliDB.CharacterLoadoutStorage.Values)
            {
                if (!characterLoadout.IsForNewCharacter())
                    continue;

                var items = itemsByCharacterLoadout.LookupByKey(characterLoadout.Id);

                if (items.Empty())
                    continue;

                for (var raceIndex = Race.Human; raceIndex < Race.Max; ++raceIndex)
                {
                    if (!characterLoadout.RaceMask.HasAnyFlag(SharedConst.GetMaskForRace(raceIndex)))
                        continue;

                    if (PlayerInfos.TryGetValue(raceIndex, (PlayerClass)characterLoadout.ChrClassID, out var playerInfo))
                    {
                        playerInfo.ItemContext = (ItemContext)characterLoadout.ItemContext;

                        foreach (var itemTemplate in items)
                        {
                            // BuyCount by default
                            var count = itemTemplate.BuyCount;

                            // special amount for food/drink
                            if (itemTemplate.Class == ItemClass.Consumable && (ItemSubClassConsumable)itemTemplate.SubClass == ItemSubClassConsumable.FoodDrink)
                            {
                                if (!itemTemplate.Effects.Empty())
                                    count = (SpellCategories)itemTemplate.Effects[0].SpellCategoryID switch
                                    {
                                        SpellCategories.Food => // food
                                            characterLoadout.ChrClassID == (int)PlayerClass.Deathknight ? 10 : 4u,
                                        SpellCategories.Drink => // drink
                                            2,
                                        _ => count
                                    };

                                if (itemTemplate.MaxStackSize < count)
                                    count = itemTemplate.MaxStackSize;
                            }

                            playerInfo.Items.Add(new PlayerCreateInfoItem(itemTemplate.Id, count));
                        }
                    }
                }
            }
        }

        Log.Logger.Information("Loading Player Create Items Override Data...");

        {
            //                                         0     1      2       3
            var result = _worldDatabase.Query("SELECT race, class, itemid, amount FROM playercreateinfo_item");

            if (result.IsEmpty())
            {
                Log.Logger.Information("Loaded 0 custom player create items. DB table `playercreateinfo_item` is empty.");
            }
            else
            {
                uint count = 0;

                do
                {
                    var currentrace = result.Read<uint>(0);

                    if (currentrace >= (int)Race.Max)
                    {
                        Log.Logger.Error("Wrong race {0} in `playercreateinfo_item` table, ignoring.", currentrace);

                        continue;
                    }

                    var currentclass = result.Read<uint>(1);

                    if (currentclass >= (int)PlayerClass.Max)
                    {
                        Log.Logger.Error("Wrong class {0} in `playercreateinfo_item` table, ignoring.", currentclass);

                        continue;
                    }

                    var itemid = result.Read<uint>(2);

                    if (GetItemTemplate(itemid).Id == 0)
                    {
                        Log.Logger.Error("Item id {0} (race {1} class {2}) in `playercreateinfo_item` table but not listed in `itemtemplate`, ignoring.", itemid, currentrace, currentclass);

                        continue;
                    }

                    var amount = result.Read<int>(3);

                    if (amount == 0)
                    {
                        Log.Logger.Error("Item id {0} (class {1} race {2}) have amount == 0 in `playercreateinfo_item` table, ignoring.", itemid, currentrace, currentclass);

                        continue;
                    }

                    if (currentrace == 0 || currentclass == 0)
                    {
                        var minrace = currentrace != 0 ? currentrace : 1;
                        var maxrace = currentrace != 0 ? currentrace + 1 : (int)Race.Max;
                        var minclass = currentclass != 0 ? currentclass : 1;
                        var maxclass = currentclass != 0 ? currentclass + 1 : (int)PlayerClass.Max;

                        for (var r = minrace; r < maxrace; ++r)
                            for (var c = minclass; c < maxclass; ++c)
                                PlayerCreateInfoAddItemHelper(r, c, itemid, amount);
                    }
                    else
                    {
                        PlayerCreateInfoAddItemHelper(currentrace, currentclass, itemid, amount);
                    }

                    ++count;
                } while (result.NextRow());

                Log.Logger.Information("Loaded {0} custom player create items in {1} ms", count, Time.GetMSTimeDiffToNow(time));
            }
        }

        // Load playercreate skills
        Log.Logger.Information("Loading Player Create Skill Data...");

        {
            var oldMSTime = Time.MSTime;

            foreach (var rcInfo in _cliDB.SkillRaceClassInfoStorage.Values)
                if (rcInfo.Availability == 1)
                    for (var raceIndex = Race.Human; raceIndex < Race.Max; ++raceIndex)
                        if (rcInfo.RaceMask == -1 || Convert.ToBoolean(SharedConst.GetMaskForRace(raceIndex) & rcInfo.RaceMask))
                            for (var classIndex = PlayerClass.Warrior; classIndex < PlayerClass.Max; ++classIndex)
                                if (rcInfo.ClassMask == -1 || Convert.ToBoolean((1 << ((int)classIndex - 1)) & rcInfo.ClassMask))
                                    if (PlayerInfos.TryGetValue(raceIndex, classIndex, out var info))
                                        info.Skills.Add(rcInfo);

            Log.Logger.Information("Loaded player create skills in {0} ms", Time.GetMSTimeDiffToNow(oldMSTime));
        }

        // Load playercreate custom spells
        Log.Logger.Information("Loading Player Create Custom Spell Data...");

        {
            var oldMSTime = Time.MSTime;

            var result = _worldDatabase.Query("SELECT racemask, classmask, Spell FROM playercreateinfo_spell_custom");

            if (result.IsEmpty())
            {
                Log.Logger.Information("Loaded 0 player create custom spells. DB table `playercreateinfo_spell_custom` is empty.");
            }
            else
            {
                uint count = 0;

                do
                {
                    var raceMask = result.Read<ulong>(0);
                    var classMask = result.Read<uint>(1);
                    var spellId = result.Read<uint>(2);

                    if (raceMask != 0 && !Convert.ToBoolean(raceMask & SharedConst.RaceMaskAllPlayable))
                    {
                        Log.Logger.Error("Wrong race mask {0} in `playercreateinfo_spell_custom` table, ignoring.", raceMask);

                        continue;
                    }

                    if (classMask != 0 && !Convert.ToBoolean(classMask & (int)PlayerClass.ClassMaskAllPlayable))
                    {
                        Log.Logger.Error("Wrong class mask {0} in `playercreateinfo_spell_custom` table, ignoring.", classMask);

                        continue;
                    }

                    for (var raceIndex = Race.Human; raceIndex < Race.Max; ++raceIndex)
                        if (raceMask == 0 || Convert.ToBoolean((ulong)SharedConst.GetMaskForRace(raceIndex) & raceMask))
                            for (var classIndex = PlayerClass.Warrior; classIndex < PlayerClass.Max; ++classIndex)
                                if (classMask == 0 || Convert.ToBoolean((1 << ((int)classIndex - 1)) & classMask))
                                    if (PlayerInfos.TryGetValue(raceIndex, classIndex, out var playerInfo))
                                    {
                                        playerInfo.CustomSpells.Add(spellId);
                                        ++count;
                                    }
                } while (result.NextRow());

                Log.Logger.Information("Loaded {0} custom player create spells in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
        }

        // Load playercreate cast spell
        Log.Logger.Information("Loading Player Create Cast Spell Data...");

        {
            var oldMSTime = Time.MSTime;

            var result = _worldDatabase.Query("SELECT raceMask, classMask, spell, createMode FROM playercreateinfo_cast_spell");

            if (result.IsEmpty())
            {
                Log.Logger.Information("Loaded 0 player create cast spells. DB table `playercreateinfo_cast_spell` is empty.");
            }
            else
            {
                uint count = 0;

                do
                {
                    var raceMask = result.Read<ulong>(0);
                    var classMask = result.Read<uint>(1);
                    var spellId = result.Read<uint>(2);
                    var playerCreateMode = result.Read<sbyte>(3);

                    if (raceMask != 0 && (raceMask & SharedConst.RaceMaskAllPlayable) == 0)
                    {
                        Log.Logger.Error($"Wrong race mask {raceMask} in `playercreateinfo_cast_spell` table, ignoring.");

                        continue;
                    }

                    if (classMask != 0 && !classMask.HasAnyFlag((uint)PlayerClass.ClassMaskAllPlayable))
                    {
                        Log.Logger.Error($"Wrong class mask {classMask} in `playercreateinfo_cast_spell` table, ignoring.");

                        continue;
                    }

                    if (playerCreateMode is < 0 or >= (sbyte)PlayerCreateMode.Max)
                    {
                        Log.Logger.Error($"Uses invalid createMode {playerCreateMode} in `playercreateinfo_cast_spell` table, ignoring.");

                        continue;
                    }

                    for (var raceIndex = Race.Human; raceIndex < Race.Max; ++raceIndex)
                        if (raceMask == 0 || Convert.ToBoolean((ulong)SharedConst.GetMaskForRace(raceIndex) & raceMask))
                            for (var classIndex = PlayerClass.Warrior; classIndex < PlayerClass.Max; ++classIndex)
                                if (classMask == 0 || Convert.ToBoolean((1 << ((int)classIndex - 1)) & classMask))
                                    if (PlayerInfos.TryGetValue(raceIndex, classIndex, out var info))
                                    {
                                        info.CastSpells[playerCreateMode].Add(spellId);
                                        ++count;
                                    }
                } while (result.NextRow());

                Log.Logger.Information("Loaded {0} player create cast spells in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
        }

        // Load playercreate actions
        time = Time.MSTime;
        Log.Logger.Information("Loading Player Create Action Data...");

        {
            //                                         0     1      2       3       4
            var result = _worldDatabase.Query("SELECT race, class, button, action, type FROM playercreateinfo_action");

            if (result.IsEmpty())
            {
                Log.Logger.Information("Loaded 0 player create actions. DB table `playercreateinfo_action` is empty.");
            }
            else
            {
                uint count = 0;

                do
                {
                    var currentrace = (Race)result.Read<uint>(0);

                    if (currentrace >= Race.Max)
                    {
                        Log.Logger.Error("Wrong race {0} in `playercreateinfo_action` table, ignoring.", currentrace);

                        continue;
                    }

                    var currentclass = (PlayerClass)result.Read<uint>(1);

                    if (currentclass >= PlayerClass.Max)
                    {
                        Log.Logger.Error("Wrong class {0} in `playercreateinfo_action` table, ignoring.", currentclass);

                        continue;
                    }

                    if (PlayerInfos.TryGetValue(currentrace, currentclass, out var info))
                        info.Actions.Add(new PlayerCreateInfoAction(result.Read<byte>(2), result.Read<uint>(3), result.Read<byte>(4)));

                    ++count;
                } while (result.NextRow());

                Log.Logger.Information("Loaded {0} player create actions in {1} ms", count, Time.GetMSTimeDiffToNow(time));
            }
        }

        time = Time.MSTime;
        // Loading levels data (class/race dependent)
        Log.Logger.Information("Loading Player Create Level Stats Data...");

        {
            var raceStatModifiers = new short[(int)Race.Max][];

            for (var i = 0; i < (int)Race.Max; ++i)
                raceStatModifiers[i] = new short[(int)Stats.Max];

            //                                         0     1    2    3    4 
            var result = _worldDatabase.Query("SELECT race, str, agi, sta, inte FROM player_racestats");

            if (result.IsEmpty())
            {
                Log.Logger.Information("Loaded 0 level stats definitions. DB table `player_racestats` is empty.");
                Global.WorldMgr.StopNow();

                return;
            }

            do
            {
                var currentrace = (Race)result.Read<uint>(0);

                if (currentrace >= Race.Max)
                {
                    Log.Logger.Error($"Wrong race {currentrace} in `player_racestats` table, ignoring.");

                    continue;
                }

                for (var i = 0; i < (int)Stats.Max; ++i)
                    raceStatModifiers[(int)currentrace][i] = result.Read<short>(i + 1);
            } while (result.NextRow());

            //                               0      1      2    3    4    5
            result = _worldDatabase.Query("SELECT class, level, str, agi, sta, inte FROM player_classlevelstats");

            if (result.IsEmpty())
            {
                Log.Logger.Information("Loaded 0 level stats definitions. DB table `player_classlevelstats` is empty.");
                Global.WorldMgr.StopNow();

                return;
            }

            uint count = 0;

            do
            {
                var currentclass = (PlayerClass)result.Read<byte>(0);

                if (currentclass >= PlayerClass.Max)
                {
                    Log.Logger.Error("Wrong class {0} in `player_classlevelstats` table, ignoring.", currentclass);

                    continue;
                }

                var currentlevel = result.Read<uint>(1);

                if (currentlevel > _configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel))
                {
                    if (currentlevel > 255) // hardcoded level maximum
                        Log.Logger.Error($"Wrong (> 255) level {currentlevel} in `player_classlevelstats` table, ignoring.");
                    else
                        Log.Logger.Warning($"Unused (> MaxPlayerLevel in worldserver.conf) level {currentlevel} in `player_levelstats` table, ignoring.");

                    continue;
                }

                for (var race = 0; race < raceStatModifiers.Length; ++race)
                {
                    if (!PlayerInfos.TryGetValue((Race)race, currentclass, out var playerInfo))
                        continue;

                    for (var i = 0; i < (int)Stats.Max; i++)
                        playerInfo.LevelInfo[currentlevel - 1].Stats[i] = (ushort)(result.Read<ushort>(i + 2) + raceStatModifiers[race][i]);
                }

                ++count;
            } while (result.NextRow());

            // Fill gaps and check integrity
            for (Race race = 0; race < Race.Max; ++race)
            {
                // skip non existed races
                if (!_cliDB.ChrRacesStorage.ContainsKey(race))
                    continue;

                for (PlayerClass _class = 0; _class < PlayerClass.Max; ++_class)
                {
                    // skip non existed classes
                    if (_cliDB.ChrClassesStorage.LookupByKey(_class) == null)
                        continue;

                    if (!PlayerInfos.TryGetValue(race, _class, out var playerInfo))
                        continue;

                    if (_configuration.GetDefaultValue("character.EnforceRaceAndClassExpations", true))
                    {
                        // skip expansion races if not playing with expansion
                        if (_configuration.GetDefaultValue("Expansion", (int)Expansion.Dragonflight) < (int)Expansion.BurningCrusade && race is Race.BloodElf or Race.Draenei)
                            continue;

                        // skip expansion classes if not playing with expansion
                        if (_configuration.GetDefaultValue("Expansion", (int)Expansion.Dragonflight) < (int)Expansion.WrathOfTheLichKing && _class == PlayerClass.Deathknight)
                            continue;

                        if (_configuration.GetDefaultValue("Expansion", (int)Expansion.Dragonflight) < (int)Expansion.MistsOfPandaria && race is Race.PandarenNeutral or Race.PandarenHorde or Race.PandarenAlliance)
                            continue;

                        if (_configuration.GetDefaultValue("Expansion", (int)Expansion.Dragonflight) < (int)Expansion.Legion && _class == PlayerClass.DemonHunter)
                            continue;

                        if (_configuration.GetDefaultValue("Expansion", (int)Expansion.Dragonflight) < (int)Expansion.Dragonflight && _class == PlayerClass.Evoker)
                            continue;
                    }

                    // fatal error if no level 1 data
                    if (playerInfo.LevelInfo[0].Stats[0] == 0)
                    {
                        Log.Logger.Error("Race {0} Class {1} Level 1 does not have stats data!", race, _class);
                        Environment.Exit(1);

                        return;
                    }

                    // fill level gaps
                    for (var level = 1; level < _configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel); ++level)
                        if (playerInfo.LevelInfo[level].Stats[0] == 0)
                        {
                            Log.Logger.Error("Race {0} Class {1} Level {2} does not have stats data. Using stats data of level {3}.", race, _class, level + 1, level);
                            playerInfo.LevelInfo[level] = playerInfo.LevelInfo[level - 1];
                        }
                }
            }

            Log.Logger.Information("Loaded {0} level stats definitions in {1} ms", count, Time.GetMSTimeDiffToNow(time));
        }

        time = Time.MSTime;
        // Loading xp per level data
        Log.Logger.Information("Loading Player Create XP Data...");

        {
            _playerXPperLevel = new uint[_cliDB.XpGameTable.GetTableRowCount() + 1];

            //                                          0      1
            var result = _worldDatabase.Query("SELECT Level, Experience FROM player_xp_for_level");

            // load the DBC's levels at first...
            for (uint level = 1; level < _cliDB.XpGameTable.GetTableRowCount(); ++level)
                _playerXPperLevel[level] = (uint)_cliDB.XpGameTable.GetRow(level).Total;

            uint count = 0;

            // ...overwrite if needed (custom values)
            if (!result.IsEmpty())
            {
                do
                {
                    uint currentlevel = result.Read<byte>(0);
                    var currentxp = result.Read<uint>(1);

                    if (currentlevel >= _configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel))
                    {
                        if (currentlevel > SharedConst.StrongMaxLevel) // hardcoded level maximum
                        {
                            Log.Logger.Error("Wrong (> {0}) level {1} in `player_xp_for_level` table, ignoring.", 255, currentlevel);
                        }
                        else
                        {
                            Log.Logger.Warning("Unused (> MaxPlayerLevel in worldserver.conf) level {0} in `player_xp_for_levels` table, ignoring.", currentlevel);
                            ++count; // make result loading percent "expected" correct in case disabled detail mode for example.
                        }

                        continue;
                    }

                    //PlayerXPperLevel
                    _playerXPperLevel[currentlevel] = currentxp;
                    ++count;
                } while (result.NextRow());

                // fill level gaps
                for (var level = 1; level < _configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel); ++level)
                    if (_playerXPperLevel[level] == 0)
                    {
                        Log.Logger.Error("Level {0} does not have XP for level data. Using data of level [{1}] + 12000.", level + 1, level);
                        _playerXPperLevel[level] = _playerXPperLevel[level - 1] + 12000;
                    }
            }

            Log.Logger.Information("Loaded {0} xp for level definition(s) from database in {1} ms", count, Time.GetMSTimeDiffToNow(time));
        }
    }

    public void LoadPointOfInterestLocales()
    {
        var oldMSTime = Time.MSTime;

        _pointOfInterestLocaleStorage.Clear(); // need for reload case

        //                                        0      1      2
        var result = _worldDatabase.Query("SELECT ID, locale, Name FROM points_of_interest_locale");

        if (result.IsEmpty())
            return;

        do
        {
            var id = result.Read<uint>(0);
            var localeName = result.Read<string>(1);
            var locale = localeName.ToEnum<Locale>();

            if (!SharedConst.IsValidLocale(locale) || locale == Locale.enUS)
                continue;

            if (!_pointOfInterestLocaleStorage.ContainsKey(id))
                _pointOfInterestLocaleStorage[id] = new PointOfInterestLocale();

            var data = _pointOfInterestLocaleStorage[id];
            AddLocaleString(result.Read<string>(2), locale, data.Name);
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} points_of_interest locale strings in {1} ms", _pointOfInterestLocaleStorage.Count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadPointsOfInterest()
    {
        var oldMSTime = Time.MSTime;

        _pointsOfInterestStorage.Clear(); // need for reload case

        //                                   0   1          2          3          4     5      6           7     8
        var result = _worldDatabase.Query("SELECT ID, PositionX, PositionY, PositionZ, Icon, Flags, Importance, Name, WMOGroupID FROM points_of_interest");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 Points of Interest definitions. DB table `points_of_interest` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var id = result.Read<uint>(0);

            PointOfInterest POI = new()
            {
                Id = id,
                Pos = new Vector3(result.Read<float>(1), result.Read<float>(2), result.Read<float>(3)),
                Icon = result.Read<uint>(4),
                Flags = result.Read<uint>(5),
                Importance = result.Read<uint>(6),
                Name = result.Read<string>(7),
                WmoGroupId = result.Read<uint>(8)
            };

            if (!GridDefines.IsValidMapCoord(POI.Pos.X, POI.Pos.Y, POI.Pos.Z))
            {
                Log.Logger.Error($"Table `points_of_interest` (ID: {id}) have invalid coordinates (PositionX: {POI.Pos.X} PositionY: {POI.Pos.Y} PositionZ: {POI.Pos.Z}), ignored.");

                continue;
            }

            _pointsOfInterestStorage[id] = POI;

            ++count;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {count} Points of Interest definitions in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    public void LoadQuestAreaTriggers()
    {
        var oldMSTime = Time.MSTime;

        _questAreaTriggerStorage.Clear(); // need for reload case

        var result = _worldDatabase.Query("SELECT id, quest FROM areatrigger_involvedrelation");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 quest trigger points. DB table `areatrigger_involvedrelation` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            ++count;

            var trigger_ID = result.Read<uint>(0);
            var quest_ID = result.Read<uint>(1);

            var atEntry = _cliDB.AreaTriggerStorage.LookupByKey(trigger_ID);

            if (atEntry == null)
            {
                Log.Logger.Error("Area trigger (ID:{0}) does not exist in `AreaTrigger.dbc`.", trigger_ID);

                continue;
            }

            var quest = GetQuestTemplate(quest_ID);

            if (quest == null)
            {
                Log.Logger.Error("Table `areatrigger_involvedrelation` has record (id: {0}) for not existing quest {1}", trigger_ID, quest_ID);

                continue;
            }

            if (!quest.HasSpecialFlag(QuestSpecialFlags.ExplorationOrEvent))
            {
                Log.Logger.Error("Table `areatrigger_involvedrelation` has record (id: {0}) for not quest {1}, but quest not have Id QUEST_SPECIAL_FLAGS_EXPLORATION_OR_EVENT. Trigger or quest flags must be fixed, quest modified to require objective.", trigger_ID, quest_ID);

                // this will prevent quest completing without objective
                quest.SetSpecialFlag(QuestSpecialFlags.ExplorationOrEvent);

                // continue; - quest modified to required objective and trigger can be allowed.
            }

            _questAreaTriggerStorage.Add(trigger_ID, quest_ID);
        } while (result.NextRow());

        foreach (var pair in _questObjectives)
        {
            var objective = pair.Value;

            if (objective.Type == QuestObjectiveType.AreaTrigger)
                _questAreaTriggerStorage.Add((uint)objective.ObjectID, objective.QuestID);
        }

        Log.Logger.Information("Loaded {0} quest trigger points in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadQuestGreetingLocales()
    {
        var oldMSTime = Time.MSTime;

        for (var i = 0; i < 2; ++i)
            _questGreetingLocaleStorage[i] = new Dictionary<uint, QuestGreetingLocale>();

        //                                         0   1     2       3
        var result = _worldDatabase.Query("SELECT Id, type, locale, Greeting FROM quest_greeting_locale");

        if (result.IsEmpty())
            return;

        uint count = 0;

        do
        {
            var id = result.Read<uint>(0);
            var type = result.Read<byte>(1);

            switch (type)
            {
                case 0: // Creature
                    if (GetCreatureTemplate(id) == null)
                    {
                        Log.Logger.Error($"Table `quest_greeting_locale`: creature template entry {id} does not exist.");

                        continue;
                    }

                    break;
                case 1: // GameObject
                    if (GetGameObjectTemplate(id) == null)
                    {
                        Log.Logger.Error($"Table `quest_greeting_locale`: gameobject template entry {id} does not exist.");

                        continue;
                    }

                    break;
                default:
                    continue;
            }

            var localeName = result.Read<string>(2);

            var locale = localeName.ToEnum<Locale>();

            if (!SharedConst.IsValidLocale(locale) || locale == Locale.enUS)
                continue;

            if (!_questGreetingLocaleStorage[type].ContainsKey(id))
                _questGreetingLocaleStorage[type][id] = new QuestGreetingLocale();

            var data = _questGreetingLocaleStorage[type][id];
            AddLocaleString(result.Read<string>(3), locale, data.Greeting);
            ++count;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {count} QuestId Greeting locale strings in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    public void LoadQuestGreetings()
    {
        var oldMSTime = Time.MSTime;

        for (var i = 0; i < 2; ++i)
            _questGreetingStorage[i] = new Dictionary<uint, QuestGreeting>();

        //                                         0   1          2                3     
        var result = _worldDatabase.Query("SELECT ID, type, GreetEmoteType, GreetEmoteDelay, Greeting FROM quest_greeting");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 npc texts, table is empty!");

            return;
        }

        uint count = 0;

        do
        {
            var id = result.Read<uint>(0);
            var type = result.Read<byte>(1);

            switch (type)
            {
                case 0: // Creature
                    if (GetCreatureTemplate(id) == null)
                    {
                        Log.Logger.Error("Table `quest_greeting`: creature template entry {0} does not exist.", id);

                        continue;
                    }

                    break;
                case 1: // GameObject
                    if (GetGameObjectTemplate(id) == null)
                    {
                        Log.Logger.Error("Table `quest_greeting`: gameobject template entry {0} does not exist.", id);

                        continue;
                    }

                    break;
                default:
                    continue;
            }

            var greetEmoteType = result.Read<ushort>(2);
            var greetEmoteDelay = result.Read<uint>(3);
            var greeting = result.Read<string>(4);

            _questGreetingStorage[type][id] = new QuestGreeting(greetEmoteType, greetEmoteDelay, greeting);
            count++;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {count} quest_greeting in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    public void LoadQuestObjectivesLocale()
    {
        var oldMSTime = Time.MSTime;

        _questObjectivesLocaleStorage.Clear(); // need for reload case
        //                                        0     1          2
        var result = _worldDatabase.Query("SELECT Id, locale, Description FROM quest_objectives_locale");

        if (result.IsEmpty())
            return;

        do
        {
            var id = result.Read<uint>(0);
            var localeName = result.Read<string>(1);
            var locale = localeName.ToEnum<Locale>();

            if (!SharedConst.IsValidLocale(locale) || locale == Locale.enUS)
                continue;

            if (!_questObjectivesLocaleStorage.ContainsKey(id))
                _questObjectivesLocaleStorage[id] = new QuestObjectivesLocale();

            var data = _questObjectivesLocaleStorage[id];
            AddLocaleString(result.Read<string>(2), locale, data.Description);
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} QuestId Objectives locale strings in {1} ms", _questObjectivesLocaleStorage.Count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadQuestOfferRewardLocale()
    {
        var oldMSTime = Time.MSTime;

        _questOfferRewardLocaleStorage.Clear(); // need for reload case
        //                                               0     1          2
        var result = _worldDatabase.Query("SELECT Id, locale, RewardText FROM quest_offer_reward_locale");

        if (result.IsEmpty())
            return;

        do
        {
            var id = result.Read<uint>(0);
            var localeName = result.Read<string>(1);
            var locale = localeName.ToEnum<Locale>();

            if (!SharedConst.IsValidLocale(locale) || locale == Locale.enUS)
                continue;

            if (!_questOfferRewardLocaleStorage.ContainsKey(id))
                _questOfferRewardLocaleStorage[id] = new QuestOfferRewardLocale();

            var data = _questOfferRewardLocaleStorage[id];
            AddLocaleString(result.Read<string>(2), locale, data.RewardText);
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} QuestId Offer Reward locale strings in {1} ms", _questOfferRewardLocaleStorage.Count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadQuestPOI()
    {
        var oldMSTime = Time.MSTime;

        _questPOIStorage.Clear(); // need for reload case

        //                                         0        1          2     3               4                 5              6      7        8         9      10             11                 12                           13               14
        var result = _worldDatabase.Query("SELECT QuestID, BlobIndex, Idx1, ObjectiveIndex, QuestObjectiveID, QuestObjectID, MapID, UiMapID, Priority, Flags, WorldEffectID, PlayerConditionID, NavigationPlayerConditionID, SpawnTrackingID, AlwaysAllowMergingBlobs FROM quest_poi order by QuestID, Idx1");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 quest POI definitions. DB table `quest_poi` is empty.");

            return;
        }

        Dictionary<uint, MultiMap<int, QuestPOIBlobPoint>> allPoints = new();

        //                                               0        1    2  3  4
        var pointsResult = _worldDatabase.Query("SELECT QuestID, Idx1, X, Y, Z FROM quest_poi_points ORDER BY QuestID DESC, Idx1, Idx2");

        if (!pointsResult.IsEmpty())
            do
            {
                var questId = pointsResult.Read<uint>(0);
                var Idx1 = pointsResult.Read<int>(1);
                var x = pointsResult.Read<int>(2);
                var y = pointsResult.Read<int>(3);
                var z = pointsResult.Read<int>(4);

                if (!allPoints.ContainsKey(questId))
                    allPoints[questId] = new MultiMap<int, QuestPOIBlobPoint>();

                allPoints[questId].Add(Idx1, new QuestPOIBlobPoint(x, y, z));
            } while (pointsResult.NextRow());

        do
        {
            var questID = (uint)result.Read<int>(0);
            var blobIndex = result.Read<int>(1);
            var idx1 = result.Read<int>(2);
            var objectiveIndex = result.Read<int>(3);
            var questObjectiveID = result.Read<int>(4);
            var questObjectID = result.Read<int>(5);
            var mapID = result.Read<int>(6);
            var uiMapId = result.Read<int>(7);
            var priority = result.Read<int>(8);
            var flags = result.Read<int>(9);
            var worldEffectID = result.Read<int>(10);
            var playerConditionID = result.Read<int>(11);
            var navigationPlayerConditionID = result.Read<int>(12);
            var spawnTrackingID = result.Read<int>(13);
            var alwaysAllowMergingBlobs = result.Read<bool>(14);

            if (GetQuestTemplate(questID) == null)
                if (_configuration.GetDefaultValue("load.autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM quest_poi WHERE QuestID = {questID}");
                else
                    Log.Logger.Error($"`quest_poi` quest id ({questID}) Idx1 ({idx1}) does not exist in `quest_template`");

            var blobs = allPoints.LookupByKey(questID);

            if (blobs != null)
            {
                var points = blobs.LookupByKey(idx1);

                if (!points.Empty())
                {
                    if (!_questPOIStorage.ContainsKey(questID))
                        _questPOIStorage[questID] = new QuestPOIData(questID);

                    var poiData = _questPOIStorage[questID];
                    poiData.QuestID = questID;

                    poiData.Blobs.Add(new QuestPOIBlobData(blobIndex,
                                                           objectiveIndex,
                                                           questObjectiveID,
                                                           questObjectID,
                                                           mapID,
                                                           uiMapId,
                                                           priority,
                                                           flags,
                                                           worldEffectID,
                                                           playerConditionID,
                                                           navigationPlayerConditionID,
                                                           spawnTrackingID,
                                                           points,
                                                           alwaysAllowMergingBlobs));

                    continue;
                }
            }

            Log.Logger.Error($"Table quest_poi references unknown quest points for quest {questID} POI id {blobIndex}");
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} quest POI definitions in {1} ms", _questPOIStorage.Count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadQuestRequestItemsLocale()
    {
        var oldMSTime = Time.MSTime;

        _questRequestItemsLocaleStorage.Clear(); // need for reload case
        //                                               0     1          2
        var result = _worldDatabase.Query("SELECT Id, locale, CompletionText FROM quest_request_items_locale");

        if (result.IsEmpty())
            return;

        do
        {
            var id = result.Read<uint>(0);
            var localeName = result.Read<string>(1);
            var locale = localeName.ToEnum<Locale>();

            if (!SharedConst.IsValidLocale(locale) || locale == Locale.enUS)
                continue;

            if (!_questRequestItemsLocaleStorage.ContainsKey(id))
                _questRequestItemsLocaleStorage[id] = new QuestRequestItemsLocale();

            var data = _questRequestItemsLocaleStorage[id];
            AddLocaleString(result.Read<string>(2), locale, data.CompletionText);
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} QuestId Request Items locale strings in {1} ms", _questRequestItemsLocaleStorage.Count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    //Quests
    public void LoadQuests()
    {
        var oldMSTime = Time.MSTime;

        // For reload case
        _questTemplates.Clear();
        _questTemplatesAutoPush.Clear();
        _questObjectives.Clear();
        _exclusiveQuestGroups.Clear();

        var result = _worldDatabase.Query("SELECT " +
                                          //0  1          2               3                4            5            6                  7                8                   9
                                          "ID, QuestType, QuestPackageID, ContentTuningID, QuestSortID, QuestInfoID, SuggestedGroupNum, RewardNextQuest, RewardXPDifficulty, RewardXPMultiplier, " +
                                          //10                    11                     12                13           14           15               16
                                          "RewardMoneyDifficulty, RewardMoneyMultiplier, RewardBonusMoney, RewardSpell, RewardHonor, RewardKillHonor, StartItem, " +
                                          //17                         18                          19                        20     21       22
                                          "RewardArtifactXPDifficulty, RewardArtifactXPMultiplier, RewardArtifactCategoryID, Flags, FlagsEx, FlagsEx2, " +
                                          //23          24             25         26                 27           28             29         30
                                          "RewardItem1, RewardAmount1, ItemDrop1, ItemDropQuantity1, RewardItem2, RewardAmount2, ItemDrop2, ItemDropQuantity2, " +
                                          //31          32             33         34                 35           36             37         38
                                          "RewardItem3, RewardAmount3, ItemDrop3, ItemDropQuantity3, RewardItem4, RewardAmount4, ItemDrop4, ItemDropQuantity4, " +
                                          //39                  40                         41                          42                   43                         44
                                          "RewardChoiceItemID1, RewardChoiceItemQuantity1, RewardChoiceItemDisplayID1, RewardChoiceItemID2, RewardChoiceItemQuantity2, RewardChoiceItemDisplayID2, " +
                                          //45                  46                         47                          48                   49                         50
                                          "RewardChoiceItemID3, RewardChoiceItemQuantity3, RewardChoiceItemDisplayID3, RewardChoiceItemID4, RewardChoiceItemQuantity4, RewardChoiceItemDisplayID4, " +
                                          //51                  52                         53                          54                   55                         56
                                          "RewardChoiceItemID5, RewardChoiceItemQuantity5, RewardChoiceItemDisplayID5, RewardChoiceItemID6, RewardChoiceItemQuantity6, RewardChoiceItemDisplayID6, " +
                                          //57           58    59    60           61           62                 63                 64
                                          "POIContinent, POIx, POIy, POIPriority, RewardTitle, RewardArenaPoints, RewardSkillLineID, RewardNumSkillUps, " +
                                          //65            66                  67                         68
                                          "PortraitGiver, PortraitGiverMount, PortraitGiverModelSceneID, PortraitTurnIn, " +
                                          //69               70                   71                      72                   73                74                   75                      76
                                          "RewardFactionID1, RewardFactionValue1, RewardFactionOverride1, RewardFactionCapIn1, RewardFactionID2, RewardFactionValue2, RewardFactionOverride2, RewardFactionCapIn2, " +
                                          //77               78                   79                      80                   81                82                   83                      84
                                          "RewardFactionID3, RewardFactionValue3, RewardFactionOverride3, RewardFactionCapIn3, RewardFactionID4, RewardFactionValue4, RewardFactionOverride4, RewardFactionCapIn4, " +
                                          //85               86                   87                      88                   89
                                          "RewardFactionID5, RewardFactionValue5, RewardFactionOverride5, RewardFactionCapIn5, RewardFactionFlags, " +
                                          //90                91                  92                 93                  94                 95                  96                 97
                                          "RewardCurrencyID1, RewardCurrencyQty1, RewardCurrencyID2, RewardCurrencyQty2, RewardCurrencyID3, RewardCurrencyQty3, RewardCurrencyID4, RewardCurrencyQty4, " +
                                          //98                 99                  100          101          102             103               104        105                  106
                                          "AcceptedSoundKitID, CompleteSoundKitID, AreaGroupID, TimeAllowed, AllowableRaces, TreasurePickerID, Expansion, ManagedWorldStateID, QuestSessionBonus, " +
                                          //107      108             109               110              111                112                113                 114                 115
                                          "LogTitle, LogDescription, QuestDescription, AreaDescription, PortraitGiverText, PortraitGiverName, PortraitTurnInText, PortraitTurnInName, QuestCompletionLog " +
                                          " FROM quest_template");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 quests definitions. DB table `quest_template` is empty.");

            return;
        }

        // create multimap previous quest for each existed quest
        // some quests can have many previous maps set by NextQuestId in previous quest
        // for example set of race quests can lead to single not race specific quest
        do
        {
            Quest.Quest newQuest = new(result.GetFields());
            _questTemplates[newQuest.Id] = newQuest;

            if (newQuest.IsAutoPush)
                _questTemplatesAutoPush.Add(newQuest);
        } while (result.NextRow());


        // Load `quest_reward_choice_items`
        //                               0        1      2      3      4      5      6
        result = _worldDatabase.Query("SELECT QuestID, Type1, Type2, Type3, Type4, Type5, Type6 FROM quest_reward_choice_items");

        if (result.IsEmpty())
            Log.Logger.Information("Loaded 0 quest reward choice items. DB table `quest_reward_choice_items` is empty.");
        else
            do
            {
                var questId = result.Read<uint>(0);

                var quest = _questTemplates.LookupByKey(questId);

                if (quest != null)
                    quest.LoadRewardChoiceItems(result.GetFields());
                else
                    Log.Logger.Error($"Table `quest_reward_choice_items` has data for quest {questId} but such quest does not exist");
            } while (result.NextRow());


        // Load `quest_reward_display_spell`
        //                               0        1        2
        result = _worldDatabase.Query("SELECT QuestID, SpellID, PlayerConditionID FROM quest_reward_display_spell ORDER BY QuestID ASC, Idx ASC");

        if (result.IsEmpty())
            Log.Logger.Information("Loaded 0 quest reward display spells. DB table `quest_reward_display_spell` is empty.");
        else
            do
            {
                var questId = result.Read<uint>(0);

                var quest = _questTemplates.LookupByKey(questId);

                if (quest != null)
                    quest.LoadRewardDisplaySpell(result.GetFields());
                else
                    Log.Logger.Error($"Table `quest_reward_display_spell` has data for quest {questId} but such quest does not exist");
            } while (result.NextRow());


        // Load `quest_details`
        //                               0   1       2       3       4       5            6            7            8
        result = _worldDatabase.Query("SELECT ID, Emote1, Emote2, Emote3, Emote4, EmoteDelay1, EmoteDelay2, EmoteDelay3, EmoteDelay4 FROM quest_details");

        if (result.IsEmpty())
            Log.Logger.Information("Loaded 0 quest details. DB table `quest_details` is empty.");
        else
            do
            {
                var questId = result.Read<uint>(0);

                var quest = _questTemplates.LookupByKey(questId);

                if (quest != null)
                    quest.LoadQuestDetails(result.GetFields());
                else
                    Log.Logger.Error("Table `quest_details` has data for quest {0} but such quest does not exist", questId);
            } while (result.NextRow());

        // Load `quest_request_items`
        //                               0   1                2                  3                     4                       5
        result = _worldDatabase.Query("SELECT ID, EmoteOnComplete, EmoteOnIncomplete, EmoteOnCompleteDelay, EmoteOnIncompleteDelay, CompletionText FROM quest_request_items");

        if (result.IsEmpty())
            Log.Logger.Information("Loaded 0 quest request items. DB table `quest_request_items` is empty.");
        else
            do
            {
                var questId = result.Read<uint>(0);

                var quest = _questTemplates.LookupByKey(questId);

                if (quest != null)
                    quest.LoadQuestRequestItems(result.GetFields());
                else
                    Log.Logger.Error("Table `quest_request_items` has data for quest {0} but such quest does not exist", questId);
            } while (result.NextRow());

        // Load `quest_offer_reward`
        //                               0   1       2       3       4       5            6            7            8            9
        result = _worldDatabase.Query("SELECT ID, Emote1, Emote2, Emote3, Emote4, EmoteDelay1, EmoteDelay2, EmoteDelay3, EmoteDelay4, RewardText FROM quest_offer_reward");

        if (result.IsEmpty())
            Log.Logger.Information("Loaded 0 quest reward emotes. DB table `quest_offer_reward` is empty.");
        else
            do
            {
                var questId = result.Read<uint>(0);

                var quest = _questTemplates.LookupByKey(questId);

                if (quest != null)
                    quest.LoadQuestOfferReward(result.GetFields());
                else
                    Log.Logger.Error("Table `quest_offer_reward` has data for quest {0} but such quest does not exist", questId);
            } while (result.NextRow());

        // Load `quest_template_addon`
        //                               0   1         2                 3              4            5            6               7                     8                     9
        result = _worldDatabase.Query("SELECT ID, MaxLevel, AllowableClasses, SourceSpellID, PrevQuestID, NextQuestID, ExclusiveGroup, BreadcrumbForQuestId, RewardMailTemplateID, RewardMailDelay, " +
                                      //10               11                   12                     13                     14                   15                   16
                                      "RequiredSkillID, RequiredSkillPoints, RequiredMinRepFaction, RequiredMaxRepFaction, RequiredMinRepValue, RequiredMaxRepValue, ProvidedItemCount, " +
                                      //17           18
                                      "SpecialFlags, ScriptName FROM quest_template_addon LEFT JOIN quest_mail_sender ON Id=QuestId");

        if (result.IsEmpty())
            Log.Logger.Information("Loaded 0 quest template addons. DB table `quest_template_addon` is empty.");
        else
            do
            {
                var questId = result.Read<uint>(0);

                var quest = _questTemplates.LookupByKey(questId);

                if (quest != null)
                    quest.LoadQuestTemplateAddon(result.GetFields());
                else
                    Log.Logger.Error("Table `quest_template_addon` has data for quest {0} but such quest does not exist", questId);
            } while (result.NextRow());

        // Load `quest_mail_sender`
        //                               0        1
        result = _worldDatabase.Query("SELECT QuestId, RewardMailSenderEntry FROM quest_mail_sender");

        if (result.IsEmpty())
            Log.Logger.Information("Loaded 0 quest mail senders. DB table `quest_mail_sender` is empty.");
        else
            do
            {
                var questId = result.Read<uint>(0);

                var quest = _questTemplates.LookupByKey(questId);

                if (quest != null)
                    quest.LoadQuestMailSender(result.GetFields());
                else
                    Log.Logger.Error("Table `quest_mail_sender` has data for quest {0} but such quest does not exist", questId);
            } while (result.NextRow());

        // Load `quest_objectives`
        //                               0        1   2     3             4         5       6      7       8                  9
        result = _worldDatabase.Query("SELECT QuestID, ID, Type, StorageIndex, ObjectID, Amount, Flags, Flags2, ProgressBarWeight, Description FROM quest_objectives ORDER BY `Order` ASC, StorageIndex ASC");

        if (result.IsEmpty())
            Log.Logger.Information("Loaded 0 quest objectives. DB table `quest_objectives` is empty.");
        else
            do
            {
                var questId = result.Read<uint>(0);
                var quest = _questTemplates.LookupByKey(questId);

                if (quest != null)
                    quest.LoadQuestObjective(result.GetFields());
                else
                    Log.Logger.Error("Table `quest_objectives` has objective for quest {0} but such quest does not exist", questId);
            } while (result.NextRow());

        // Load `quest_visual_effect` join table with quest_objectives because visual effects are based on objective ID (core stores objectives by their index in quest)
        //                                 0     1     2          3        4
        result = _worldDatabase.Query("SELECT v.ID, o.ID, o.QuestID, v.Index, v.VisualEffect FROM quest_visual_effect AS v LEFT JOIN quest_objectives AS o ON v.ID = o.ID ORDER BY v.Index DESC");

        if (result.IsEmpty())
            Log.Logger.Information("Loaded 0 quest visual effects. DB table `quest_visual_effect` is empty.");
        else
            do
            {
                var vID = result.Read<uint>(0);
                var oID = result.Read<uint>(1);

                if (vID == 0)
                {
                    Log.Logger.Error("Table `quest_visual_effect` has visual effect for null objective id");

                    continue;
                }

                // objID will be null if match for table join is not found
                if (vID != oID)
                {
                    Log.Logger.Error("Table `quest_visual_effect` has visual effect for objective {0} but such objective does not exist.", vID);

                    continue;
                }

                var questId = result.Read<uint>(2);

                // Do not throw error here because error for non existing quest is thrown while loading quest objectives. we do not need duplication
                var quest = _questTemplates.LookupByKey(questId);

                quest?.LoadQuestObjectiveVisualEffect(result.GetFields());
            } while (result.NextRow());

        Dictionary<uint, uint> usedMailTemplates = new();

        // Post processing
        foreach (var qinfo in _questTemplates.Values)
        {
            // skip post-loading checks for disabled quests
            if (Global.DisableMgr.IsDisabledFor(DisableType.Quest, qinfo.Id, null))
                continue;

            // additional quest integrity checks (GO, creaturetemplate and itemtemplate must be loaded already)

            if (qinfo.Type >= QuestType.Max)
                Log.Logger.Error("QuestId {0} has `Method` = {1}, expected values are 0, 1 or 2.", qinfo.Id, qinfo.Type);

            if (Convert.ToBoolean(qinfo.SpecialFlags & ~QuestSpecialFlags.DbAllowed))
            {
                Log.Logger.Error("QuestId {0} has `SpecialFlags` = {1} > max allowed value. Correct `SpecialFlags` to value <= {2}",
                                 qinfo.Id,
                                 qinfo.SpecialFlags,
                                 QuestSpecialFlags.DbAllowed);

                qinfo.SpecialFlags &= QuestSpecialFlags.DbAllowed;
            }

            if (qinfo.Flags.HasAnyFlag(QuestFlags.Daily) && qinfo.Flags.HasAnyFlag(QuestFlags.Weekly))
            {
                Log.Logger.Error("Weekly QuestId {0} is marked as daily quest in `Flags`, removed daily Id.", qinfo.Id);
                qinfo.Flags &= ~QuestFlags.Daily;
            }

            if (qinfo.Flags.HasAnyFlag(QuestFlags.Daily))
                if (!qinfo.SpecialFlags.HasAnyFlag(QuestSpecialFlags.Repeatable))
                {
                    Log.Logger.Error("Daily QuestId {0} not marked as repeatable in `SpecialFlags`, added.", qinfo.Id);
                    qinfo.SpecialFlags |= QuestSpecialFlags.Repeatable;
                }

            if (qinfo.Flags.HasAnyFlag(QuestFlags.Weekly))
                if (!qinfo.SpecialFlags.HasAnyFlag(QuestSpecialFlags.Repeatable))
                {
                    Log.Logger.Error("Weekly QuestId {0} not marked as repeatable in `SpecialFlags`, added.", qinfo.Id);
                    qinfo.SpecialFlags |= QuestSpecialFlags.Repeatable;
                }

            if (qinfo.SpecialFlags.HasAnyFlag(QuestSpecialFlags.Monthly))
                if (!qinfo.SpecialFlags.HasAnyFlag(QuestSpecialFlags.Repeatable))
                {
                    Log.Logger.Error("Monthly quest {0} not marked as repeatable in `SpecialFlags`, added.", qinfo.Id);
                    qinfo.SpecialFlags |= QuestSpecialFlags.Repeatable;
                }

            if (Convert.ToBoolean(qinfo.Flags & QuestFlags.Tracking))
                // at auto-reward can be rewarded only RewardChoiceItemId[0]
                for (var j = 1; j < qinfo.RewardChoiceItemId.Length; ++j)
                {
                    var id = qinfo.RewardChoiceItemId[j];

                    if (id != 0)
                        Log.Logger.Error("QuestId {0} has `RewardChoiceItemId{1}` = {2} but item from `RewardChoiceItemId{3}` can't be rewarded with quest Id QUESTFLAGSTRACKING.",
                                         qinfo.Id,
                                         j + 1,
                                         id,
                                         j + 1);
                    // no changes, quest ignore this data
                }

            if (qinfo.ContentTuningId != 0 && !_cliDB.ContentTuningStorage.ContainsKey(qinfo.ContentTuningId))
                Log.Logger.Error($"QuestId {qinfo.Id} has `ContentTuningID` = {qinfo.ContentTuningId} but content tuning with this id does not exist.");

            // client quest log visual (area case)
            if (qinfo.QuestSortID > 0)
                if (!_cliDB.AreaTableStorage.ContainsKey(qinfo.QuestSortID))
                    Log.Logger.Error("QuestId {0} has `ZoneOrSort` = {1} (zone case) but zone with this id does not exist.",
                                     qinfo.Id,
                                     qinfo.QuestSortID);

            // no changes, quest not dependent from this value but can have problems at client
            // client quest log visual (sort case)
            if (qinfo.QuestSortID < 0)
            {
                var qSort = _cliDB.QuestSortStorage.LookupByKey((uint)-qinfo.QuestSortID);

                if (qSort == null)
                    Log.Logger.Error("QuestId {0} has `ZoneOrSort` = {1} (sort case) but quest sort with this id does not exist.",
                                     qinfo.Id,
                                     qinfo.QuestSortID);

                // no changes, quest not dependent from this value but can have problems at client (note some may be 0, we must allow this so no check)
                //check for proper RequiredSkillId value (skill case)
                var skillid = SharedConst.SkillByQuestSort(-qinfo.QuestSortID);

                if (skillid != SkillType.None)
                    if (qinfo.RequiredSkillId != (uint)skillid)
                        Log.Logger.Error("QuestId {0} has `ZoneOrSort` = {1} but `RequiredSkillId` does not have a corresponding value ({2}).",
                                         qinfo.Id,
                                         qinfo.QuestSortID,
                                         skillid);
                //override, and force proper value here?
            }

            // AllowableClasses, can be 0/CLASSMASK_ALL_PLAYABLE to allow any class
            if (qinfo.AllowableClasses != 0)
                if (!Convert.ToBoolean(qinfo.AllowableClasses & (uint)PlayerClass.ClassMaskAllPlayable))
                {
                    Log.Logger.Error("QuestId {0} does not contain any playable classes in `RequiredClasses` ({1}), value set to 0 (all classes).", qinfo.Id, qinfo.AllowableClasses);
                    qinfo.AllowableClasses = 0;
                }

            // AllowableRaces, can be -1/RACEMASK_ALL_PLAYABLE to allow any race
            if (qinfo.AllowableRaces != -1)
                if (qinfo.AllowableRaces > 0 && !Convert.ToBoolean(qinfo.AllowableRaces & (long)SharedConst.RaceMaskAllPlayable))
                {
                    Log.Logger.Error("QuestId {0} does not contain any playable races in `RequiredRaces` ({1}), value set to 0 (all races).", qinfo.Id, qinfo.AllowableRaces);
                    qinfo.AllowableRaces = -1;
                }

            // RequiredSkillId, can be 0
            if (qinfo.RequiredSkillId != 0)
                if (!_cliDB.SkillLineStorage.ContainsKey(qinfo.RequiredSkillId))
                    Log.Logger.Error("QuestId {0} has `RequiredSkillId` = {1} but this skill does not exist",
                                     qinfo.Id,
                                     qinfo.RequiredSkillId);

            if (qinfo.RequiredSkillPoints != 0)
                if (qinfo.RequiredSkillPoints > Global.WorldMgr.ConfigMaxSkillValue)
                    Log.Logger.Error("QuestId {0} has `RequiredSkillPoints` = {1} but max possible skill is {2}, quest can't be done.",
                                     qinfo.Id,
                                     qinfo.RequiredSkillPoints,
                                     Global.WorldMgr.ConfigMaxSkillValue);
            // no changes, quest can't be done for this requirement
            // else Skill quests can have 0 skill level, this is ok

            if (qinfo.RequiredMinRepFaction != 0 && !_cliDB.FactionStorage.ContainsKey(qinfo.RequiredMinRepFaction))
                Log.Logger.Error("QuestId {0} has `RequiredMinRepFaction` = {1} but faction template {2} does not exist, quest can't be done.",
                                 qinfo.Id,
                                 qinfo.RequiredMinRepFaction,
                                 qinfo.RequiredMinRepFaction);

            // no changes, quest can't be done for this requirement
            if (qinfo.RequiredMaxRepFaction != 0 && !_cliDB.FactionStorage.ContainsKey(qinfo.RequiredMaxRepFaction))
                Log.Logger.Error("QuestId {0} has `RequiredMaxRepFaction` = {1} but faction template {2} does not exist, quest can't be done.",
                                 qinfo.Id,
                                 qinfo.RequiredMaxRepFaction,
                                 qinfo.RequiredMaxRepFaction);

            // no changes, quest can't be done for this requirement
            if (qinfo.RequiredMinRepValue != 0 && qinfo.RequiredMinRepValue > SharedConst.ReputationCap)
                Log.Logger.Error("QuestId {0} has `RequiredMinRepValue` = {1} but max reputation is {2}, quest can't be done.",
                                 qinfo.Id,
                                 qinfo.RequiredMinRepValue,
                                 SharedConst.ReputationCap);

            // no changes, quest can't be done for this requirement
            if (qinfo.RequiredMinRepValue != 0 && qinfo.RequiredMaxRepValue != 0 && qinfo.RequiredMaxRepValue <= qinfo.RequiredMinRepValue)
                Log.Logger.Error("QuestId {0} has `RequiredMaxRepValue` = {1} and `RequiredMinRepValue` = {2}, quest can't be done.",
                                 qinfo.Id,
                                 qinfo.RequiredMaxRepValue,
                                 qinfo.RequiredMinRepValue);

            // no changes, quest can't be done for this requirement
            if (qinfo.RequiredMinRepFaction == 0 && qinfo.RequiredMinRepValue != 0)
                Log.Logger.Error("QuestId {0} has `RequiredMinRepValue` = {1} but `RequiredMinRepFaction` is 0, value has no effect",
                                 qinfo.Id,
                                 qinfo.RequiredMinRepValue);

            // warning
            if (qinfo.RequiredMaxRepFaction == 0 && qinfo.RequiredMaxRepValue != 0)
                Log.Logger.Error("QuestId {0} has `RequiredMaxRepValue` = {1} but `RequiredMaxRepFaction` is 0, value has no effect",
                                 qinfo.Id,
                                 qinfo.RequiredMaxRepValue);

            // warning
            if (qinfo.RewardTitleId != 0 && !_cliDB.CharTitlesStorage.ContainsKey(qinfo.RewardTitleId))
            {
                Log.Logger.Error("QuestId {0} has `RewardTitleId` = {1} but CharTitle Id {1} does not exist, quest can't be rewarded with title.",
                                 qinfo.Id,
                                 qinfo.RewardTitleId);

                qinfo.RewardTitleId = 0;
                // quest can't reward this title
            }

            if (qinfo.SourceItemId != 0)
            {
                if (GetItemTemplate(qinfo.SourceItemId) == null)
                {
                    Log.Logger.Error("QuestId {0} has `SourceItemId` = {1} but item with entry {2} does not exist, quest can't be done.",
                                     qinfo.Id,
                                     qinfo.SourceItemId,
                                     qinfo.SourceItemId);

                    qinfo.SourceItemId = 0; // quest can't be done for this requirement
                }
                else if (qinfo.SourceItemIdCount == 0)
                {
                    if (_configuration.GetDefaultValue("load.autoclean", false))
                        _worldDatabase.Execute($"UPDATE quest_template_addon SET ProvidedItemCount = 1 WHERE ID = {qinfo.Id}");
                    else
                        Log.Logger.Error("QuestId {0} has `StartItem` = {1} but `ProvidedItemCount` = 0, set to 1 but need fix in DB.",
                                         qinfo.Id,
                                         qinfo.SourceItemId);

                    qinfo.SourceItemIdCount = 1; // update to 1 for allow quest work for backward compatibility with DB
                }
            }
            else if (qinfo.SourceItemIdCount > 0)
            {
                Log.Logger.Error("QuestId {0} has `SourceItemId` = 0 but `SourceItemIdCount` = {1}, useless value.",
                                 qinfo.Id,
                                 qinfo.SourceItemIdCount);

                qinfo.SourceItemIdCount = 0; // no quest work changes in fact
            }

            if (qinfo.SourceSpellID != 0)
            {
                var spellInfo = Global.SpellMgr.GetSpellInfo(qinfo.SourceSpellID, Difficulty.None);

                if (spellInfo == null)
                {
                    Log.Logger.Error("QuestId {0} has `SourceSpellid` = {1} but spell {1} doesn't exist, quest can't be done.",
                                     qinfo.Id,
                                     qinfo.SourceSpellID);

                    qinfo.SourceSpellID = 0; // quest can't be done for this requirement
                }
                else if (!Global.SpellMgr.IsSpellValid(spellInfo))
                {
                    Log.Logger.Error("QuestId {0} has `SourceSpellid` = {1} but spell {1} is broken, quest can't be done.",
                                     qinfo.Id,
                                     qinfo.SourceSpellID);

                    qinfo.SourceSpellID = 0; // quest can't be done for this requirement
                }
            }

            foreach (var obj in qinfo.Objectives)
            {
                // Store objective for lookup by id
                _questObjectives[obj.Id] = obj;

                // Check storage index for objectives which store data
                if (obj.StorageIndex < 0)
                    switch (obj.Type)
                    {
                        case QuestObjectiveType.Monster:
                        case QuestObjectiveType.Item:
                        case QuestObjectiveType.GameObject:
                        case QuestObjectiveType.TalkTo:
                        case QuestObjectiveType.PlayerKills:
                        case QuestObjectiveType.AreaTrigger:
                        case QuestObjectiveType.WinPetBattleAgainstNpc:
                        case QuestObjectiveType.ObtainCurrency:
                            Log.Logger.Error("QuestId {0} objective {1} has invalid StorageIndex = {2} for objective type {3}", qinfo.Id, obj.Id, obj.StorageIndex, obj.Type);

                            break;
                        default:
                            break;
                    }

                switch (obj.Type)
                {
                    case QuestObjectiveType.Item:
                        if (GetItemTemplate((uint)obj.ObjectID) == null)
                            if (_configuration.GetDefaultValue("load.autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error($"QuestId {qinfo.Id} objective {obj.Id} has non existing item entry {obj.ObjectID}, quest can't be done.");

                        break;
                    case QuestObjectiveType.Monster:
                        if (GetCreatureTemplate((uint)obj.ObjectID) == null)
                            if (_configuration.GetDefaultValue("load.autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error($"QuestId {qinfo.Id} objective {obj.Id} has non existing creature entry {obj.ObjectID}, quest can't be done.");

                        break;
                    case QuestObjectiveType.GameObject:
                        if (GetGameObjectTemplate((uint)obj.ObjectID) == null)
                            if (_configuration.GetDefaultValue("load.autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error($"QuestId {qinfo.Id} objective {obj.Id} has non existing gameobject entry {obj.ObjectID}, quest can't be done.");

                        break;
                    case QuestObjectiveType.TalkTo:
                        if (GetCreatureTemplate((uint)obj.ObjectID) == null)
                            if (_configuration.GetDefaultValue("load.autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error($"QuestId {qinfo.Id} objective {obj.Id} has non existing creature entry {obj.ObjectID}, quest can't be done.");

                        break;
                    case QuestObjectiveType.MinReputation:
                    case QuestObjectiveType.MaxReputation:
                    case QuestObjectiveType.IncreaseReputation:
                        if (!_cliDB.FactionStorage.ContainsKey((uint)obj.ObjectID))
                            if (_configuration.GetDefaultValue("load.autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error("QuestId {0} objective {1} has non existing faction id {2}", qinfo.Id, obj.Id, obj.ObjectID);

                        break;
                    case QuestObjectiveType.PlayerKills:
                        if (obj.Amount <= 0)
                            if (_configuration.GetDefaultValue("load.autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error("QuestId {0} objective {1} has invalid player kills count {2}", qinfo.Id, obj.Id, obj.Amount);

                        break;
                    case QuestObjectiveType.Currency:
                    case QuestObjectiveType.HaveCurrency:
                    case QuestObjectiveType.ObtainCurrency:
                        if (!_cliDB.CurrencyTypesStorage.ContainsKey((uint)obj.ObjectID))
                            if (_configuration.GetDefaultValue("load.autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error("QuestId {0} objective {1} has non existing currency {2}", qinfo.Id, obj.Id, obj.ObjectID);

                        if (obj.Amount <= 0)
                            if (_configuration.GetDefaultValue("load.autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error("QuestId {0} objective {1} has invalid currency amount {2}", qinfo.Id, obj.Id, obj.Amount);

                        break;
                    case QuestObjectiveType.LearnSpell:
                        if (!Global.SpellMgr.HasSpellInfo((uint)obj.ObjectID, Difficulty.None))
                            if (_configuration.GetDefaultValue("load.autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error("QuestId {0} objective {1} has non existing spell id {2}", qinfo.Id, obj.Id, obj.ObjectID);

                        break;
                    case QuestObjectiveType.WinPetBattleAgainstNpc:
                        if (obj.ObjectID != 0 && GetCreatureTemplate((uint)obj.ObjectID) == null)
                            if (_configuration.GetDefaultValue("load.autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error("QuestId {0} objective {1} has non existing creature entry {2}, quest can't be done.", qinfo.Id, obj.Id, obj.ObjectID);

                        break;
                    case QuestObjectiveType.DefeatBattlePet:
                        if (!_cliDB.BattlePetSpeciesStorage.ContainsKey((uint)obj.ObjectID))
                            if (_configuration.GetDefaultValue("load.autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error("QuestId {0} objective {1} has non existing battlepet species id {2}", qinfo.Id, obj.Id, obj.ObjectID);

                        break;
                    case QuestObjectiveType.CriteriaTree:
                        if (!_cliDB.CriteriaTreeStorage.ContainsKey((uint)obj.ObjectID))
                            if (_configuration.GetDefaultValue("load.autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error("QuestId {0} objective {1} has non existing criteria tree id {2}", qinfo.Id, obj.Id, obj.ObjectID);

                        break;
                    case QuestObjectiveType.AreaTrigger:
                        if (!_cliDB.AreaTriggerStorage.ContainsKey((uint)obj.ObjectID) && obj.ObjectID != -1)
                            if (_configuration.GetDefaultValue("load.autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error("QuestId {0} objective {1} has non existing AreaTrigger.db2 id {2}", qinfo.Id, obj.Id, obj.ObjectID);

                        break;
                    case QuestObjectiveType.AreaTriggerEnter:
                    case QuestObjectiveType.AreaTriggerExit:
                        if (Global.AreaTriggerDataStorage.GetAreaTriggerTemplate(new AreaTriggerId((uint)obj.ObjectID, false)) == null && Global.AreaTriggerDataStorage.GetAreaTriggerTemplate(new AreaTriggerId((uint)obj.ObjectID, true)) != null)
                            if (_configuration.GetDefaultValue("load.autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error("QuestId {0} objective {1} has non existing areatrigger id {2}", qinfo.Id, obj.Id, obj.ObjectID);

                        break;
                    case QuestObjectiveType.Money:
                    case QuestObjectiveType.WinPvpPetBattles:
                    case QuestObjectiveType.ProgressBar:
                        break;
                    default:
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                        else
                            Log.Logger.Error("QuestId {0} objective {1} has unhandled type {2}", qinfo.Id, obj.Id, obj.Type);

                        break;
                }

                if (obj.Flags.HasAnyFlag(QuestObjectiveFlags.Sequenced))
                    qinfo.SetSpecialFlag(QuestSpecialFlags.SequencedObjectives);
            }

            for (var j = 0; j < SharedConst.QuestItemDropCount; j++)
            {
                var id = qinfo.ItemDrop[j];

                if (id != 0)
                {
                    if (GetItemTemplate(id) == null)
                        Log.Logger.Error("QuestId {0} has `RequiredSourceItemId{1}` = {2} but item with entry {2} does not exist, quest can't be done.",
                                         qinfo.Id,
                                         j + 1,
                                         id);
                    // no changes, quest can't be done for this requirement
                }
                else
                {
                    if (qinfo.ItemDropQuantity[j] > 0)
                        Log.Logger.Error("QuestId {0} has `RequiredSourceItemId{1}` = 0 but `RequiredSourceItemCount{1}` = {2}.",
                                         qinfo.Id,
                                         j + 1,
                                         qinfo.ItemDropQuantity[j]);
                    // no changes, quest ignore this data
                }
            }

            for (var j = 0; j < SharedConst.QuestRewardChoicesCount; ++j)
            {
                var id = qinfo.RewardChoiceItemId[j];

                if (id != 0)
                {
                    switch (qinfo.RewardChoiceItemType[j])
                    {
                        case LootItemType.Item:
                            if (GetItemTemplate(id) == null)
                            {
                                Log.Logger.Error($"QuestId {qinfo.Id} has `RewardChoiceItemId{j + 1}` = {id} but item with entry {id} does not exist, quest will not reward this item.");
                                qinfo.RewardChoiceItemId[j] = 0; // no changes, quest will not reward this
                            }

                            break;
                        case LootItemType.Currency:
                            if (!_cliDB.CurrencyTypesStorage.HasRecord(id))
                            {
                                Log.Logger.Error($"QuestId {qinfo.Id} has `RewardChoiceItemId{j + 1}` = {id} but currency with id {id} does not exist, quest will not reward this currency.");
                                qinfo.RewardChoiceItemId[j] = 0; // no changes, quest will not reward this
                            }

                            break;
                        default:
                            Log.Logger.Error($"QuestId {qinfo.Id} has `RewardChoiceItemType{j + 1}` = {qinfo.RewardChoiceItemType[j]} but it is not a valid item type, reward removed.");
                            qinfo.RewardChoiceItemId[j] = 0;

                            break;
                    }

                    if (qinfo.RewardChoiceItemCount[j] == 0)
                        Log.Logger.Error($"QuestId {qinfo.Id} has `RewardChoiceItemId{j + 1}` = {id} but `RewardChoiceItemCount{j + 1}` = 0, quest can't be done.");
                }
                else if (qinfo.RewardChoiceItemCount[j] > 0)
                {
                    Log.Logger.Error($"QuestId {qinfo.Id} has `RewardChoiceItemId{j + 1}` = 0 but `RewardChoiceItemCount{j + 1}` = {qinfo.RewardChoiceItemCount[j]}.");
                    // no changes, quest ignore this data
                }
            }

            for (var j = 0; j < SharedConst.QuestRewardItemCount; ++j)
            {
                var id = qinfo.RewardItemId[j];

                if (id != 0)
                {
                    if (GetItemTemplate(id) == null)
                    {
                        Log.Logger.Error("QuestId {0} has `RewardItemId{1}` = {2} but item with entry {3} does not exist, quest will not reward this item.",
                                         qinfo.Id,
                                         j + 1,
                                         id,
                                         id);

                        qinfo.RewardItemId[j] = 0; // no changes, quest will not reward this item
                    }

                    if (qinfo.RewardItemCount[j] == 0)
                        Log.Logger.Error("QuestId {0} has `RewardItemId{1}` = {2} but `RewardItemIdCount{3}` = 0, quest will not reward this item.",
                                         qinfo.Id,
                                         j + 1,
                                         id,
                                         j + 1);
                    // no changes
                }
                else if (qinfo.RewardItemCount[j] > 0)
                {
                    Log.Logger.Error("QuestId {0} has `RewardItemId{1}` = 0 but `RewardItemIdCount{2}` = {3}.",
                                     qinfo.Id,
                                     j + 1,
                                     j + 1,
                                     qinfo.RewardItemCount[j]);
                    // no changes, quest ignore this data
                }
            }

            for (var j = 0; j < SharedConst.QuestRewardReputationsCount; ++j)
                if (qinfo.RewardFactionId[j] != 0)
                {
                    if (Math.Abs(qinfo.RewardFactionValue[j]) > 9)
                        Log.Logger.Error("QuestId {0} has RewardFactionValueId{1} = {2}. That is outside the range of valid values (-9 to 9).", qinfo.Id, j + 1, qinfo.RewardFactionValue[j]);

                    if (!_cliDB.FactionStorage.ContainsKey(qinfo.RewardFactionId[j]))
                    {
                        Log.Logger.Error("QuestId {0} has `RewardFactionId{1}` = {2} but raw faction (faction.dbc) {3} does not exist, quest will not reward reputation for this faction.",
                                         qinfo.Id,
                                         j + 1,
                                         qinfo.RewardFactionId[j],
                                         qinfo.RewardFactionId[j]);

                        qinfo.RewardFactionId[j] = 0; // quest will not reward this
                    }
                }

                else if (qinfo.RewardFactionOverride[j] != 0)
                {
                    Log.Logger.Error("QuestId {0} has `RewardFactionId{1}` = 0 but `RewardFactionValueIdOverride{2}` = {3}.",
                                     qinfo.Id,
                                     j + 1,
                                     j + 1,
                                     qinfo.RewardFactionOverride[j]);
                    // no changes, quest ignore this data
                }

            if (qinfo.RewardSpell > 0)
            {
                var spellInfo = Global.SpellMgr.GetSpellInfo(qinfo.RewardSpell, Difficulty.None);

                if (spellInfo == null)
                {
                    Log.Logger.Error("QuestId {0} has `RewardSpellCast` = {1} but spell {2} does not exist, quest will not have a spell reward.",
                                     qinfo.Id,
                                     qinfo.RewardSpell,
                                     qinfo.RewardSpell);

                    qinfo.RewardSpell = 0; // no spell will be casted on player
                }

                else if (!Global.SpellMgr.IsSpellValid(spellInfo))
                {
                    Log.Logger.Error("QuestId {0} has `RewardSpellCast` = {1} but spell {2} is broken, quest will not have a spell reward.",
                                     qinfo.Id,
                                     qinfo.RewardSpell,
                                     qinfo.RewardSpell);

                    qinfo.RewardSpell = 0; // no spell will be casted on player
                }
            }

            if (qinfo.RewardMailTemplateId != 0)
            {
                if (!_cliDB.MailTemplateStorage.ContainsKey(qinfo.RewardMailTemplateId))
                {
                    Log.Logger.Error("QuestId {0} has `RewardMailTemplateId` = {1} but mail template {2} does not exist, quest will not have a mail reward.",
                                     qinfo.Id,
                                     qinfo.RewardMailTemplateId,
                                     qinfo.RewardMailTemplateId);

                    qinfo.RewardMailTemplateId = 0; // no mail will send to player
                    qinfo.RewardMailDelay = 0;      // no mail will send to player
                    qinfo.RewardMailSenderEntry = 0;
                }
                else if (usedMailTemplates.ContainsKey(qinfo.RewardMailTemplateId))
                {
                    var usedId = usedMailTemplates.LookupByKey(qinfo.RewardMailTemplateId);

                    Log.Logger.Error("QuestId {0} has `RewardMailTemplateId` = {1} but mail template  {2} already used for quest {3}, quest will not have a mail reward.",
                                     qinfo.Id,
                                     qinfo.RewardMailTemplateId,
                                     qinfo.RewardMailTemplateId,
                                     usedId);

                    qinfo.RewardMailTemplateId = 0; // no mail will send to player
                    qinfo.RewardMailDelay = 0;      // no mail will send to player
                    qinfo.RewardMailSenderEntry = 0;
                }
                else
                {
                    usedMailTemplates[qinfo.RewardMailTemplateId] = qinfo.Id;
                }
            }

            if (qinfo.NextQuestInChain != 0)
                if (!_questTemplates.ContainsKey(qinfo.NextQuestInChain))
                {
                    Log.Logger.Error("QuestId {0} has `NextQuestIdChain` = {1} but quest {2} does not exist, quest chain will not work.",
                                     qinfo.Id,
                                     qinfo.NextQuestInChain,
                                     qinfo.NextQuestInChain);

                    qinfo.NextQuestInChain = 0;
                }

            for (var j = 0; j < SharedConst.QuestRewardCurrencyCount; ++j)
                if (qinfo.RewardCurrencyId[j] != 0)
                {
                    if (qinfo.RewardCurrencyCount[j] == 0)
                        Log.Logger.Error("QuestId {0} has `RewardCurrencyId{1}` = {2} but `RewardCurrencyCount{3}` = 0, quest can't be done.",
                                         qinfo.Id,
                                         j + 1,
                                         qinfo.RewardCurrencyId[j],
                                         j + 1);

                    // no changes, quest can't be done for this requirement
                    if (!_cliDB.CurrencyTypesStorage.ContainsKey(qinfo.RewardCurrencyId[j]))
                    {
                        Log.Logger.Error("QuestId {0} has `RewardCurrencyId{1}` = {2} but currency with entry {3} does not exist, quest can't be done.",
                                         qinfo.Id,
                                         j + 1,
                                         qinfo.RewardCurrencyId[j],
                                         qinfo.RewardCurrencyId[j]);

                        qinfo.RewardCurrencyCount[j] = 0; // prevent incorrect work of quest
                    }
                }
                else if (qinfo.RewardCurrencyCount[j] > 0)
                {
                    Log.Logger.Error("QuestId {0} has `RewardCurrencyId{1}` = 0 but `RewardCurrencyCount{2}` = {3}, quest can't be done.",
                                     qinfo.Id,
                                     j + 1,
                                     j + 1,
                                     qinfo.RewardCurrencyCount[j]);

                    qinfo.RewardCurrencyCount[j] = 0; // prevent incorrect work of quest
                }

            if (qinfo.SoundAccept != 0)
                if (!_cliDB.SoundKitStorage.ContainsKey(qinfo.SoundAccept))
                {
                    Log.Logger.Error("QuestId {0} has `SoundAccept` = {1} but sound {2} does not exist, set to 0.",
                                     qinfo.Id,
                                     qinfo.SoundAccept,
                                     qinfo.SoundAccept);

                    qinfo.SoundAccept = 0; // no sound will be played
                }

            if (qinfo.SoundTurnIn != 0)
                if (!_cliDB.SoundKitStorage.ContainsKey(qinfo.SoundTurnIn))
                {
                    Log.Logger.Error("QuestId {0} has `SoundTurnIn` = {1} but sound {2} does not exist, set to 0.",
                                     qinfo.Id,
                                     qinfo.SoundTurnIn,
                                     qinfo.SoundTurnIn);

                    qinfo.SoundTurnIn = 0; // no sound will be played
                }

            if (qinfo.RewardSkillId > 0)
            {
                if (!_cliDB.SkillLineStorage.ContainsKey(qinfo.RewardSkillId))
                    Log.Logger.Error("QuestId {0} has `RewardSkillId` = {1} but this skill does not exist",
                                     qinfo.Id,
                                     qinfo.RewardSkillId);

                if (qinfo.RewardSkillPoints == 0)
                    Log.Logger.Error("QuestId {0} has `RewardSkillId` = {1} but `RewardSkillPoints` is 0",
                                     qinfo.Id,
                                     qinfo.RewardSkillId);
            }

            if (qinfo.RewardSkillPoints != 0)
            {
                if (qinfo.RewardSkillPoints > Global.WorldMgr.ConfigMaxSkillValue)
                    Log.Logger.Error("QuestId {0} has `RewardSkillPoints` = {1} but max possible skill is {2}, quest can't be done.",
                                     qinfo.Id,
                                     qinfo.RewardSkillPoints,
                                     Global.WorldMgr.ConfigMaxSkillValue);

                // no changes, quest can't be done for this requirement
                if (qinfo.RewardSkillId == 0)
                    Log.Logger.Error("QuestId {0} has `RewardSkillPoints` = {1} but `RewardSkillId` is 0",
                                     qinfo.Id,
                                     qinfo.RewardSkillPoints);
            }

            // fill additional data stores
            var prevQuestId = (uint)Math.Abs(qinfo.PrevQuestId);

            if (prevQuestId != 0)
            {
                var prevQuestItr = _questTemplates.LookupByKey(prevQuestId);

                if (prevQuestItr == null)
                    Log.Logger.Error($"QuestId {qinfo.Id} has PrevQuestId {prevQuestId}, but no such quest");
                else if (prevQuestItr.BreadcrumbForQuestId != 0)
                    Log.Logger.Error($"QuestId {qinfo.Id} should not be unlocked by breadcrumb quest {prevQuestId}");
                else if (qinfo.PrevQuestId > 0)
                    qinfo.DependentPreviousQuests.Add(prevQuestId);
            }

            if (qinfo.NextQuestId != 0)
            {
                var nextquest = _questTemplates.LookupByKey(qinfo.NextQuestId);

                if (nextquest == null)
                    Log.Logger.Error("QuestId {0} has NextQuestId {1}, but no such quest", qinfo.Id, qinfo.NextQuestId);
                else
                    nextquest.DependentPreviousQuests.Add(qinfo.Id);
            }

            var breadcrumbForQuestId = (uint)Math.Abs(qinfo.BreadcrumbForQuestId);

            if (breadcrumbForQuestId != 0)
            {
                if (!_questTemplates.ContainsKey(breadcrumbForQuestId))
                {
                    Log.Logger.Error($"QuestId {qinfo.Id} is a breadcrumb for quest {breadcrumbForQuestId}, but no such quest exists");
                    qinfo.BreadcrumbForQuestId = 0;
                }

                if (qinfo.NextQuestId != 0)
                    Log.Logger.Error($"QuestId {qinfo.Id} is a breadcrumb, should not unlock quest {qinfo.NextQuestId}");
            }

            if (qinfo.ExclusiveGroup != 0)
                _exclusiveQuestGroups.Add(qinfo.ExclusiveGroup, qinfo.Id);
        }

        foreach (var questPair in _questTemplates)
        {
            // skip post-loading checks for disabled quests
            if (Global.DisableMgr.IsDisabledFor(DisableType.Quest, questPair.Key, null))
                continue;

            var qinfo = questPair.Value;
            var qid = qinfo.Id;
            var breadcrumbForQuestId = (uint)Math.Abs(qinfo.BreadcrumbForQuestId);
            List<uint> questSet = new();

            while (breadcrumbForQuestId != 0)
            {
                //a previously visited quest was found as a breadcrumb quest
                //breadcrumb loop found!
                if (questSet.Contains(qinfo.Id))
                {
                    Log.Logger.Error($"Breadcrumb quests {qid} and {breadcrumbForQuestId} are in a loop");
                    qinfo.BreadcrumbForQuestId = 0;

                    break;
                }

                questSet.Add(qinfo.Id);

                qinfo = GetQuestTemplate(breadcrumbForQuestId);

                //every quest has a list of every breadcrumb towards it
                qinfo.DependentBreadcrumbQuests.Add(qid);

                breadcrumbForQuestId = (uint)Math.Abs(qinfo.BreadcrumbForQuestId);
            }
        }

        // check QUEST_SPECIAL_FLAGS_EXPLORATION_OR_EVENT for spell with SPELL_EFFECT_QUEST_COMPLETE
        foreach (var spellNameEntry in _cliDB.SpellNameStorage.Values)
        {
            var spellInfo = Global.SpellMgr.GetSpellInfo(spellNameEntry.Id, Difficulty.None);

            if (spellInfo == null)
                continue;

            foreach (var spellEffectInfo in spellInfo.Effects)
            {
                if (spellEffectInfo.Effect != SpellEffectName.QuestComplete)
                    continue;

                var questId = (uint)spellEffectInfo.MiscValue;
                var quest = GetQuestTemplate(questId);

                // some quest referenced in spells not exist (outdated spells)
                if (quest == null)
                    continue;

                if (!quest.HasSpecialFlag(QuestSpecialFlags.ExplorationOrEvent))
                {
                    Log.Logger.Error("Spell (id: {0}) have SPELL_EFFECT_QUEST_COMPLETE for quest {1}, but quest not have Id QUEST_SPECIAL_FLAGS_EXPLORATION_OR_EVENT. " +
                                     "QuestId flags must be fixed, quest modified to enable objective.",
                                     spellInfo.Id,
                                     questId);

                    // this will prevent quest completing without objective
                    quest.SetSpecialFlag(QuestSpecialFlags.ExplorationOrEvent);
                }
            }
        }

        // Make all paragon reward quests repeatable
        foreach (var paragonReputation in _cliDB.ParagonReputationStorage.Values)
        {
            var quest = GetQuestTemplate((uint)paragonReputation.QuestID);

            quest?.SetSpecialFlag(QuestSpecialFlags.Repeatable);
        }

        Log.Logger.Information("Loaded {0} quests definitions in {1} ms", _questTemplates.Count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadQuestStartersAndEnders()
    {
        Log.Logger.Information("Loading GO Start QuestId Data...");
        LoadGameobjectQuestStarters();
        Log.Logger.Information("Loading GO End QuestId Data...");
        LoadGameobjectQuestEnders();
        Log.Logger.Information("Loading Creature Start QuestId Data...");
        LoadCreatureQuestStarters();
        Log.Logger.Information("Loading Creature End QuestId Data...");
        LoadCreatureQuestEnders();
    }

    public void LoadQuestTemplateLocale()
    {
        var oldMSTime = Time.MSTime;

        _questObjectivesLocaleStorage.Clear(); // need for reload case

        //                                         0     1     2           3                 4                5                 6                  7                   8                   9                  10
        var result = _worldDatabase.Query("SELECT Id, locale, LogTitle, LogDescription, QuestDescription, AreaDescription, PortraitGiverText, PortraitGiverName, PortraitTurnInText, PortraitTurnInName, QuestCompletionLog" +
                                          " FROM quest_template_locale");

        if (result.IsEmpty())
            return;

        do
        {
            var id = result.Read<uint>(0);
            var localeName = result.Read<string>(1);
            var locale = localeName.ToEnum<Locale>();

            if (!SharedConst.IsValidLocale(locale) || locale == Locale.enUS)
                continue;

            if (!_questTemplateLocaleStorage.ContainsKey(id))
                _questTemplateLocaleStorage[id] = new QuestTemplateLocale();

            var data = _questTemplateLocaleStorage[id];
            AddLocaleString(result.Read<string>(2), locale, data.LogTitle);
            AddLocaleString(result.Read<string>(3), locale, data.LogDescription);
            AddLocaleString(result.Read<string>(4), locale, data.QuestDescription);
            AddLocaleString(result.Read<string>(5), locale, data.AreaDescription);
            AddLocaleString(result.Read<string>(6), locale, data.PortraitGiverText);
            AddLocaleString(result.Read<string>(7), locale, data.PortraitGiverName);
            AddLocaleString(result.Read<string>(8), locale, data.PortraitTurnInText);
            AddLocaleString(result.Read<string>(9), locale, data.PortraitTurnInName);
            AddLocaleString(result.Read<string>(10), locale, data.QuestCompletionLog);
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} QuestId Tempalate locale strings in {1} ms", _questTemplateLocaleStorage.Count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadRaceAndClassExpansionRequirements()
    {
        var oldMSTime = Time.MSTime;
        _raceUnlockRequirementStorage.Clear();

        //                                         0       1          2
        var result = _worldDatabase.Query("SELECT raceID, expansion, achievementId FROM `race_unlock_requirement`");

        if (!result.IsEmpty())
        {
            uint count = 0;

            do
            {
                var raceID = result.Read<byte>(0);
                var expansion = result.Read<byte>(1);
                var achievementId = result.Read<uint>(2);

                var raceEntry = _cliDB.ChrRacesStorage.LookupByKey(raceID);

                if (raceEntry == null)
                {
                    Log.Logger.Error("Race {0} defined in `race_unlock_requirement` does not exists, skipped.", raceID);

                    continue;
                }

                if (expansion >= (int)Expansion.MaxAccountExpansions)
                {
                    Log.Logger.Error("Race {0} defined in `race_unlock_requirement` has incorrect expansion {1}, skipped.", raceID, expansion);

                    continue;
                }

                if (achievementId != 0 && !_cliDB.AchievementStorage.ContainsKey(achievementId))
                {
                    Log.Logger.Error($"Race {raceID} defined in `race_unlock_requirement` has incorrect achievement {achievementId}, skipped.");

                    continue;
                }

                RaceUnlockRequirement raceUnlockRequirement = new()
                {
                    Expansion = expansion,
                    AchievementId = achievementId
                };

                _raceUnlockRequirementStorage[raceID] = raceUnlockRequirement;

                ++count;
            } while (result.NextRow());

            Log.Logger.Information("Loaded {0} race expansion requirements in {1} ms.", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }
        else
        {
            Log.Logger.Information("Loaded 0 race expansion requirements. DB table `race_expansion_requirement` is empty.");
        }

        oldMSTime = Time.MSTime;
        _classExpansionRequirementStorage.Clear();

        //                               0        1       2                     3
        result = _worldDatabase.Query("SELECT ClassID, RaceID, ActiveExpansionLevel, AccountExpansionLevel FROM `class_expansion_requirement`");

        if (!result.IsEmpty())
        {
            Dictionary<byte, Dictionary<byte, Tuple<byte, byte>>> temp = new();
            var minRequirementForClass = new byte[(int)PlayerClass.Max];
            Array.Fill(minRequirementForClass, (byte)Expansion.Max);
            uint count = 0;

            do
            {
                var classID = result.Read<byte>(0);
                var raceID = result.Read<byte>(1);
                var activeExpansionLevel = result.Read<byte>(2);
                var accountExpansionLevel = result.Read<byte>(3);

                var classEntry = _cliDB.ChrClassesStorage.LookupByKey(classID);

                if (classEntry == null)
                {
                    Log.Logger.Error($"Class {classID} (race {raceID}) defined in `class_expansion_requirement` does not exists, skipped.");

                    continue;
                }

                var raceEntry = _cliDB.ChrRacesStorage.LookupByKey(raceID);

                if (raceEntry == null)
                {
                    Log.Logger.Error($"Race {raceID} (class {classID}) defined in `class_expansion_requirement` does not exists, skipped.");

                    continue;
                }

                if (activeExpansionLevel >= (int)Expansion.Max)
                {
                    Log.Logger.Error($"Class {classID} Race {raceID} defined in `class_expansion_requirement` has incorrect ActiveExpansionLevel {activeExpansionLevel}, skipped.");

                    continue;
                }

                if (accountExpansionLevel >= (int)Expansion.MaxAccountExpansions)
                {
                    Log.Logger.Error($"Class {classID} Race {raceID} defined in `class_expansion_requirement` has incorrect AccountExpansionLevel {accountExpansionLevel}, skipped.");

                    continue;
                }

                if (!temp.ContainsKey(raceID))
                    temp[raceID] = new Dictionary<byte, Tuple<byte, byte>>();

                temp[raceID][classID] = Tuple.Create(activeExpansionLevel, accountExpansionLevel);
                minRequirementForClass[classID] = Math.Min(minRequirementForClass[classID], activeExpansionLevel);

                ++count;
            } while (result.NextRow());

            foreach (var race in temp)
            {
                RaceClassAvailability raceClassAvailability = new()
                {
                    RaceID = race.Key
                };

                foreach (var class_ in race.Value)
                {
                    ClassAvailability classAvailability = new()
                    {
                        ClassID = class_.Key,
                        ActiveExpansionLevel = class_.Value.Item1,
                        AccountExpansionLevel = class_.Value.Item2,
                        MinActiveExpansionLevel = minRequirementForClass[class_.Key]
                    };

                    raceClassAvailability.Classes.Add(classAvailability);
                }

                _classExpansionRequirementStorage.Add(raceClassAvailability);
            }

            Log.Logger.Information($"Loaded {count} class expansion requirements in {Time.GetMSTimeDiffToNow(oldMSTime)} ms.");
        }
        else
        {
            Log.Logger.Information("Loaded 0 class expansion requirements. DB table `class_expansion_requirement` is empty.");
        }
    }

    public void LoadRealmNames()
    {
        var oldMSTime = Time.MSTime;
        _realmNameStorage.Clear();

        //                                         0   1
        var result = DB.Login.Query("SELECT id, name FROM `realmlist`");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 realm names. DB table `realmlist` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var realm = result.Read<uint>(0);
            var realmName = result.Read<string>(1);

            _realmNameStorage[realm] = realmName;

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} realm names in {1} ms.", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }
    public void LoadReputationOnKill()
    {
        var oldMSTime = Time.MSTime;

        // For reload case
        _repOnKillStorage.Clear();

        //                                                0            1                     2
        var result = _worldDatabase.Query("SELECT creature_id, RewOnKillRepFaction1, RewOnKillRepFaction2, " +
                                          //   3             4             5                   6             7             8                   9
                                          "IsTeamAward1, MaxStanding1, RewOnKillRepValue1, IsTeamAward2, MaxStanding2, RewOnKillRepValue2, TeamDependent " +
                                          "FROM creature_onkill_reputation");

        if (result.IsEmpty())
        {
            Log.Logger.Error("oaded 0 creature award reputation definitions. DB table `creature_onkill_reputation` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var creature_id = result.Read<uint>(0);

            ReputationOnKillEntry repOnKill = new()
            {
                RepFaction1 = result.Read<ushort>(1),
                RepFaction2 = result.Read<ushort>(2),
                IsTeamAward1 = result.Read<bool>(3),
                ReputationMaxCap1 = result.Read<byte>(4),
                RepValue1 = result.Read<int>(5),
                IsTeamAward2 = result.Read<bool>(6),
                ReputationMaxCap2 = result.Read<byte>(7),
                RepValue2 = result.Read<int>(8),
                TeamDependent = result.Read<bool>(9)
            };

            if (GetCreatureTemplate(creature_id) == null)
            {
                Log.Logger.Error("Table `creature_onkill_reputation` have data for not existed creature entry ({0}), skipped", creature_id);

                continue;
            }

            if (repOnKill.RepFaction1 != 0)
            {
                var factionEntry1 = _cliDB.FactionStorage.LookupByKey(repOnKill.RepFaction1);

                if (factionEntry1 == null)
                {
                    Log.Logger.Error("Faction (faction.dbc) {0} does not exist but is used in `creature_onkill_reputation`", repOnKill.RepFaction1);

                    continue;
                }
            }

            if (repOnKill.RepFaction2 != 0)
            {
                var factionEntry2 = _cliDB.FactionStorage.LookupByKey(repOnKill.RepFaction2);

                if (factionEntry2 == null)
                {
                    Log.Logger.Error("Faction (faction.dbc) {0} does not exist but is used in `creature_onkill_reputation`", repOnKill.RepFaction2);

                    continue;
                }
            }

            _repOnKillStorage[creature_id] = repOnKill;

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} creature award reputation definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    //General
    public void LoadReputationRewardRate()
    {
        var oldMSTime = Time.MSTime;

        _repRewardRateStorage.Clear(); // for reload case

        //                                          0          1             2                  3                  4                 5                      6             7
        var result = _worldDatabase.Query("SELECT faction, quest_rate, quest_daily_rate, quest_weekly_rate, quest_monthly_rate, quest_repeatable_rate, creature_rate, spell_rate FROM reputation_reward_rate");

        if (result.IsEmpty())
        {
            Log.Logger.Error("Loaded `reputation_reward_rate`, table is empty!");

            return;
        }

        uint count = 0;

        do
        {
            var factionId = result.Read<uint>(0);

            RepRewardRate repRate = new()
            {
                QuestRate = result.Read<float>(1),
                QuestDailyRate = result.Read<float>(2),
                QuestWeeklyRate = result.Read<float>(3),
                QuestMonthlyRate = result.Read<float>(4),
                QuestRepeatableRate = result.Read<float>(5),
                CreatureRate = result.Read<float>(6),
                SpellRate = result.Read<float>(7)
            };

            var factionEntry = _cliDB.FactionStorage.LookupByKey(factionId);

            if (factionEntry == null)
            {
                Log.Logger.Error("Faction (faction.dbc) {0} does not exist but is used in `reputation_reward_rate`", factionId);

                continue;
            }

            if (repRate.QuestRate < 0.0f)
            {
                Log.Logger.Error("Table reputation_reward_rate has quest_rate with invalid rate {0}, skipping data for faction {1}", repRate.QuestRate, factionId);

                continue;
            }

            if (repRate.QuestDailyRate < 0.0f)
            {
                Log.Logger.Error("Table reputation_reward_rate has quest_daily_rate with invalid rate {0}, skipping data for faction {1}", repRate.QuestDailyRate, factionId);

                continue;
            }

            if (repRate.QuestWeeklyRate < 0.0f)
            {
                Log.Logger.Error("Table reputation_reward_rate has quest_weekly_rate with invalid rate {0}, skipping data for faction {1}", repRate.QuestWeeklyRate, factionId);

                continue;
            }

            if (repRate.QuestMonthlyRate < 0.0f)
            {
                Log.Logger.Error("Table reputation_reward_rate has quest_monthly_rate with invalid rate {0}, skipping data for faction {1}", repRate.QuestMonthlyRate, factionId);

                continue;
            }

            if (repRate.QuestRepeatableRate < 0.0f)
            {
                Log.Logger.Error("Table reputation_reward_rate has quest_repeatable_rate with invalid rate {0}, skipping data for faction {1}", repRate.QuestRepeatableRate, factionId);

                continue;
            }

            if (repRate.CreatureRate < 0.0f)
            {
                Log.Logger.Error("Table reputation_reward_rate has creature_rate with invalid rate {0}, skipping data for faction {1}", repRate.CreatureRate, factionId);

                continue;
            }

            if (repRate.SpellRate < 0.0f)
            {
                Log.Logger.Error("Table reputation_reward_rate has spell_rate with invalid rate {0}, skipping data for faction {1}", repRate.SpellRate, factionId);

                continue;
            }

            _repRewardRateStorage[factionId] = repRate;

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} reputation_reward_rate in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadReputationSpilloverTemplate()
    {
        var oldMSTime = Time.MSTime;

        _repSpilloverTemplateStorage.Clear(); // for reload case

        //                                        0        1         2       3       4         5       6       7         8       9       10        11      12      13        14      15
        var result = _worldDatabase.Query("SELECT faction, faction1, rate_1, rank_1, faction2, rate_2, rank_2, faction3, rate_3, rank_3, faction4, rate_4, rank_4, faction5, rate_5, rank_5 FROM " +
                                          "reputation_spillover_template");

        if (result.IsEmpty())
        {
            Log.Logger.Error("Loaded `reputation_spillover_template`, table is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var factionId = result.Read<uint>(0);

            RepSpilloverTemplate repTemplate = new()
            {
                Faction =
                {
                    [0] = result.Read<uint>(1),
                    [1] = result.Read<uint>(4),
                    [2] = result.Read<uint>(7),
                    [3] = result.Read<uint>(10),
                    [4] = result.Read<uint>(13)
                },
                FactionRate =
                {
                    [0] = result.Read<float>(2),
                    [1] = result.Read<float>(5),
                    [2] = result.Read<float>(8),
                    [3] = result.Read<float>(11),
                    [4] = result.Read<float>(14)
                },
                FactionRank =
                {
                    [0] = result.Read<uint>(3),
                    [1] = result.Read<uint>(6),
                    [2] = result.Read<uint>(9),
                    [3] = result.Read<uint>(12),
                    [4] = result.Read<uint>(15)
                }
            };

            var factionEntry = _cliDB.FactionStorage.LookupByKey(factionId);

            if (factionEntry == null)
            {
                Log.Logger.Error("Faction (faction.dbc) {0} does not exist but is used in `reputation_spillover_template`", factionId);

                continue;
            }

            if (factionEntry.ParentFactionID == 0)
            {
                Log.Logger.Error("Faction (faction.dbc) {0} in `reputation_spillover_template` does not belong to any team, skipping", factionId);

                continue;
            }

            var invalidSpilloverFaction = false;

            for (var i = 0; i < 5; ++i)
                if (repTemplate.Faction[i] != 0)
                {
                    var factionSpillover = _cliDB.FactionStorage.LookupByKey(repTemplate.Faction[i]);

                    if (factionSpillover.Id == 0)
                    {
                        Log.Logger.Error("Spillover faction (faction.dbc) {0} does not exist but is used in `reputation_spillover_template` for faction {1}, skipping", repTemplate.Faction[i], factionId);
                        invalidSpilloverFaction = true;

                        break;
                    }

                    if (!factionSpillover.CanHaveReputation())
                    {
                        Log.Logger.Error("Spillover faction (faction.dbc) {0} for faction {1} in `reputation_spillover_template` can not be listed for client, and then useless, skipping",
                                         repTemplate.Faction[i],
                                         factionId);

                        invalidSpilloverFaction = true;

                        break;
                    }

                    if (repTemplate.FactionRank[i] >= (uint)ReputationRank.Max)
                    {
                        Log.Logger.Error("Rank {0} used in `reputation_spillover_template` for spillover faction {1} is not valid, skipping", repTemplate.FactionRank[i], repTemplate.Faction[i]);
                        invalidSpilloverFaction = true;

                        break;
                    }
                }

            if (invalidSpilloverFaction)
                continue;

            _repSpilloverTemplateStorage[factionId] = repTemplate;
            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} reputation_spillover_template in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadReservedPlayersNames()
    {
        var oldMSTime = Time.MSTime;

        _reservedNamesStorage.Clear(); // need for reload case

        var result = DB.Characters.Query("SELECT name FROM reserved_name");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 reserved player names. DB table `reserved_name` is empty!");

            return;
        }

        uint count = 0;

        do
        {
            var name = result.Read<string>(0);

            _reservedNamesStorage.Add(name.ToLower());
            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} reserved player names in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadSceneTemplates()
    {
        var oldMSTime = Time.MSTime;
        _sceneTemplateStorage.Clear();

        var result = _worldDatabase.Query("SELECT SceneId, Flags, ScriptPackageID, Encrypted, ScriptName FROM scene_template");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 scene templates. DB table `scene_template` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var sceneId = result.Read<uint>(0);

            SceneTemplate sceneTemplate = new()
            {
                SceneId = sceneId,
                PlaybackFlags = (SceneFlags)result.Read<uint>(1),
                ScenePackageId = result.Read<uint>(2),
                Encrypted = result.Read<byte>(3) != 0,
                ScriptId = GetScriptId(result.Read<string>(4))
            };

            _sceneTemplateStorage[sceneId] = sceneTemplate;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} scene templates in {1} ms.", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadSkillTiers()
    {
        var oldMSTime = Time.MSTime;

        _skillTiers.Clear();

        var result = _worldDatabase.Query("SELECT ID, Value1, Value2, Value3, Value4, Value5, Value6, Value7, Value8, Value9, Value10, " +
                                          " Value11, Value12, Value13, Value14, Value15, Value16 FROM skill_tiers");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 skill max values. DB table `skill_tiers` is empty.");

            return;
        }

        do
        {
            var id = result.Read<uint>(0);
            SkillTiersEntry tier = new();

            for (var i = 0; i < SkillConst.MaxSkillStep; ++i)
                tier.Value[i] = result.Read<uint>(1 + i);

            _skillTiers[id] = tier;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} skill max values in {1} ms", _skillTiers.Count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadSpawnGroups()
    {
        var oldMSTime = Time.MSTime;

        //                                         0        1          2
        var result = _worldDatabase.Query("SELECT groupId, spawnType, spawnId FROM spawn_group");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 spawn group members. DB table `spawn_group` is empty.");

            return;
        }

        uint numMembers = 0;

        do
        {
            var groupId = result.Read<uint>(0);
            var spawnType = (SpawnObjectType)result.Read<byte>(1);
            var spawnId = result.Read<ulong>(2);

            if (!SpawnMetadata.TypeIsValid(spawnType))
            {
                if (_configuration.GetDefaultValue("load.autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM spawn_group WHERE groupId = {groupId} AND spawnType = {(byte)spawnType} AND spawnId = {spawnId}");
                else
                    Log.Logger.Error($"Spawn data with invalid type {spawnType} listed for spawn group {groupId}. Skipped.");

                continue;
            }

            var data = GetSpawnMetadata(spawnType, spawnId);

            if (data == null)
            {
                if (_configuration.GetDefaultValue("load.autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM spawn_group WHERE groupId = {groupId} AND spawnType = {(byte)spawnType} AND spawnId = {spawnId}");
                else
                    Log.Logger.Error($"Spawn data with ID ({spawnType},{spawnId}) not found, but is listed as a member of spawn group {groupId}!");

                continue;
            }
            else if (data.SpawnGroupData.GroupId != 0)
            {
                if (_configuration.GetDefaultValue("load.autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM spawn_group WHERE groupId = {groupId} AND spawnType = {(byte)spawnType} AND spawnId = {spawnId}");
                else
                    Log.Logger.Error($"Spawn with ID ({spawnType},{spawnId}) is listed as a member of spawn group {groupId}, but is already a member of spawn group {data.SpawnGroupData.GroupId}. Skipping.");

                continue;
            }

            var groupTemplate = _spawnGroupDataStorage.LookupByKey(groupId);

            if (groupTemplate == null)
            {
                Log.Logger.Error($"Spawn group {groupId} assigned to spawn ID ({spawnType},{spawnId}), but group is found!");

                continue;
            }
            else
            {
                if (groupTemplate.MapId == 0xFFFFFFFF)
                {
                    groupTemplate.MapId = data.MapId;
                    _spawnGroupsByMap.Add(data.MapId, groupId);
                }
                else if (groupTemplate.MapId != data.MapId && !groupTemplate.Flags.HasAnyFlag(SpawnGroupFlags.System))
                {
                    if (_configuration.GetDefaultValue("load.autoclean", false))
                        _worldDatabase.Execute($"DELETE FROM spawn_group WHERE groupId = {groupId} AND spawnType = {(byte)spawnType} AND spawnId = {spawnId}");
                    else
                        Log.Logger.Error($"Spawn group {groupId} has map ID {groupTemplate.MapId}, but spawn ({spawnType},{spawnId}) has map id {data.MapId} - spawn NOT added to group!");

                    continue;
                }

                data.SpawnGroupData = groupTemplate;

                if (!groupTemplate.Flags.HasAnyFlag(SpawnGroupFlags.System))
                    _spawnGroupMapStorage.Add(groupId, data);

                ++numMembers;
            }
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {numMembers} spawn group members in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    public void LoadSpawnGroupTemplates()
    {
        var oldMSTime = Time.MSTime;

        //                                         0        1          2
        var result = _worldDatabase.Query("SELECT groupId, groupName, groupFlags FROM spawn_group_template");

        if (!result.IsEmpty())
            do
            {
                var groupId = result.Read<uint>(0);

                SpawnGroupTemplateData group = new()
                {
                    GroupId = groupId,
                    Name = result.Read<string>(1),
                    MapId = 0xFFFFFFFF
                };

                var flags = (SpawnGroupFlags)result.Read<uint>(2);

                if (flags.HasAnyFlag(~SpawnGroupFlags.All))
                {
                    flags &= SpawnGroupFlags.All;
                    Log.Logger.Error($"Invalid spawn group Id {flags} on group ID {groupId} ({group.Name}), reduced to valid Id {group.Flags}.");
                }

                if (flags.HasAnyFlag(SpawnGroupFlags.System) && flags.HasAnyFlag(SpawnGroupFlags.ManualSpawn))
                {
                    flags &= ~SpawnGroupFlags.ManualSpawn;
                    Log.Logger.Error($"System spawn group {groupId} ({group.Name}) has invalid manual spawn Id. Ignored.");
                }

                group.Flags = flags;

                _spawnGroupDataStorage[groupId] = group;
            } while (result.NextRow());

        if (!_spawnGroupDataStorage.ContainsKey(0))
        {
            Log.Logger.Error("Default spawn group (index 0) is missing from DB! Manually inserted.");

            SpawnGroupTemplateData data = new()
            {
                GroupId = 0,
                Name = "Default Group",
                MapId = 0,
                Flags = SpawnGroupFlags.System
            };

            _spawnGroupDataStorage[0] = data;
        }

        if (!_spawnGroupDataStorage.ContainsKey(1))
        {
            Log.Logger.Error("Default legacy spawn group (index 1) is missing from DB! Manually inserted.");

            SpawnGroupTemplateData data = new()
            {
                GroupId = 1,
                Name = "Legacy Group",
                MapId = 0,
                Flags = SpawnGroupFlags.System | SpawnGroupFlags.CompatibilityMode
            };

            _spawnGroupDataStorage[1] = data;
        }

        if (!result.IsEmpty())
            Log.Logger.Information($"Loaded {_spawnGroupDataStorage.Count} spawn group templates in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
        else
            Log.Logger.Information("Loaded 0 spawn group templates. DB table `spawn_group_template` is empty.");
    }

    public void LoadSpellScriptNames()
    {
        var oldMSTime = Time.MSTime;

        _spellScriptsStorage.Clear(); // need for reload case

        var result = _worldDatabase.Query("SELECT spell_id, ScriptName FROM spell_script_names");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 spell script names. DB table `spell_script_names` is empty!");

            return;
        }

        uint count = 0;

        do
        {
            var spellId = result.Read<int>(0);
            var scriptName = result.Read<string>(1);

            if (RegisterSpellScript(spellId, scriptName))
                ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} spell script names in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadSpellScripts()
    {
        LoadScripts(ScriptsType.Spell);

        // check ids
        foreach (var script in SpellScripts)
        {
            var spellId = script.Key & 0x00FFFFFF;
            var spellInfo = Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None);

            if (spellInfo == null)
            {
                Log.Logger.Error("Table `spell_scripts` has not existing spell (Id: {0}) as script id", spellId);

                continue;
            }

            var spellEffIndex = (byte)((script.Key >> 24) & 0x000000FF);

            if (spellEffIndex >= spellInfo.Effects.Count)
            {
                Log.Logger.Error($"Table `spell_scripts` has too high effect index {spellEffIndex} for spell (Id: {spellId}) as script id");

                continue;
            }

            //check for correct spellEffect
            if (spellInfo.GetEffect(spellEffIndex).Effect == 0 || (spellInfo.GetEffect(spellEffIndex).Effect != SpellEffectName.ScriptEffect && spellInfo.GetEffect(spellEffIndex).Effect != SpellEffectName.Dummy))
                Log.Logger.Error($"Table `spell_scripts` - spell {spellId} effect {spellEffIndex} is not SPELL_EFFECT_SCRIPT_EFFECT or SPELL_EFFECT_DUMMY");
        }
    }

    public void LoadTavernAreaTriggers()
    {
        var oldMSTime = Time.MSTime;

        _tavernAreaTriggerStorage.Clear(); // need for reload case

        var result = _worldDatabase.Query("SELECT id FROM areatrigger_tavern");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 tavern triggers. DB table `areatrigger_tavern` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            ++count;

            var Trigger_ID = result.Read<uint>(0);

            var atEntry = _cliDB.AreaTriggerStorage.LookupByKey(Trigger_ID);

            if (atEntry == null)
            {
                Log.Logger.Error("Area trigger (ID:{0}) does not exist in `AreaTrigger.dbc`.", Trigger_ID);

                continue;
            }

            _tavernAreaTriggerStorage.Add(Trigger_ID);
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} tavern triggers in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadTempSummons()
    {
        var oldMSTime = Time.MSTime;

        _tempSummonDataStorage.Clear(); // needed for reload case

        //                                             0           1             2        3      4           5           6           7            8           9
        var result = _worldDatabase.Query("SELECT summonerId, summonerType, groupId, entry, position_x, position_y, position_z, orientation, summonType, summonTime FROM creature_summon_groups");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 temp summons. DB table `creature_summon_groups` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var summonerId = result.Read<uint>(0);
            var summonerType = (SummonerType)result.Read<byte>(1);
            var group = result.Read<byte>(2);

            switch (summonerType)
            {
                case SummonerType.Creature:
                    if (GetCreatureTemplate(summonerId) == null)
                    {
                        Log.Logger.Error("Table `creature_summon_groups` has summoner with non existing entry {0} for creature summoner type, skipped.", summonerId);

                        continue;
                    }

                    break;
                case SummonerType.GameObject:
                    if (GetGameObjectTemplate(summonerId) == null)
                    {
                        Log.Logger.Error("Table `creature_summon_groups` has summoner with non existing entry {0} for gameobject summoner type, skipped.", summonerId);

                        continue;
                    }

                    break;
                case SummonerType.Map:
                    if (!_cliDB.MapStorage.ContainsKey(summonerId))
                    {
                        Log.Logger.Error("Table `creature_summon_groups` has summoner with non existing entry {0} for map summoner type, skipped.", summonerId);

                        continue;
                    }

                    break;
                default:
                    Log.Logger.Error("Table `creature_summon_groups` has unhandled summoner type {0} for summoner {1}, skipped.", summonerType, summonerId);

                    continue;
            }

            TempSummonData data = new()
            {
                entry = result.Read<uint>(3)
            };

            if (GetCreatureTemplate(data.entry) == null)
            {
                Log.Logger.Error("Table `creature_summon_groups` has creature in group [Summoner ID: {0}, Summoner Type: {1}, Group ID: {2}] with non existing creature entry {3}, skipped.",
                                 summonerId,
                                 summonerType,
                                 group,
                                 data.entry);

                continue;
            }

            var posX = result.Read<float>(4);
            var posY = result.Read<float>(5);
            var posZ = result.Read<float>(6);
            var orientation = result.Read<float>(7);

            data.pos = new Position(posX, posY, posZ, orientation);

            data.type = (TempSummonType)result.Read<byte>(8);

            if (data.type > TempSummonType.ManualDespawn)
            {
                Log.Logger.Error("Table `creature_summon_groups` has unhandled temp summon type {0} in group [Summoner ID: {1}, Summoner Type: {2}, Group ID: {3}] for creature entry {4}, skipped.",
                                 data.type,
                                 summonerId,
                                 summonerType,
                                 group,
                                 data.entry);

                continue;
            }

            data.time = result.Read<uint>(9);

            var key = Tuple.Create(summonerId, summonerType, group);
            _tempSummonDataStorage.Add(key, data);

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} temp summons in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadTrainers()
    {
        var oldMSTime = Time.MSTime;

        // For reload case
        _trainers.Clear();

        MultiMap<uint, TrainerSpell> spellsByTrainer = new();
        var trainerSpellsResult = _worldDatabase.Query("SELECT TrainerId, SpellId, MoneyCost, ReqSkillLine, ReqSkillRank, ReqAbility1, ReqAbility2, ReqAbility3, ReqLevel FROM trainer_spell");

        if (!trainerSpellsResult.IsEmpty())
            do
            {
                TrainerSpell spell = new();
                var trainerId = trainerSpellsResult.Read<uint>(0);
                spell.SpellId = trainerSpellsResult.Read<uint>(1);
                spell.MoneyCost = trainerSpellsResult.Read<uint>(2);
                spell.ReqSkillLine = trainerSpellsResult.Read<uint>(3);
                spell.ReqSkillRank = trainerSpellsResult.Read<uint>(4);
                spell.ReqAbility[0] = trainerSpellsResult.Read<uint>(5);
                spell.ReqAbility[1] = trainerSpellsResult.Read<uint>(6);
                spell.ReqAbility[2] = trainerSpellsResult.Read<uint>(7);
                spell.ReqLevel = trainerSpellsResult.Read<byte>(8);

                var spellInfo = Global.SpellMgr.GetSpellInfo(spell.SpellId, Difficulty.None);

                if (spellInfo == null)
                {
                    Log.Logger.Error($"Table `trainer_spell` references non-existing spell (SpellId: {spell.SpellId}) for TrainerId {trainerId}, ignoring");

                    continue;
                }

                if (spell.ReqSkillLine != 0 && !_cliDB.SkillLineStorage.ContainsKey(spell.ReqSkillLine))
                {
                    Log.Logger.Error($"Table `trainer_spell` references non-existing skill (ReqSkillLine: {spell.ReqSkillLine}) for TrainerId {trainerId} and SpellId {spell.SpellId}, ignoring");

                    continue;
                }

                var allReqValid = true;

                for (var i = 0; i < spell.ReqAbility.Count; ++i)
                {
                    var requiredSpell = spell.ReqAbility[i];

                    if (requiredSpell != 0 && !Global.SpellMgr.HasSpellInfo(requiredSpell, Difficulty.None))
                    {
                        Log.Logger.Error($"Table `trainer_spell` references non-existing spell (ReqAbility {i + 1}: {requiredSpell}) for TrainerId {trainerId} and SpellId {spell.SpellId}, ignoring");
                        allReqValid = false;
                    }
                }

                if (!allReqValid)
                    continue;

                spellsByTrainer.Add(trainerId, spell);
            } while (trainerSpellsResult.NextRow());

        var trainersResult = _worldDatabase.Query("SELECT Id, Type, Greeting FROM trainer");

        if (!trainersResult.IsEmpty())
            do
            {
                var trainerId = trainersResult.Read<uint>(0);
                var trainerType = (TrainerType)trainersResult.Read<byte>(1);
                var greeting = trainersResult.Read<string>(2);
                List<TrainerSpell> spells = new();
                var spellList = spellsByTrainer.LookupByKey(trainerId);

                if (spellList != null)
                {
                    spells = spellList;
                    spellsByTrainer.Remove(trainerId);
                }

                _trainers.Add(trainerId, new Trainer(trainerId, trainerType, greeting, spells, _classFactory.Resolve<ConditionManager>(), _classFactory.Resolve<BattlePetMgrData>(), _classFactory.Resolve<SpellManager>()));
            } while (trainersResult.NextRow());

        foreach (var unusedSpells in spellsByTrainer.KeyValueList)
            Log.Logger.Error($"Table `trainer_spell` references non-existing trainer (TrainerId: {unusedSpells.Key}) for SpellId {unusedSpells.Value.SpellId}, ignoring");

        var trainerLocalesResult = _worldDatabase.Query("SELECT Id, locale, Greeting_lang FROM trainer_locale");

        if (!trainerLocalesResult.IsEmpty())
            do
            {
                var trainerId = trainerLocalesResult.Read<uint>(0);
                var localeName = trainerLocalesResult.Read<string>(1);

                var locale = localeName.ToEnum<Locale>();

                if (!SharedConst.IsValidLocale(locale) || locale == Locale.enUS)
                    continue;

                var trainer = _trainers.LookupByKey(trainerId);

                if (trainer != null)
                    trainer.AddGreetingLocale(locale, trainerLocalesResult.Read<string>(2));
                else
                    Log.Logger.Error($"Table `trainer_locale` references non-existing trainer (TrainerId: {trainerId}) for locale {localeName}, ignoring");
            } while (trainerLocalesResult.NextRow());

        Log.Logger.Information($"Loaded {_trainers.Count} Trainers in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    public void LoadVehicleAccessories()
    {
        var oldMSTime = Time.MSTime;

        _vehicleAccessoryStore.Clear(); // needed for reload case

        uint count = 0;

        //                                          0             1             2          3           4             5
        var result = _worldDatabase.Query("SELECT `guid`, `accessory_entry`, `seat_id`, `minion`, `summontype`, `summontimer` FROM `vehicle_accessory`");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 Vehicle Accessories in {0} ms", Time.GetMSTimeDiffToNow(oldMSTime));

            return;
        }

        do
        {
            var uiGUID = result.Read<uint>(0);
            var uiAccessory = result.Read<uint>(1);
            var uiSeat = result.Read<sbyte>(2);
            var bMinion = result.Read<bool>(3);
            var uiSummonType = result.Read<byte>(4);
            var uiSummonTimer = result.Read<uint>(5);

            if (GetCreatureTemplate(uiAccessory) == null)
            {
                Log.Logger.Error("Table `vehicle_accessory`: Accessory {0} does not exist.", uiAccessory);

                continue;
            }

            _vehicleAccessoryStore.Add(uiGUID, new VehicleAccessory(uiAccessory, uiSeat, bMinion, uiSummonType, uiSummonTimer));

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} Vehicle Accessories in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadVehicleSeatAddon()
    {
        var oldMSTime = Time.MSTime;

        _vehicleSeatAddonStore.Clear(); // needed for reload case

        //                                          0            1                  2             3             4             5             6
        var result = _worldDatabase.Query("SELECT `SeatEntry`, `SeatOrientation`, `ExitParamX`, `ExitParamY`, `ExitParamZ`, `ExitParamO`, `ExitParamValue` FROM `vehicle_seat_addon`");

        if (result.IsEmpty())
        {
            Log.Logger.Error("Loaded 0 vehicle seat addons. DB table `vehicle_seat_addon` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var seatID = result.Read<uint>(0);
            var orientation = result.Read<float>(1);
            var exitX = result.Read<float>(2);
            var exitY = result.Read<float>(3);
            var exitZ = result.Read<float>(4);
            var exitO = result.Read<float>(5);
            var exitParam = result.Read<byte>(6);

            if (!_cliDB.VehicleSeatStorage.ContainsKey(seatID))
            {
                Log.Logger.Error($"Table `vehicle_seat_addon`: SeatID: {seatID} does not exist in VehicleSeat.dbc. Skipping entry.");

                continue;
            }

            // Sanitizing values
            if (orientation > MathF.PI * 2)
            {
                Log.Logger.Error($"Table `vehicle_seat_addon`: SeatID: {seatID} is using invalid angle offset value ({orientation}). Set Value to 0.");
                orientation = 0.0f;
            }

            if (exitParam >= (byte)VehicleExitParameters.VehicleExitParamMax)
            {
                Log.Logger.Error($"Table `vehicle_seat_addon`: SeatID: {seatID} is using invalid exit parameter value ({exitParam}). Setting to 0 (none).");

                continue;
            }

            _vehicleSeatAddonStore[seatID] = new VehicleSeatAddon(orientation, exitX, exitY, exitZ, exitO, exitParam);

            ++count;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {count} Vehicle Seat Addon entries in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    //Vehicles
    public void LoadVehicleTemplate()
    {
        var oldMSTime = Time.MSTime;

        _vehicleTemplateStore.Clear();

        //                                         0           1
        var result = _worldDatabase.Query("SELECT creatureId, despawnDelayMs FROM vehicle_template");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 vehicle template. DB table `vehicle_template` is empty.");

            return;
        }

        do
        {
            var creatureId = result.Read<uint>(0);

            if (GetCreatureTemplate(creatureId) == null)
            {
                Log.Logger.Error($"Table `vehicle_template`: Vehicle {creatureId} does not exist.");

                continue;
            }

            VehicleTemplate vehicleTemplate = new()
            {
                DespawnDelay = TimeSpan.FromMilliseconds(result.Read<int>(1))
            };

            _vehicleTemplateStore[creatureId] = vehicleTemplate;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {_vehicleTemplateStore.Count} Vehicle Template entries in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    public void LoadVehicleTemplateAccessories()
    {
        var oldMSTime = Time.MSTime;

        _vehicleTemplateAccessoryStore.Clear(); // needed for reload case

        uint count = 0;

        //                                          0             1              2          3           4             5
        var result = _worldDatabase.Query("SELECT `entry`, `accessory_entry`, `seat_id`, `minion`, `summontype`, `summontimer` FROM `vehicle_template_accessory`");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 vehicle template accessories. DB table `vehicle_template_accessory` is empty.");

            return;
        }

        do
        {
            var entry = result.Read<uint>(0);
            var accessory = result.Read<uint>(1);
            var seatId = result.Read<sbyte>(2);
            var isMinion = result.Read<bool>(3);
            var summonType = result.Read<byte>(4);
            var summonTimer = result.Read<uint>(5);

            if (GetCreatureTemplate(entry) == null)
            {
                Log.Logger.Error("Table `vehicle_template_accessory`: creature template entry {0} does not exist.", entry);

                continue;
            }

            if (GetCreatureTemplate(accessory) == null)
            {
                Log.Logger.Error("Table `vehicle_template_accessory`: Accessory {0} does not exist.", accessory);

                continue;
            }

            if (!_spellClickInfoStorage.ContainsKey(entry))
            {
                Log.Logger.Error("Table `vehicle_template_accessory`: creature template entry {0} has no data in npc_spellclick_spells", entry);

                continue;
            }

            _vehicleTemplateAccessoryStore.Add(entry, new VehicleAccessory(accessory, seatId, isMinion, summonType, summonTimer));

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} Vehicle Template Accessories in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadVendors()
    {
        var time = Time.MSTime;
        // For reload case
        _cacheVendorItemStorage.Clear();

        List<uint> skipvendors = new();

        var result = _worldDatabase.Query("SELECT entry, item, maxcount, incrtime, ExtendedCost, type, BonusListIDs, PlayerConditionID, IgnoreFiltering FROM npc_vendor ORDER BY entry, slot ASC");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 Vendors. DB table `npc_vendor` is empty!");

            return;
        }

        uint count = 0;

        do
        {
            var entry = result.Read<uint>(0);
            var itemid = result.Read<int>(1);

            // if item is a negative, its a reference
            if (itemid < 0)
            {
                count += LoadReferenceVendor((int)entry, -itemid, skipvendors);
            }
            else
            {
                VendorItem vItem = new()
                {
                    Item = (uint)itemid,
                    Maxcount = result.Read<uint>(2),
                    Incrtime = result.Read<uint>(3),
                    ExtendedCost = result.Read<uint>(4),
                    Type = (ItemVendorType)result.Read<byte>(5),
                    PlayerConditionId = result.Read<uint>(7),
                    IgnoreFiltering = result.Read<bool>(8)
                };

                var bonusListIDsTok = new StringArray(result.Read<string>(6), ' ');

                if (!bonusListIDsTok.IsEmpty())
                    foreach (string token in bonusListIDsTok)
                        if (uint.TryParse(token, out var id))
                            vItem.BonusListIDs.Add(id);

                if (!IsVendorItemValid(entry, vItem, null, skipvendors))
                    continue;

                if (_cacheVendorItemStorage.LookupByKey(entry) == null)
                    _cacheVendorItemStorage.Add(entry, new VendorItemData());

                _cacheVendorItemStorage[entry].AddItem(vItem);
                ++count;
            }
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} Vendors in {1} ms", count, Time.GetMSTimeDiffToNow(time));
    }

    //Load WP Scripts
    public void LoadWaypointScripts()
    {
        LoadScripts(ScriptsType.Waypoint);

        List<uint> actionSet = new();

        foreach (var script in WaypointScripts)
            actionSet.Add(script.Key);

        var stmt = _worldDatabase.GetPreparedStatement(WorldStatements.SEL_WAYPOINT_DATA_ACTION);
        var result = _worldDatabase.Query(stmt);

        if (!result.IsEmpty())
            do
            {
                var action = result.Read<uint>(0);

                actionSet.Remove(action);
            } while (result.NextRow());

        foreach (var id in actionSet)
            Log.Logger.Error("There is no waypoint which links to the waypoint script {0}", id);
    }

    public void LoadWorldSafeLocs()
    {
        var oldMSTime = Time.MSTime;

        //                                         0   1      2     3     4     5
        var result = _worldDatabase.Query("SELECT ID, MapID, LocX, LocY, LocZ, Facing FROM world_safe_locs");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 world locations. DB table `world_safe_locs` is empty.");

            return;
        }

        do
        {
            var id = result.Read<uint>(0);
            WorldLocation loc = new(result.Read<uint>(1), result.Read<float>(2), result.Read<float>(3), result.Read<float>(4), MathFunctions.DegToRad(result.Read<float>(5)));

            if (!GridDefines.IsValidMapCoord(loc))
            {
                Log.Logger.Error($"World location (ID: {id}) has a invalid position MapID: {loc.MapId} {loc}, skipped");

                continue;
            }

            WorldSafeLocsEntry worldSafeLocs = new()
            {
                Id = id,
                Loc = loc
            };

            _worldSafeLocs[id] = worldSafeLocs;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {_worldSafeLocs.Count} world locations {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }
    public CreatureData NewOrExistCreatureData(ulong spawnId)
    {
        if (!_creatureDataStorage.ContainsKey(spawnId))
            _creatureDataStorage[spawnId] = new CreatureData();

        return _creatureDataStorage[spawnId];
    }

    public GameObjectData NewOrExistGameObjectData(ulong spawnId)
    {
        if (!_gameObjectDataStorage.ContainsKey(spawnId))
            _gameObjectDataStorage[spawnId] = new GameObjectData();

        return _gameObjectDataStorage[spawnId];
    }

    public bool RegisterAreaTriggerScript(uint areaTriggerId, string scriptName)
    {
        _areaTriggerScriptStorage.AddUnique(areaTriggerId, GetScriptId(scriptName));

        return true;
    }

    public bool RegisterSpellScript(int spellId, string scriptName)
    {
        var allRanks = false;

        if (spellId < 0)
        {
            allRanks = true;
            spellId = -spellId;
        }

        return RegisterSpellScript((uint)spellId, scriptName, allRanks);
    }

    public bool RegisterSpellScript(uint spellId, string scriptName, bool allRanks = false)
    {
        var spellInfo = Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None);

        if (spellInfo == null)
        {
            Log.Logger.Error("Scriptname: `{0}` spell (Id: {1}) does not exist.", scriptName, spellId);

            return false;
        }

        if (allRanks)
        {
            if (!spellInfo.IsRanked)
                Log.Logger.Debug("Scriptname: `{0}` spell (Id: {1}) has no ranks of spell.", scriptName, spellId);

            while (spellInfo != null)
            {
                _spellScriptsStorage.AddUnique(spellInfo.Id, GetScriptId(scriptName));
                spellInfo = spellInfo.GetNextRankSpell();
            }
        }
        else
        {
            if (spellInfo.IsRanked)
                Log.Logger.Debug("Scriptname: `{0}` spell (Id: {1}) is ranked spell. Perhaps not all ranks are assigned to this script.", scriptName, spellId);

            _spellScriptsStorage.AddUnique(spellInfo.Id, GetScriptId(scriptName));
        }

        return true;
    }

    public void RemoveCreatureFromGrid(CreatureData data)
    {
        RemoveSpawnDataFromGrid(data);
    }

    public void RemoveGameObjectFromGrid(GameObjectData data)
    {
        RemoveSpawnDataFromGrid(data);
    }

    public void RemoveGraveYardLink(uint id, uint zoneId, TeamFaction team, bool persist = false)
    {
        var range = GraveYardStorage.LookupByKey(zoneId);

        if (range.Empty())
        {
            Log.Logger.Error("Table `game_graveyard_zone` incomplete: Zone {0} Team {1} does not have a linked graveyard.", zoneId, team);

            return;
        }

        var found = false;


        foreach (var data in range)
        {
            // skip not matching safezone id
            if (data.SafeLocId != id)
                continue;

            // skip enemy faction graveyard at same map (normal area, city, or Battleground)
            // team == 0 case can be at call from .neargrave
            if (data.Team != 0 && team != 0 && data.Team != (uint)team)
                continue;

            found = true;

            break;
        }

        // no match, return
        if (!found)
            return;

        // remove from links
        GraveYardStorage.Remove(zoneId);

        // remove link from DB
        if (persist)
        {
            var stmt = _worldDatabase.GetPreparedStatement(WorldStatements.DEL_GRAVEYARD_ZONE);

            stmt.AddValue(0, id);
            stmt.AddValue(1, zoneId);
            stmt.AddValue(2, (uint)team);

            _worldDatabase.Execute(stmt);
        }
    }
    public bool RemoveVendorItem(uint entry, uint item, ItemVendorType type, bool persist = true)
    {
        var iter = _cacheVendorItemStorage.LookupByKey(entry);

        if (iter == null)
            return false;

        if (!iter.RemoveItem(item, type))
            return false;

        if (persist)
        {
            var stmt = _worldDatabase.GetPreparedStatement(WorldStatements.DEL_NPC_VENDOR);

            stmt.AddValue(0, entry);
            stmt.AddValue(1, item);
            stmt.AddValue(2, (byte)type);

            _worldDatabase.Execute(stmt);
        }

        return true;
    }

    //not very fast function but it is called only once a day, or on starting-up
    public void ReturnOrDeleteOldMails(bool serverUp)
    {
        var oldMSTime = Time.MSTime;

        var curTime = GameTime.CurrentTime;
        var lt = Time.UnixTimeToDateTime(curTime).ToLocalTime();
        Log.Logger.Information("Returning mails current time: hour: {0}, minute: {1}, second: {2} ", lt.Hour, lt.Minute, lt.Second);

        PreparedStatement stmt;

        // Delete all old mails without item and without body immediately, if starting server
        if (!serverUp)
        {
            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_EMPTY_EXPIRED_MAIL);
            stmt.AddValue(0, curTime);
            DB.Characters.Execute(stmt);
        }

        stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_EXPIRED_MAIL);
        stmt.AddValue(0, curTime);
        var result = DB.Characters.Query(stmt);

        if (result.IsEmpty())
        {
            Log.Logger.Information("No expired mails found.");

            return; // any mails need to be returned or deleted
        }

        MultiMap<ulong, MailItemInfo> itemsCache = new();
        stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_EXPIRED_MAIL_ITEMS);
        stmt.AddValue(0, curTime);
        var items = DB.Characters.Query(stmt);

        if (!items.IsEmpty())
        {
            MailItemInfo item = new();

            do
            {
                item.item_guid = result.Read<uint>(0);
                item.item_template = result.Read<uint>(1);
                var mailId = result.Read<ulong>(2);
                itemsCache.Add(mailId, item);
            } while (items.NextRow());
        }

        uint deletedCount = 0;
        uint returnedCount = 0;

        do
        {
            var receiver = result.Read<ulong>(3);

            if (serverUp && Global.ObjAccessor.FindConnectedPlayer(ObjectGuid.Create(HighGuid.Player, receiver)))
                continue;

            Mail m = new()
            {
                messageID = result.Read<ulong>(0),
                messageType = (MailMessageType)result.Read<byte>(1),
                sender = result.Read<uint>(2),
                receiver = receiver
            };

            var has_items = result.Read<bool>(4);
            m.expire_time = result.Read<long>(5);
            m.deliver_time = 0;
            m.COD = result.Read<ulong>(6);
            m.checkMask = (MailCheckMask)result.Read<byte>(7);
            m.mailTemplateId = result.Read<ushort>(8);

            // Delete or return mail
            if (has_items)
            {
                // read items from cache
                var temp = itemsCache[m.messageID];
                Extensions.Swap(ref m.items, ref temp);

                // if it is mail from non-player, or if it's already return mail, it shouldn't be returned, but deleted
                if (m.messageType != MailMessageType.Normal || (m.checkMask.HasAnyFlag(MailCheckMask.CodPayment | MailCheckMask.Returned)))
                {
                    // mail open and then not returned
                    foreach (var itemInfo in m.items)
                    {
                        Item.DeleteFromDB(null, itemInfo.item_guid);
                        AzeriteItem.DeleteFromDB(null, itemInfo.item_guid);
                        AzeriteEmpoweredItem.DeleteFromDB(null, itemInfo.item_guid);
                    }

                    stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM_BY_ID);
                    stmt.AddValue(0, m.messageID);
                    DB.Characters.Execute(stmt);
                }
                else
                {
                    // Mail will be returned
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_MAIL_RETURNED);
                    stmt.AddValue(0, m.receiver);
                    stmt.AddValue(1, m.sender);
                    stmt.AddValue(2, curTime + 30 * Time.DAY);
                    stmt.AddValue(3, curTime);
                    stmt.AddValue(4, (byte)MailCheckMask.Returned);
                    stmt.AddValue(5, m.messageID);
                    DB.Characters.Execute(stmt);

                    foreach (var itemInfo in m.items)
                    {
                        // Update receiver in mail items for its proper delivery, and in instance_item for avoid lost item at sender delete
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_MAIL_ITEM_RECEIVER);
                        stmt.AddValue(0, m.sender);
                        stmt.AddValue(1, itemInfo.item_guid);
                        DB.Characters.Execute(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ITEM_OWNER);
                        stmt.AddValue(0, m.sender);
                        stmt.AddValue(1, itemInfo.item_guid);
                        DB.Characters.Execute(stmt);
                    }

                    ++returnedCount;

                    continue;
                }
            }

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_BY_ID);
            stmt.AddValue(0, m.messageID);
            DB.Characters.Execute(stmt);
            ++deletedCount;
        } while (result.NextRow());

        Log.Logger.Information("Processed {0} expired mails: {1} deleted and {2} returned in {3} ms", deletedCount + returnedCount, deletedCount, returnedCount, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public bool SetCreatureLinkedRespawn(ulong guidLow, ulong linkedGuidLow)
    {
        if (guidLow == 0)
            return false;

        var master = GetCreatureData(guidLow);
        var guid = ObjectGuid.Create(HighGuid.Creature, master.MapId, master.Id, guidLow);
        PreparedStatement stmt;

        if (linkedGuidLow == 0) // we're removing the linking
        {
            _linkedRespawnStorage.Remove(guid);
            stmt = _worldDatabase.GetPreparedStatement(WorldStatements.DEL_LINKED_RESPAWN);
            stmt.AddValue(0, guidLow);
            stmt.AddValue(1, (uint)CreatureLinkedRespawnType.CreatureToCreature);
            _worldDatabase.Execute(stmt);

            return true;
        }

        var slave = GetCreatureData(linkedGuidLow);

        if (slave == null)
        {
            Log.Logger.Error("Creature '{0}' linking to non-existent creature '{1}'.", guidLow, linkedGuidLow);

            return false;
        }

        var map = _cliDB.MapStorage.LookupByKey(master.MapId);

        if (map == null || !map.Instanceable() || (master.MapId != slave.MapId))
        {
            Log.Logger.Error("Creature '{0}' linking to '{1}' on an unpermitted map.", guidLow, linkedGuidLow);

            return false;
        }

        // they must have a possibility to meet (normal/heroic difficulty)
        if (!master.SpawnDifficulties.Intersect(slave.SpawnDifficulties).Any())
        {
            Log.Logger.Error("LinkedRespawn: Creature '{0}' linking to '{1}' with not corresponding spawnMask", guidLow, linkedGuidLow);

            return false;
        }

        var linkedGuid = ObjectGuid.Create(HighGuid.Creature, slave.MapId, slave.Id, linkedGuidLow);

        _linkedRespawnStorage[guid] = linkedGuid;
        stmt = _worldDatabase.GetPreparedStatement(WorldStatements.REP_LINKED_RESPAWN);
        stmt.AddValue(0, guidLow);
        stmt.AddValue(1, linkedGuidLow);
        stmt.AddValue(2, (uint)CreatureLinkedRespawnType.CreatureToCreature);
        _worldDatabase.Execute(stmt);

        return true;
    }

    public void SetHighestGuids()
    {
        var result = DB.Characters.Query("SELECT MAX(guid) FROM characters");

        if (!result.IsEmpty())
            GetGuidSequenceGenerator(HighGuid.Player).Set(result.Read<ulong>(0) + 1);

        result = DB.Characters.Query("SELECT MAX(guid) FROM item_instance");

        if (!result.IsEmpty())
            GetGuidSequenceGenerator(HighGuid.Item).Set(result.Read<ulong>(0) + 1);

        // Cleanup other tables from not existed guids ( >= hiItemGuid)
        DB.Characters.Execute("DELETE FROM character_inventory WHERE item >= {0}", GetGuidSequenceGenerator(HighGuid.Item).GetNextAfterMaxUsed()); // One-time query
        DB.Characters.Execute("DELETE FROM mail_items WHERE item_guid >= {0}", GetGuidSequenceGenerator(HighGuid.Item).GetNextAfterMaxUsed());     // One-time query

        DB.Characters.Execute("DELETE a, ab, ai FROM auctionhouse a LEFT JOIN auction_bidders ab ON ab.auctionId = a.id LEFT JOIN auction_items ai ON ai.auctionId = a.id WHERE ai.itemGuid >= '{0}'",
                              GetGuidSequenceGenerator(HighGuid.Item).GetNextAfterMaxUsed()); // One-time query

        DB.Characters.Execute("DELETE FROM guild_bank_item WHERE item_guid >= {0}", GetGuidSequenceGenerator(HighGuid.Item).GetNextAfterMaxUsed()); // One-time query

        result = _worldDatabase.Query("SELECT MAX(guid) FROM transports");

        if (!result.IsEmpty())
            GetGuidSequenceGenerator(HighGuid.Transport).Set(result.Read<ulong>(0) + 1);

        result = DB.Characters.Query("SELECT MAX(id) FROM auctionhouse");

        if (!result.IsEmpty())
            _auctionId = result.Read<uint>(0) + 1;

        result = DB.Characters.Query("SELECT MAX(id) FROM mail");

        if (!result.IsEmpty())
            _mailId = result.Read<ulong>(0) + 1;

        result = DB.Characters.Query("SELECT MAX(arenateamid) FROM arena_team");

        if (!result.IsEmpty())
            Global.ArenaTeamMgr.SetNextArenaTeamId(result.Read<uint>(0) + 1);

        result = DB.Characters.Query("SELECT MAX(maxguid) FROM ((SELECT MAX(setguid) AS maxguid FROM character_equipmentsets) UNION (SELECT MAX(setguid) AS maxguid FROM character_transmog_outfits)) allsets");

        if (!result.IsEmpty())
            _equipmentSetGuid = result.Read<ulong>(0) + 1;

        result = DB.Characters.Query("SELECT MAX(guildId) FROM guild");

        if (!result.IsEmpty())
            Global.GuildMgr.SetNextGuildId(result.Read<uint>(0) + 1);

        result = DB.Characters.Query("SELECT MAX(itemId) from character_void_storage");

        if (!result.IsEmpty())
            _voidItemId = result.Read<ulong>(0) + 1;

        result = _worldDatabase.Query("SELECT MAX(guid) FROM creature");

        if (!result.IsEmpty())
            _creatureSpawnId = result.Read<ulong>(0) + 1;

        result = _worldDatabase.Query("SELECT MAX(guid) FROM gameobject");

        if (!result.IsEmpty())
            _gameObjectSpawnId = result.Read<ulong>(0) + 1;
    }

    public bool TryGetGetCreatureMovementOverride(ulong spawnId, out CreatureMovementData movementData) => _creatureMovementOverrides.TryGetValue(spawnId, out movementData);

    public bool TryGetQuestTemplate(uint questId, out Quest.Quest quest)
    {
        return _questTemplates.TryGetValue(questId, out quest);
    }

    public void UnloadPhaseConditions()
    {
        foreach (var pair in _phaseInfoByArea.KeyValueList)
            pair.Value.Conditions.Clear();
    }

    public void ValidateSpellScripts()
    {
        var oldMSTime = Time.MSTime;

        if (_spellScriptsStorage.Empty())
        {
            Log.Logger.Information("Validated 0 scripts.");

            return;
        }

        uint count = 0;

        _spellScriptsStorage.RemoveIfMatching((script) =>
        {
            var spellEntry = Global.SpellMgr.GetSpellInfo(script.Key, Difficulty.None);

            var SpellScriptLoaders = ScriptManager.CreateSpellScriptLoaders(script.Key);

            foreach (var pair in SpellScriptLoaders)
            {
                var spellScript = pair.Key.GetSpellScript();
                var valid = true;

                if (spellScript == null)
                {
                    Log.Logger.Error("Functions GetSpellScript() of script `{0}` do not return object - script skipped", GetScriptName(pair.Value));
                    valid = false;
                }

                if (spellScript != null)
                {
                    spellScript._Init(pair.Key.GetName(), spellEntry.Id);
                    spellScript._Register();

                    if (!spellScript._Validate(spellEntry))
                        valid = false;
                }

                if (!valid)
                    return true;
            }

            var AuraScriptLoaders = ScriptManager.CreateAuraScriptLoaders(script.Key);

            foreach (var pair in AuraScriptLoaders)
            {
                var auraScript = pair.Key.GetAuraScript();
                var valid = true;

                if (auraScript == null)
                {
                    Log.Logger.Error("Functions GetAuraScript() of script `{0}` do not return object - script skipped", GetScriptName(pair.Value));
                    valid = false;
                }

                if (auraScript != null)
                {
                    auraScript._Init(pair.Key.GetName(), spellEntry.Id);
                    auraScript._Register();

                    if (!auraScript._Validate(spellEntry))
                        valid = false;
                }

                if (!valid)
                    return true;
            }

            ++count;

            return false;
        });

        Log.Logger.Information("Validated {0} scripts in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }
    private static LanguageType GetRealmLanguageType(bool create)
    {
        return (RealmZones)_configuration.GetDefaultValue("RealmZone", (int)RealmZones.Development) switch
        {
            RealmZones.Unknown => // any language
                LanguageType.Any,
            RealmZones.Development => LanguageType.Any,
            RealmZones.TestServer  => LanguageType.Any,
            RealmZones.QaServer    => LanguageType.Any,
            RealmZones.UnitedStates => // extended-Latin
                LanguageType.ExtendenLatin,
            RealmZones.Oceanic      => LanguageType.ExtendenLatin,
            RealmZones.LatinAmerica => LanguageType.ExtendenLatin,
            RealmZones.English      => LanguageType.ExtendenLatin,
            RealmZones.German       => LanguageType.ExtendenLatin,
            RealmZones.French       => LanguageType.ExtendenLatin,
            RealmZones.Spanish      => LanguageType.ExtendenLatin,
            RealmZones.Korea => // East-Asian
                LanguageType.EastAsia,
            RealmZones.Taiwan => LanguageType.EastAsia,
            RealmZones.China  => LanguageType.EastAsia,
            RealmZones.Russian => // Cyrillic
                LanguageType.Cyrillic,
            _ => create ? LanguageType.BasicLatin : LanguageType.Any
        };
    }

    private static bool IsCultureString(LanguageType culture, string str, bool numericOrSpace)
    {
        foreach (var wchar in str)
        {
            if (numericOrSpace && (char.IsNumber(wchar) || char.IsWhiteSpace(wchar)))
                return true;

            switch (culture)
            {
                case LanguageType.BasicLatin:
                    if (wchar is >= 'a' and <= 'z' or >= 'A' and <= 'Z') // LATIN SMALL LETTER A - LATIN SMALL LETTER Z
                        return true;

                    return false;
                case LanguageType.ExtendenLatin:
                    if (wchar >= 0x00C0 && wchar <= 0x00D6) // LATIN CAPITAL LETTER A WITH GRAVE - LATIN CAPITAL LETTER O WITH DIAERESIS
                        return true;

                    if (wchar >= 0x00D8 && wchar <= 0x00DE) // LATIN CAPITAL LETTER O WITH STROKE - LATIN CAPITAL LETTER THORN
                        return true;

                    if (wchar == 0x00DF) // LATIN SMALL LETTER SHARP S
                        return true;

                    if (wchar >= 0x00E0 && wchar <= 0x00F6) // LATIN SMALL LETTER A WITH GRAVE - LATIN SMALL LETTER O WITH DIAERESIS
                        return true;

                    if (wchar >= 0x00F8 && wchar <= 0x00FE) // LATIN SMALL LETTER O WITH STROKE - LATIN SMALL LETTER THORN
                        return true;

                    if (wchar >= 0x0100 && wchar <= 0x012F) // LATIN CAPITAL LETTER A WITH MACRON - LATIN SMALL LETTER I WITH OGONEK
                        return true;

                    if (wchar == 0x1E9E) // LATIN CAPITAL LETTER SHARP S
                        return true;

                    return false;
                case LanguageType.Cyrillic:
                    if (wchar >= 0x0410 && wchar <= 0x044F) // CYRILLIC CAPITAL LETTER A - CYRILLIC SMALL LETTER YA
                        return true;

                    if (wchar == 0x0401 || wchar == 0x0451) // CYRILLIC CAPITAL LETTER IO, CYRILLIC SMALL LETTER IO
                        return true;

                    return false;
                case LanguageType.EastAsia:
                    if (wchar >= 0x1100 && wchar <= 0x11F9) // Hangul Jamo
                        return true;

                    if (wchar >= 0x3041 && wchar <= 0x30FF) // Hiragana + Katakana
                        return true;

                    if (wchar >= 0x3131 && wchar <= 0x318E) // Hangul Compatibility Jamo
                        return true;

                    if (wchar >= 0x31F0 && wchar <= 0x31FF) // Katakana Phonetic Ext.
                        return true;

                    if (wchar >= 0x3400 && wchar <= 0x4DB5) // CJK Ideographs Ext. A
                        return true;

                    if (wchar >= 0x4E00 && wchar <= 0x9FC3) // Unified CJK Ideographs
                        return true;

                    if (wchar >= 0xAC00 && wchar <= 0xD7A3) // Hangul Syllables
                        return true;

                    if (wchar >= 0xFF01 && wchar <= 0xFFEE) // Halfwidth forms
                        return true;

                    return false;
            }
        }

        return false;
    }

    private static bool IsValidString(string str, uint strictMask, bool numericOrSpace, bool create = false)
    {
        if (strictMask == 0) // any language, ignore realm
        {
            if (IsCultureString(LanguageType.BasicLatin, str, numericOrSpace))
                return true;

            if (IsCultureString(LanguageType.ExtendenLatin, str, numericOrSpace))
                return true;

            if (IsCultureString(LanguageType.Cyrillic, str, numericOrSpace))
                return true;

            if (IsCultureString(LanguageType.EastAsia, str, numericOrSpace))
                return true;

            return false;
        }

        if (Convert.ToBoolean(strictMask & 0x2)) // realm zone specific
        {
            var lt = GetRealmLanguageType(create);

            if (lt.HasAnyFlag(LanguageType.ExtendenLatin))
            {
                if (IsCultureString(LanguageType.BasicLatin, str, numericOrSpace))
                    return true;

                if (IsCultureString(LanguageType.ExtendenLatin, str, numericOrSpace))
                    return true;
            }

            if (lt.HasAnyFlag(LanguageType.Cyrillic))
                if (IsCultureString(LanguageType.Cyrillic, str, numericOrSpace))
                    return true;

            if (lt.HasAnyFlag(LanguageType.EastAsia))
                if (IsCultureString(LanguageType.EastAsia, str, numericOrSpace))
                    return true;
        }

        if (Convert.ToBoolean(strictMask & 0x1)) // basic Latin
            if (IsCultureString(LanguageType.BasicLatin, str, numericOrSpace))
                return true;

        return false;
    }
    private void AddSpawnDataToGrid(SpawnData data)
    {
        var cellId = GridDefines.ComputeCellCoord(data.SpawnPoint.X, data.SpawnPoint.Y).GetId();
        var isPersonalPhase = PhasingHandler.IsPersonalPhase(data.PhaseId);

        if (!isPersonalPhase)
            foreach (var difficulty in data.SpawnDifficulties)
            {
                var key = (data.MapId, difficulty);

                if (!_mapObjectGuidsStore.ContainsKey(key))
                    _mapObjectGuidsStore[key] = new Dictionary<uint, CellObjectGuids>();

                if (!_mapObjectGuidsStore[key].ContainsKey(cellId))
                    _mapObjectGuidsStore[key][cellId] = new CellObjectGuids();

                _mapObjectGuidsStore[key][cellId].AddSpawn(data);
            }
        else
            foreach (var difficulty in data.SpawnDifficulties)
            {
                var key = (data.MapId, difficulty, data.PhaseId);

                if (!_mapPersonalObjectGuidsStore.ContainsKey(key))
                    _mapPersonalObjectGuidsStore[key] = new Dictionary<uint, CellObjectGuids>();

                if (!_mapPersonalObjectGuidsStore[key].ContainsKey(cellId))
                    _mapPersonalObjectGuidsStore[key][cellId] = new CellObjectGuids();

                _mapPersonalObjectGuidsStore[key][cellId].AddSpawn(data);
            }
    }

    private PlayerLevelInfo BuildPlayerLevelInfo(Race race, PlayerClass _class, uint level)
    {
        // base data (last known level)
        var info = PlayerInfos[race][_class].LevelInfo[_configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel) - 1];

        for (var lvl = _configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel) - 1; lvl < level; ++lvl)
            switch (_class)
            {
                case PlayerClass.Warrior:
                    info.Stats[0] += (lvl > 23 ? 2 : (lvl > 1 ? 1 : 0));
                    info.Stats[1] += (lvl > 23 ? 2 : (lvl > 1 ? 1 : 0));
                    info.Stats[2] += (lvl > 36 ? 1 : (lvl > 6 && (lvl % 2) != 0 ? 1 : 0));
                    info.Stats[3] += (lvl > 9 && (lvl % 2) == 0 ? 1 : 0);

                    break;
                case PlayerClass.Paladin:
                    info.Stats[0] += (lvl > 3 ? 1 : 0);
                    info.Stats[1] += (lvl > 33 ? 2 : (lvl > 1 ? 1 : 0));
                    info.Stats[2] += (lvl > 38 ? 1 : (lvl > 7 && (lvl % 2) == 0 ? 1 : 0));
                    info.Stats[3] += (lvl > 6 && (lvl % 2) != 0 ? 1 : 0);

                    break;
                case PlayerClass.Hunter:
                    info.Stats[0] += (lvl > 4 ? 1 : 0);
                    info.Stats[1] += (lvl > 4 ? 1 : 0);
                    info.Stats[2] += (lvl > 33 ? 2 : (lvl > 1 ? 1 : 0));
                    info.Stats[3] += (lvl > 8 && (lvl % 2) != 0 ? 1 : 0);

                    break;
                case PlayerClass.Rogue:
                    info.Stats[0] += (lvl > 5 ? 1 : 0);
                    info.Stats[1] += (lvl > 4 ? 1 : 0);
                    info.Stats[2] += (lvl > 16 ? 2 : (lvl > 1 ? 1 : 0));
                    info.Stats[3] += (lvl > 8 && (lvl % 2) == 0 ? 1 : 0);

                    break;
                case PlayerClass.Priest:
                    info.Stats[0] += (lvl > 9 && (lvl % 2) == 0 ? 1 : 0);
                    info.Stats[1] += (lvl > 5 ? 1 : 0);
                    info.Stats[2] += (lvl > 38 ? 1 : (lvl > 8 && (lvl % 2) != 0 ? 1 : 0));
                    info.Stats[3] += (lvl > 22 ? 2 : (lvl > 1 ? 1 : 0));

                    break;
                case PlayerClass.Shaman:
                    info.Stats[0] += (lvl > 34 ? 1 : (lvl > 6 && (lvl % 2) != 0 ? 1 : 0));
                    info.Stats[1] += (lvl > 4 ? 1 : 0);
                    info.Stats[2] += (lvl > 7 && (lvl % 2) == 0 ? 1 : 0);
                    info.Stats[3] += (lvl > 5 ? 1 : 0);

                    break;
                case PlayerClass.Mage:
                    info.Stats[0] += (lvl > 9 && (lvl % 2) == 0 ? 1 : 0);
                    info.Stats[1] += (lvl > 5 ? 1 : 0);
                    info.Stats[2] += (lvl > 9 && (lvl % 2) == 0 ? 1 : 0);
                    info.Stats[3] += (lvl > 24 ? 2 : (lvl > 1 ? 1 : 0));

                    break;
                case PlayerClass.Warlock:
                    info.Stats[0] += (lvl > 9 && (lvl % 2) == 0 ? 1 : 0);
                    info.Stats[1] += (lvl > 38 ? 2 : (lvl > 3 ? 1 : 0));
                    info.Stats[2] += (lvl > 9 && (lvl % 2) == 0 ? 1 : 0);
                    info.Stats[3] += (lvl > 33 ? 2 : (lvl > 2 ? 1 : 0));

                    break;
                case PlayerClass.Druid:
                    info.Stats[0] += (lvl > 38 ? 2 : (lvl > 6 && (lvl % 2) != 0 ? 1 : 0));
                    info.Stats[1] += (lvl > 32 ? 2 : (lvl > 4 ? 1 : 0));
                    info.Stats[2] += (lvl > 38 ? 2 : (lvl > 8 && (lvl % 2) != 0 ? 1 : 0));
                    info.Stats[3] += (lvl > 38 ? 3 : (lvl > 4 ? 1 : 0));

                    break;
            }

        return info;
    }

    private void CheckAndFixGOChairHeightId(GameObjectTemplate goInfo, ref uint dataN, uint N)
    {
        if (dataN <= (UnitStandStateType.SitHighChair - UnitStandStateType.SitLowChair))
            return;

        Log.Logger.Error("Gameobject (Entry: {0} GoType: {1}) have data{2}={3} but correct chair height in range 0..{4}.", goInfo.entry, goInfo.type, N, dataN, UnitStandStateType.SitHighChair - UnitStandStateType.SitLowChair);

        // prevent client and server unexpected work
        dataN = 0;
    }

    private void CheckCreatureMovement(string table, ulong id, CreatureMovementData creatureMovement)
    {
        if (creatureMovement.Ground >= CreatureGroundMovementType.Max)
        {
            Log.Logger.Error($"`{table}`.`Ground` wrong value ({creatureMovement.Ground}) for Id {id}, setting to Run.");
            creatureMovement.Ground = CreatureGroundMovementType.Run;
        }

        if (creatureMovement.Flight >= CreatureFlightMovementType.Max)
        {
            Log.Logger.Error($"`{table}`.`Flight` wrong value ({creatureMovement.Flight}) for Id {id}, setting to None.");
            creatureMovement.Flight = CreatureFlightMovementType.None;
        }

        if (creatureMovement.Chase >= CreatureChaseMovementType.Max)
        {
            Log.Logger.Error($"`{table}`.`Chase` wrong value ({creatureMovement.Chase}) for Id {id}, setting to Run.");
            creatureMovement.Chase = CreatureChaseMovementType.Run;
        }

        if (creatureMovement.Random >= CreatureRandomMovementType.Max)
        {
            Log.Logger.Error($"`{table}`.`Random` wrong value ({creatureMovement.Random}) for Id {id}, setting to Walk.");
            creatureMovement.Random = CreatureRandomMovementType.Walk;
        }
    }

    private void CheckGOConsumable(GameObjectTemplate goInfo, uint dataN, uint N)
    {
        // 0/1 correct values
        if (dataN <= 1)
            return;

        Log.Logger.Error("Gameobject (Entry: {0} GoType: {1}) have data{2}={3} but expected boolean (0/1) consumable field value.",
                         goInfo.entry,
                         goInfo.type,
                         N,
                         dataN);
    }

    private void CheckGOLinkedTrapId(GameObjectTemplate goInfo, uint dataN, uint N)
    {
        var trapInfo = GetGameObjectTemplate(dataN);

        if (trapInfo != null)
            if (trapInfo.type != GameObjectTypes.Trap)
                Log.Logger.Error("Gameobject (Entry: {0} GoType: {1}) have data{2}={3} but GO (Entry {4}) have not GAMEOBJECT_TYPE_TRAP type.", goInfo.entry, goInfo.type, N, dataN, dataN);
    }

    private void CheckGOLockId(GameObjectTemplate goInfo, uint dataN, uint N)
    {
        if (_cliDB.LockStorage.ContainsKey(dataN))
            return;

        Log.Logger.Debug("Gameobject (Entry: {0} GoType: {1}) have data{2}={3} but lock (Id: {4}) not found.", goInfo.entry, goInfo.type, N, goInfo.Door.open, goInfo.Door.open);
    }

    private void CheckGONoDamageImmuneId(GameObjectTemplate goTemplate, uint dataN, uint N)
    {
        // 0/1 correct values
        if (dataN <= 1)
            return;

        Log.Logger.Error("Gameobject (Entry: {0} GoType: {1}) have data{2}={3} but expected boolean (0/1) noDamageImmune field value.", goTemplate.entry, goTemplate.type, N, dataN);
    }

    private void CheckGOSpellId(GameObjectTemplate goInfo, uint dataN, uint N)
    {
        if (Global.SpellMgr.HasSpellInfo(dataN, Difficulty.None))
            return;

        Log.Logger.Error("Gameobject (Entry: {0} GoType: {1}) have data{2}={3} but Spell (Entry {4}) not exist.", goInfo.entry, goInfo.type, N, dataN, dataN);
    }

    private CellObjectGuids CreateCellObjectGuids(uint mapid, Difficulty difficulty, uint cellid)
    {
        var key = (mapid, difficulty);

        if (!_mapObjectGuidsStore.TryGetValue(key, out var internalDict))
        {
            internalDict = new Dictionary<uint, CellObjectGuids>();
            _mapObjectGuidsStore.Add(key, internalDict);
        }

        if (!internalDict.TryGetValue(cellid, out var cell))
        {
            cell = new CellObjectGuids();
            _mapObjectGuidsStore[key].Add(cellid, cell);
        }

        return cell;
    }

    private uint FillMaxDurability(ItemClass itemClass, uint itemSubClass, InventoryType inventoryType, ItemQuality quality, uint itemLevel)
    {
        if (itemClass != ItemClass.Armor && itemClass != ItemClass.Weapon)
            return 0;

        var levelPenalty = 1.0f;

        if (itemLevel <= 28)
            levelPenalty = 0.966f - (28u - itemLevel) / 54.0f;

        if (itemClass == ItemClass.Armor)
        {
            if (inventoryType > InventoryType.Robe)
                return 0;

            return 5 * (uint)(Math.Round(25.0f * qualityMultipliers[(int)quality] * armorMultipliers[(int)inventoryType] * levelPenalty));
        }

        return 5 * (uint)(Math.Round(18.0f * qualityMultipliers[(int)quality] * weaponMultipliers[itemSubClass] * levelPenalty));
    }

    private MultiMap<uint, uint> GetGameObjectQuestItemMap()
    {
        return _gameObjectQuestItemStorage;
    }

    private ObjectGuidGenerator GetGuidSequenceGenerator(HighGuid high)
    {
        if (!_guidGenerators.ContainsKey(high))
            _guidGenerators[high] = new ObjectGuidGenerator(high);

        return _guidGenerators[high];
    }

    private QuestRelationResult GetQuestRelationsFrom(MultiMap<uint, uint> map, uint key, bool onlyActive)
    {
        return new QuestRelationResult(map.LookupByKey(key), onlyActive);
    }

    private bool IsScriptDatabaseBound(uint id)
    {
        var entry = _scriptNamesStorage.Find(id);

        if (entry != null)
            return entry.IsScriptDatabaseBound;

        return false;
    }

    private void LoadAreaPhases()
    {
        var oldMSTime = Time.MSTime;

        //                                         0       1
        var result = _worldDatabase.Query("SELECT AreaId, PhaseId FROM `phase_area`");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 phase areas. DB table `phase_area` is empty.");

            return;
        }

        PhaseInfoStruct getOrCreatePhaseIfMissing(uint phaseId)
        {
            var phaseInfo = _phaseInfoById[phaseId];
            phaseInfo.Id = phaseId;

            return phaseInfo;
        }

        uint count = 0;

        do
        {
            var area = result.Read<uint>(0);
            var phaseId = result.Read<uint>(1);

            if (!_cliDB.AreaTableStorage.ContainsKey(area))
            {
                Log.Logger.Error($"Area {area} defined in `phase_area` does not exist, skipped.");

                continue;
            }

            if (!_cliDB.PhaseStorage.ContainsKey(phaseId))
            {
                Log.Logger.Error($"Phase {phaseId} defined in `phase_area` does not exist, skipped.");

                continue;
            }

            var phase = getOrCreatePhaseIfMissing(phaseId);
            phase.Areas.Add(area);
            _phaseInfoByArea.Add(area, new PhaseAreaInfo(phase));

            ++count;
        } while (result.NextRow());

        foreach (var pair in _phaseInfoByArea.KeyValueList)
        {
            var parentAreaId = pair.Key;

            do
            {
                var area = _cliDB.AreaTableStorage.LookupByKey(parentAreaId);

                if (area == null)
                    break;

                parentAreaId = area.ParentAreaID;

                if (parentAreaId == 0)
                    break;

                var parentAreaPhases = _phaseInfoByArea.LookupByKey(parentAreaId);

                foreach (var parentAreaPhase in parentAreaPhases)
                    if (parentAreaPhase.PhaseInfo.Id == pair.Value.PhaseInfo.Id)
                        parentAreaPhase.SubAreaExclusions.Add(pair.Key);
            } while (true);
        }

        Log.Logger.Information($"Loaded {count} phase areas in {Time.GetMSTimeDiffToNow(oldMSTime)} ms.");
    }

    private void LoadCreatureSummonedData()
    {
        var oldMSTime = Time.MSTime;

        //                                         0           1                            2                     3
        var result = _worldDatabase.Query("SELECT CreatureID, CreatureIDVisibleToSummoner, GroundMountDisplayID, FlyingMountDisplayID FROM creature_summoned_data");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 creature summoned data definitions. DB table `creature_summoned_data` is empty.");

            return;
        }

        do
        {
            var creatureId = result.Read<uint>(0);

            if (GetCreatureTemplate(creatureId) == null)
            {
                Log.Logger.Debug($"Table `creature_summoned_data` references non-existing creature {creatureId}, skipped");

                continue;
            }

            if (!_creatureSummonedDataStorage.ContainsKey(creatureId))
                _creatureSummonedDataStorage[creatureId] = new CreatureSummonedData();

            var summonedData = _creatureSummonedDataStorage[creatureId];

            if (!result.IsNull(1))
            {
                summonedData.CreatureIdVisibleToSummoner = result.Read<uint>(1);

                if (GetCreatureTemplate(summonedData.CreatureIdVisibleToSummoner.Value) == null)
                {
                    Log.Logger.Debug($"Table `creature_summoned_data` references non-existing creature {summonedData.CreatureIdVisibleToSummoner.Value} in CreatureIDVisibleToSummoner for creature {creatureId}, set to 0");
                    summonedData.CreatureIdVisibleToSummoner = null;
                }
            }

            if (!result.IsNull(2))
            {
                summonedData.GroundMountDisplayId = result.Read<uint>(2);

                if (!_cliDB.CreatureDisplayInfoStorage.ContainsKey(summonedData.GroundMountDisplayId.Value))
                {
                    Log.Logger.Debug($"Table `creature_summoned_data` references non-existing display id {summonedData.GroundMountDisplayId.Value} in GroundMountDisplayID for creature {creatureId}, set to 0");
                    summonedData.CreatureIdVisibleToSummoner = null;
                }
            }

            if (!result.IsNull(3))
            {
                summonedData.FlyingMountDisplayId = result.Read<uint>(3);

                if (!_cliDB.CreatureDisplayInfoStorage.ContainsKey(summonedData.FlyingMountDisplayId.Value))
                {
                    Log.Logger.Debug($"Table `creature_summoned_data` references non-existing display id {summonedData.FlyingMountDisplayId.Value} in FlyingMountDisplayID for creature {creatureId}, set to 0");
                    summonedData.GroundMountDisplayId = null;
                }
            }
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {_creatureSummonedDataStorage.Count} creature summoned data definitions in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    private void LoadCreatureTemplateModels()
    {
        var oldMSTime = Time.MSTime;
        //                                         0           1                  2             3
        var result = _worldDatabase.Query("SELECT CreatureID, CreatureDisplayID, DisplayScale, Probability FROM creature_template_model ORDER BY Idx ASC");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 creature template model definitions. DB table `creature_template_model` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var creatureId = result.Read<uint>(0);
            var creatureDisplayId = result.Read<uint>(1);
            var displayScale = result.Read<float>(2);
            var probability = result.Read<float>(3);

            var cInfo = GetCreatureTemplate(creatureId);

            if (cInfo == null)
            {
                if (_configuration.GetDefaultValue("load.autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM creature_template_model WHERE CreatureID = {creatureId}");
                else
                    Log.Logger.Debug($"Creature template (Entry: {creatureId}) does not exist but has a record in `creature_template_model`");

                continue;
            }

            var displayEntry = _cliDB.CreatureDisplayInfoStorage.LookupByKey(creatureDisplayId);

            if (displayEntry == null)
            {
                Log.Logger.Debug($"Creature (Entry: {creatureId}) lists non-existing CreatureDisplayID id ({creatureDisplayId}), this can crash the client.");

                continue;
            }

            var modelInfo = GetCreatureModelInfo(creatureDisplayId);

            if (modelInfo == null)
                Log.Logger.Debug($"No model data exist for `CreatureDisplayID` = {creatureDisplayId} listed by creature (Entry: {creatureId}).");

            if (displayScale <= 0.0f)
                displayScale = 1.0f;

            cInfo.Models.Add(new CreatureModel(creatureDisplayId, displayScale, probability));
            ++count;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {count} creature template models in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    private void LoadCreatureTemplateResistances()
    {
        var oldMSTime = Time.MSTime;

        //                                         0           1       2
        var result = _worldDatabase.Query("SELECT CreatureID, School, Resistance FROM creature_template_resistance");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 creature template resistance definitions. DB table `creature_template_resistance` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var creatureID = result.Read<uint>(0);
            var school = (SpellSchools)result.Read<byte>(1);

            if (school is SpellSchools.Normal or >= SpellSchools.Max)
            {
                Log.Logger.Error($"creature_template_resistance has resistance definitions for creature {creatureID} but this school {school} doesn't exist");

                continue;
            }

            if (!_creatureTemplateStorage.TryGetValue(creatureID, out var creatureTemplate))
            {
                Log.Logger.Error($"creature_template_resistance has resistance definitions for creature {creatureID} but this creature doesn't exist");

                continue;
            }

            creatureTemplate.Resistance[(int)school] = result.Read<short>(2);

            ++count;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {count} creature template resistances in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    private void LoadCreatureTemplateSpells()
    {
        var oldMSTime = Time.MSTime;

        //                                         0           1       2
        var result = _worldDatabase.Query("SELECT CreatureID, `Index`, Spell FROM creature_template_spell");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 creature template spell definitions. DB table `creature_template_spell` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var creatureID = result.Read<uint>(0);
            var index = result.Read<byte>(1);

            if (index >= SharedConst.MaxCreatureSpells)
            {
                Log.Logger.Error($"creature_template_spell has spell definitions for creature {creatureID} with a incorrect index {index}");

                continue;
            }

            if (!_creatureTemplateStorage.TryGetValue(creatureID, out var creatureTemplate))
            {
                Log.Logger.Error($"creature_template_spell has spell definitions for creature {creatureID} but this creature doesn't exist");

                continue;
            }

            creatureTemplate.Spells[index] = result.Read<uint>(2);

            ++count;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {count} creature template spells in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    private void LoadQuestRelationsHelper(MultiMap<uint, uint> map, MultiMap<uint, uint> reverseMap, string table)
    {
        var oldMSTime = Time.MSTime;

        map.Clear(); // need for reload case

        uint count = 0;

        var result = _worldDatabase.Query($"SELECT id, quest FROM {table}");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 quest relations from `{0}`, table is empty.", table);

            return;
        }

        do
        {
            var id = result.Read<uint>(0);
            var quest = result.Read<uint>(1);

            if (!_questTemplates.ContainsKey(quest))
            {
                Log.Logger.Error("Table `{0}`: QuestId {1} listed for entry {2} does not exist.", table, quest, id);

                continue;
            }

            map.Add(id, quest);

            reverseMap?.Add(quest, id);

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} quest relations from {1} in {2} ms", count, table, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    private uint LoadReferenceVendor(int vendor, int item, List<uint> skip_vendors)
    {
        // find all items from the reference vendor
        var stmt = _worldDatabase.GetPreparedStatement(WorldStatements.SEL_NPC_VENDOR_REF);
        stmt.AddValue(0, item);
        var result = _worldDatabase.Query(stmt);

        if (result.IsEmpty())
            return 0;

        uint count = 0;

        do
        {
            var item_id = result.Read<int>(0);

            // if item is a negative, its a reference
            if (item_id < 0)
            {
                count += LoadReferenceVendor(vendor, -item_id, skip_vendors);
            }
            else
            {
                VendorItem vItem = new()
                {
                    Item = (uint)item_id,
                    Maxcount = result.Read<uint>(1),
                    Incrtime = result.Read<uint>(2),
                    ExtendedCost = result.Read<uint>(3),
                    Type = (ItemVendorType)result.Read<byte>(4),
                    PlayerConditionId = result.Read<uint>(6),
                    IgnoreFiltering = result.Read<bool>(7)
                };

                var bonusListIDsTok = new StringArray(result.Read<string>(5), ' ');

                if (!bonusListIDsTok.IsEmpty())
                    foreach (string token in bonusListIDsTok)
                        if (uint.TryParse(token, out var id))
                            vItem.BonusListIDs.Add(id);

                if (!IsVendorItemValid((uint)vendor, vItem, null, skip_vendors))
                    continue;

                var vList = _cacheVendorItemStorage.LookupByKey((uint)vendor);

                if (vList == null)
                    continue;

                vList.AddItem(vItem);
                ++count;
            }
        } while (result.NextRow());

        return count;
    }

    private void LoadScripts(ScriptsType type)
    {
        var oldMSTime = Time.MSTime;

        var scripts = GetScriptsMapByType(type);

        if (scripts == null)
            return;

        var tableName = GetScriptsTableNameByType(type);

        if (string.IsNullOrEmpty(tableName))
            return;

        if (Global.MapMgr.IsScriptScheduled()) // function cannot be called when scripts are in use.
            return;

        Log.Logger.Information("Loading {0}...", tableName);

        scripts.Clear(); // need for reload support

        var isSpellScriptTable = (type == ScriptsType.Spell);
        //                                         0    1       2         3         4          5    6  7  8  9
        var result = _worldDatabase.Query("SELECT id, delay, command, datalong, datalong2, dataint, x, y, z, o{0} FROM {1}", isSpellScriptTable ? ", effIndex" : "", tableName);

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 script definitions. DB table `{0}` is empty!", tableName);

            return;
        }

        uint count = 0;

        do
        {
            ScriptInfo tmp = new()
            {
                type = type,
                id = result.Read<uint>(0)
            };

            if (isSpellScriptTable)
                tmp.id |= result.Read<uint>(10) << 24;

            tmp.delay = result.Read<uint>(1);
            tmp.command = (ScriptCommands)result.Read<uint>(2);

            unsafe
            {
                tmp.Raw.nData[0] = result.Read<uint>(3);
                tmp.Raw.nData[1] = result.Read<uint>(4);
                tmp.Raw.nData[2] = (uint)result.Read<int>(5);
                tmp.Raw.fData[0] = result.Read<float>(6);
                tmp.Raw.fData[1] = result.Read<float>(7);
                tmp.Raw.fData[2] = result.Read<float>(8);
                tmp.Raw.fData[3] = result.Read<float>(9);
            }

            // generic command args check
            switch (tmp.command)
            {
                case ScriptCommands.Talk:
                {
                    if (tmp.Talk.ChatType > ChatMsg.RaidBossWhisper)
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                        else
                            Log.Logger.Error("Table `{0}` has invalid talk type (datalong = {1}) in SCRIPT_COMMAND_TALK for script id {2}",
                                             tableName,
                                             tmp.Talk.ChatType,
                                             tmp.id);

                        continue;
                    }

                    if (!_cliDB.BroadcastTextStorage.ContainsKey((uint)tmp.Talk.TextID))
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                        else
                            Log.Logger.Error("Table `{0}` has invalid talk text id (dataint = {1}) in SCRIPT_COMMAND_TALK for script id {2}",
                                             tableName,
                                             tmp.Talk.TextID,
                                             tmp.id);

                        continue;
                    }

                    break;
                }

                case ScriptCommands.Emote:
                {
                    if (!_cliDB.EmotesStorage.ContainsKey(tmp.Emote.EmoteID))
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                        else
                            Log.Logger.Error("Table `{0}` has invalid emote id (datalong = {1}) in SCRIPT_COMMAND_EMOTE for script id {2}",
                                             tableName,
                                             tmp.Emote.EmoteID,
                                             tmp.id);

                        continue;
                    }

                    break;
                }

                case ScriptCommands.TeleportTo:
                {
                    if (!_cliDB.MapStorage.ContainsKey(tmp.TeleportTo.MapID))
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                        else
                            Log.Logger.Error("Table `{0}` has invalid map (Id: {1}) in SCRIPT_COMMAND_TELEPORT_TO for script id {2}",
                                             tableName,
                                             tmp.TeleportTo.MapID,
                                             tmp.id);

                        continue;
                    }

                    if (!GridDefines.IsValidMapCoord(tmp.TeleportTo.DestX, tmp.TeleportTo.DestY, tmp.TeleportTo.DestZ, tmp.TeleportTo.Orientation))
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                        else
                            Log.Logger.Error("Table `{0}` has invalid coordinates (X: {1} Y: {2} Z: {3} O: {4}) in SCRIPT_COMMAND_TELEPORT_TO for script id {5}",
                                             tableName,
                                             tmp.TeleportTo.DestX,
                                             tmp.TeleportTo.DestY,
                                             tmp.TeleportTo.DestZ,
                                             tmp.TeleportTo.Orientation,
                                             tmp.id);

                        continue;
                    }

                    break;
                }

                case ScriptCommands.QuestExplored:
                {
                    var quest = GetQuestTemplate(tmp.QuestExplored.QuestID);

                    if (quest == null)
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                        else
                            Log.Logger.Error("Table `{0}` has invalid quest (ID: {1}) in SCRIPT_COMMAND_QUEST_EXPLORED in `datalong` for script id {2}",
                                             tableName,
                                             tmp.QuestExplored.QuestID,
                                             tmp.id);

                        continue;
                    }

                    if (!quest.HasSpecialFlag(QuestSpecialFlags.ExplorationOrEvent))
                    {
                        Log.Logger.Error("Table `{0}` has quest (ID: {1}) in SCRIPT_COMMAND_QUEST_EXPLORED in `datalong` for script id {2}, but quest not have Id QUEST_SPECIAL_FLAGS_EXPLORATION_OR_EVENT in quest flags. Script command or quest flags wrong. QuestId modified to require objective.",
                                         tableName,
                                         tmp.QuestExplored.QuestID,
                                         tmp.id);

                        // this will prevent quest completing without objective
                        quest.SetSpecialFlag(QuestSpecialFlags.ExplorationOrEvent);

                        // continue; - quest objective requirement set and command can be allowed
                    }

                    if (tmp.QuestExplored.Distance > SharedConst.DefaultVisibilityDistance)
                    {
                        Log.Logger.Error("Table `{0}` has too large distance ({1}) for exploring objective complete in `datalong2` in SCRIPT_COMMAND_QUEST_EXPLORED in `datalong` for script id {2}",
                                         tableName,
                                         tmp.QuestExplored.Distance,
                                         tmp.id);

                        continue;
                    }

                    if (tmp.QuestExplored.Distance != 0 && tmp.QuestExplored.Distance > SharedConst.DefaultVisibilityDistance)
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                        else
                            Log.Logger.Error("Table `{0}` has too large distance ({1}) for exploring objective complete in `datalong2` in SCRIPT_COMMAND_QUEST_EXPLORED in `datalong` for script id {2}, max distance is {3} or 0 for disable distance check",
                                             tableName,
                                             tmp.QuestExplored.Distance,
                                             tmp.id,
                                             SharedConst.DefaultVisibilityDistance);

                        continue;
                    }

                    if (tmp.QuestExplored.Distance != 0 && tmp.QuestExplored.Distance < SharedConst.InteractionDistance)
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                        else
                            Log.Logger.Error("Table `{0}` has too small distance ({1}) for exploring objective complete in `datalong2` in SCRIPT_COMMAND_QUEST_EXPLORED in `datalong` for script id {2}, min distance is {3} or 0 for disable distance check",
                                             tableName,
                                             tmp.QuestExplored.Distance,
                                             tmp.id,
                                             SharedConst.InteractionDistance);

                        continue;
                    }

                    break;
                }

                case ScriptCommands.KillCredit:
                {
                    if (GetCreatureTemplate(tmp.KillCredit.CreatureEntry) == null)
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                        else
                            Log.Logger.Error("Table `{0}` has invalid creature (Entry: {1}) in SCRIPT_COMMAND_KILL_CREDIT for script id {2}",
                                             tableName,
                                             tmp.KillCredit.CreatureEntry,
                                             tmp.id);

                        continue;
                    }

                    break;
                }

                case ScriptCommands.RespawnGameobject:
                {
                    var data = GetGameObjectData(tmp.RespawnGameObject.GOGuid);

                    if (data == null)
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                        else
                            Log.Logger.Error("Table `{0}` has invalid gameobject (GUID: {1}) in SCRIPT_COMMAND_RESPAWN_GAMEOBJECT for script id {2}",
                                             tableName,
                                             tmp.RespawnGameObject.GOGuid,
                                             tmp.id);

                        continue;
                    }

                    var info = GetGameObjectTemplate(data.Id);

                    if (info == null)
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                        else
                            Log.Logger.Error("Table `{0}` has gameobject with invalid entry (GUID: {1} Entry: {2}) in SCRIPT_COMMAND_RESPAWN_GAMEOBJECT for script id {3}",
                                             tableName,
                                             tmp.RespawnGameObject.GOGuid,
                                             data.Id,
                                             tmp.id);

                        continue;
                    }

                    if (info.type is GameObjectTypes.FishingNode or GameObjectTypes.FishingHole or GameObjectTypes.Door or GameObjectTypes.Button or GameObjectTypes.Trap)
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                        else
                            Log.Logger.Error("Table `{0}` have gameobject type ({1}) unsupported by command SCRIPT_COMMAND_RESPAWN_GAMEOBJECT for script id {2}",
                                             tableName,
                                             info.entry,
                                             tmp.id);

                        continue;
                    }

                    break;
                }

                case ScriptCommands.TempSummonCreature:
                {
                    if (!GridDefines.IsValidMapCoord(tmp.TempSummonCreature.PosX, tmp.TempSummonCreature.PosY, tmp.TempSummonCreature.PosZ, tmp.TempSummonCreature.Orientation))
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                        else
                            Log.Logger.Error("Table `{0}` has invalid coordinates (X: {1} Y: {2} Z: {3} O: {4}) in SCRIPT_COMMAND_TEMP_SUMMON_CREATURE for script id {5}",
                                             tableName,
                                             tmp.TempSummonCreature.PosX,
                                             tmp.TempSummonCreature.PosY,
                                             tmp.TempSummonCreature.PosZ,
                                             tmp.TempSummonCreature.Orientation,
                                             tmp.id);

                        continue;
                    }

                    if (GetCreatureTemplate(tmp.TempSummonCreature.CreatureEntry) == null)
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                        else
                            Log.Logger.Error("Table `{0}` has invalid creature (Entry: {1}) in SCRIPT_COMMAND_TEMP_SUMMON_CREATURE for script id {2}",
                                             tableName,
                                             tmp.TempSummonCreature.CreatureEntry,
                                             tmp.id);

                        continue;
                    }

                    break;
                }

                case ScriptCommands.OpenDoor:
                case ScriptCommands.CloseDoor:
                {
                    var data = GetGameObjectData(tmp.ToggleDoor.GOGuid);

                    if (data == null)
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                        else
                            Log.Logger.Error("Table `{0}` has invalid gameobject (GUID: {1}) in {2} for script id {3}",
                                             tableName,
                                             tmp.ToggleDoor.GOGuid,
                                             tmp.command,
                                             tmp.id);

                        continue;
                    }

                    var info = GetGameObjectTemplate(data.Id);

                    if (info == null)
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                        else
                            Log.Logger.Error("Table `{0}` has gameobject with invalid entry (GUID: {1} Entry: {2}) in {3} for script id {4}",
                                             tableName,
                                             tmp.ToggleDoor.GOGuid,
                                             data.Id,
                                             tmp.command,
                                             tmp.id);

                        continue;
                    }

                    if (info.type != GameObjectTypes.Door)
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                        else
                            Log.Logger.Error("Table `{0}` has gameobject type ({1}) non supported by command {2} for script id {3}",
                                             tableName,
                                             info.entry,
                                             tmp.command,
                                             tmp.id);

                        continue;
                    }

                    break;
                }

                case ScriptCommands.RemoveAura:
                {
                    if (!Global.SpellMgr.HasSpellInfo(tmp.RemoveAura.SpellID, Difficulty.None))
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                        else
                            Log.Logger.Error("Table `{0}` using non-existent spell (id: {1}) in SCRIPT_COMMAND_REMOVE_AURA for script id {2}",
                                             tableName,
                                             tmp.RemoveAura.SpellID,
                                             tmp.id);

                        continue;
                    }

                    if (Convert.ToBoolean((int)tmp.RemoveAura.Flags & ~0x1)) // 1 bits (0, 1)
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                        else
                            Log.Logger.Error("Table `{0}` using unknown flags in datalong2 ({1}) in SCRIPT_COMMAND_REMOVE_AURA for script id {2}",
                                             tableName,
                                             tmp.RemoveAura.Flags,
                                             tmp.id);

                        continue;
                    }

                    break;
                }

                case ScriptCommands.CastSpell:
                {
                    if (!Global.SpellMgr.HasSpellInfo(tmp.CastSpell.SpellID, Difficulty.None))
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                        else
                            Log.Logger.Error("Table `{0}` using non-existent spell (id: {1}) in SCRIPT_COMMAND_CAST_SPELL for script id {2}",
                                             tableName,
                                             tmp.CastSpell.SpellID,
                                             tmp.id);

                        continue;
                    }

                    if ((int)tmp.CastSpell.Flags > 4) // targeting type
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                        else
                            Log.Logger.Error("Table `{0}` using unknown target in datalong2 ({1}) in SCRIPT_COMMAND_CAST_SPELL for script id {2}",
                                             tableName,
                                             tmp.CastSpell.Flags,
                                             tmp.id);

                        continue;
                    }

                    if ((int)tmp.CastSpell.Flags != 4 && Convert.ToBoolean(tmp.CastSpell.CreatureEntry & ~0x1)) // 1 bit (0, 1)
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                        else
                            Log.Logger.Error("Table `{0}` using unknown flags in dataint ({1}) in SCRIPT_COMMAND_CAST_SPELL for script id {2}",
                                             tableName,
                                             tmp.CastSpell.CreatureEntry,
                                             tmp.id);

                        continue;
                    }
                    else if ((int)tmp.CastSpell.Flags == 4 && GetCreatureTemplate((uint)tmp.CastSpell.CreatureEntry) == null)
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                        else
                            Log.Logger.Error("Table `{0}` using invalid creature entry in dataint ({1}) in SCRIPT_COMMAND_CAST_SPELL for script id {2}",
                                             tableName,
                                             tmp.CastSpell.CreatureEntry,
                                             tmp.id);

                        continue;
                    }

                    break;
                }

                case ScriptCommands.CreateItem:
                {
                    if (GetItemTemplate(tmp.CreateItem.ItemEntry) == null)
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                        else
                            Log.Logger.Error("Table `{0}` has nonexistent item (entry: {1}) in SCRIPT_COMMAND_CREATE_ITEM for script id {2}",
                                             tableName,
                                             tmp.CreateItem.ItemEntry,
                                             tmp.id);

                        continue;
                    }

                    if (tmp.CreateItem.Amount == 0)
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                        else
                            Log.Logger.Error("Table `{0}` SCRIPT_COMMAND_CREATE_ITEM but amount is {1} for script id {2}",
                                             tableName,
                                             tmp.CreateItem.Amount,
                                             tmp.id);

                        continue;
                    }

                    break;
                }
                case ScriptCommands.PlayAnimkit:
                {
                    if (!_cliDB.AnimKitStorage.ContainsKey(tmp.PlayAnimKit.AnimKitID))
                    {
                        if (_configuration.GetDefaultValue("load.autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                        else
                            Log.Logger.Error("Table `{0}` has invalid AnimKid id (datalong = {1}) in SCRIPT_COMMAND_PLAY_ANIMKIT for script id {2}",
                                             tableName,
                                             tmp.PlayAnimKit.AnimKitID,
                                             tmp.id);

                        continue;
                    }

                    break;
                }
                case ScriptCommands.FieldSetDeprecated:
                case ScriptCommands.FlagSetDeprecated:
                case ScriptCommands.FlagRemoveDeprecated:
                {
                    if (_configuration.GetDefaultValue("load.autoclean", false))
                        _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                    else
                        Log.Logger.Error($"Table `{tableName}` uses deprecated direct updatefield modify command {tmp.command} for script id {tmp.id}");

                    continue;
                }
                default:
                    break;
            }

            if (!scripts.ContainsKey(tmp.id))
                scripts[tmp.id] = new MultiMap<uint, ScriptInfo>();

            scripts[tmp.id].Add(tmp.delay, tmp);

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} script definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }
    private void LoadTerrainSwapDefaults()
    {
        var oldMSTime = Time.MSTime;

        var result = _worldDatabase.Query("SELECT MapId, TerrainSwapMap FROM `terrain_swap_defaults`");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 terrain swap defaults. DB table `terrain_swap_defaults` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var mapId = result.Read<uint>(0);

            if (!_cliDB.MapStorage.ContainsKey(mapId))
            {
                Log.Logger.Error("Map {0} defined in `terrain_swap_defaults` does not exist, skipped.", mapId);

                continue;
            }

            var terrainSwap = result.Read<uint>(1);

            if (!_cliDB.MapStorage.ContainsKey(terrainSwap))
            {
                Log.Logger.Error("TerrainSwapMap {0} defined in `terrain_swap_defaults` does not exist, skipped.", terrainSwap);

                continue;
            }

            var terrainSwapInfo = _terrainSwapInfoById[terrainSwap];
            terrainSwapInfo.Id = terrainSwap;
            _terrainSwapInfoByMap.Add(mapId, terrainSwapInfo);

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} terrain swap defaults in {1} ms.", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    private void LoadTerrainWorldMaps()
    {
        var oldMSTime = Time.MSTime;

        //                                         0               1
        var result = _worldDatabase.Query("SELECT TerrainSwapMap, UiMapPhaseId  FROM `terrain_worldmap`");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 terrain world maps. DB table `terrain_worldmap` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var mapId = result.Read<uint>(0);
            var uiMapPhaseId = result.Read<uint>(1);

            if (!_cliDB.MapStorage.ContainsKey(mapId))
            {
                Log.Logger.Error("TerrainSwapMap {0} defined in `terrain_worldmap` does not exist, skipped.", mapId);

                continue;
            }

            if (!Global.DB2Mgr.IsUiMapPhase((int)uiMapPhaseId))
            {
                Log.Logger.Error($"Phase {uiMapPhaseId} defined in `terrain_worldmap` is not a valid terrain swap phase, skipped.");

                continue;
            }

            if (!_terrainSwapInfoById.ContainsKey(mapId))
                _terrainSwapInfoById.Add(mapId, new TerrainSwapInfo());

            var terrainSwapInfo = _terrainSwapInfoById[mapId];
            terrainSwapInfo.Id = mapId;
            terrainSwapInfo.UiMapPhaseIDs.Add(uiMapPhaseId);

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} terrain world maps in {1} ms.", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    private void OnDeleteSpawnData(SpawnData data)
    {
        var templateIt = _spawnGroupDataStorage.LookupByKey(data.SpawnGroupData.GroupId);

        if (templateIt.Flags.HasAnyFlag(SpawnGroupFlags.System)) // system groups don't store their members in the map
            return;

        var spawnDatas = _spawnGroupMapStorage.LookupByKey(data.SpawnGroupData.GroupId);

        foreach (var it in spawnDatas)
        {
            if (it != data)
                continue;

            _spawnGroupMapStorage.Remove(data.SpawnGroupData.GroupId, it);

            return;
        }
    }

    private List<Difficulty> ParseSpawnDifficulties(string difficultyString, string table, ulong spawnId, uint mapId, List<Difficulty> mapDifficulties)
    {
        List<Difficulty> difficulties = new();
        StringArray tokens = new(difficultyString, ',');

        if (tokens.Length == 0)
            return difficulties;

        var isTransportMap = IsTransportMap(mapId);

        foreach (string token in tokens)
        {
            var difficultyId = (Difficulty)Enum.Parse(typeof(Difficulty), token);

            if (difficultyId != 0 && !_cliDB.DifficultyStorage.ContainsKey(difficultyId))
            {
                Log.Logger.Error($"Table `{table}` has {table} (GUID: {spawnId}) with non invalid difficulty id {difficultyId}, skipped.");

                continue;
            }

            if (!isTransportMap && !mapDifficulties.Contains(difficultyId))
            {
                Log.Logger.Error($"Table `{table}` has {table} (GUID: {spawnId}) has unsupported difficulty {difficultyId} for map (Id: {mapId}).");

                continue;
            }

            difficulties.Add(difficultyId);
        }

        difficulties.Sort();

        return difficulties;
    }

    private void PlayerCreateInfoAddItemHelper(uint race, uint class_, uint itemId, int count)
    {
        if (!PlayerInfos.TryGetValue((Race)race, (PlayerClass)class_, out var playerInfo))
            return;

        if (count > 0)
        {
            playerInfo.Items.Add(new PlayerCreateInfoItem(itemId, (uint)count));
        }
        else
        {
            if (count < -1)
                Log.Logger.Error("Invalid count {0} specified on item {1} be removed from original player create info (use -1)!", count, itemId);

            playerInfo.Items.RemoveAll(item => item.ItemId == itemId);
        }
    }

    private void RemoveSpawnDataFromGrid(SpawnData data)
    {
        var cellId = GridDefines.ComputeCellCoord(data.SpawnPoint.X, data.SpawnPoint.Y).GetId();
        var isPersonalPhase = PhasingHandler.IsPersonalPhase(data.PhaseId);

        if (!isPersonalPhase)
            foreach (var difficulty in data.SpawnDifficulties)
            {
                var key = (data.MapId, difficulty);

                if (!_mapObjectGuidsStore.ContainsKey(key) || !_mapObjectGuidsStore[key].ContainsKey(cellId))
                    continue;

                _mapObjectGuidsStore[(data.MapId, difficulty)][cellId].RemoveSpawn(data);
            }
        else
            foreach (var difficulty in data.SpawnDifficulties)
            {
                var key = (data.MapId, difficulty, data.PhaseId);

                if (!_mapPersonalObjectGuidsStore.ContainsKey(key) || !_mapPersonalObjectGuidsStore[key].ContainsKey(cellId))
                    continue;

                _mapPersonalObjectGuidsStore[key][cellId].RemoveSpawn(data);
            }
    }
}