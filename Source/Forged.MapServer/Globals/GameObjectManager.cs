// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.Arenas;
using Forged.MapServer.Chat;
using Forged.MapServer.Chrono;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.D;
using Forged.MapServer.DataStorage.Structs.T;
using Forged.MapServer.DungeonFinding;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.AreaTriggers;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals.Caching;
using Forged.MapServer.Guilds;
using Forged.MapServer.LootManagement;
using Forged.MapServer.Mails;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Movement;
using Forged.MapServer.Phasing;
using Forged.MapServer.Pools;
using Forged.MapServer.Quest;
using Forged.MapServer.Reputation;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Forged.MapServer.World;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Game.Common;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Globals;

public sealed class GameObjectManager
{
    private readonly CharacterDatabase _characterDatabase;
    private readonly ClassFactory _classFactory;
    private readonly CliDB _cliDB;
    private readonly IConfiguration _configuration;
    private readonly Dictionary<uint, CreatureBaseStats> _creatureBaseStatsStorage = new();
    private readonly Dictionary<(uint creatureId, uint gossipMenuId, uint gossipOptionIndex), uint> _creatureDefaultTrainers = new();
    private readonly Dictionary<uint, CreatureLocale> _creatureLocaleStorage = new();
    private readonly MultiMap<uint, uint> _creatureQuestInvolvedRelations = new();
    private readonly MultiMap<uint, uint> _creatureQuestInvolvedRelationsReverse = new();
    private readonly MultiMap<uint, uint> _creatureQuestItemStorage = new();
    private readonly MultiMap<uint, uint> _creatureQuestRelations = new();
    private readonly Dictionary<uint, CreatureAddon> _creatureTemplateAddonStorage = new();
    private readonly Dictionary<uint, StringArray> _cypherStringStorage = new();
    private readonly MultiMap<ulong, DungeonEncounter> _dungeonEncounterStorage = new();
    private readonly MultiMap<int, uint> _exclusiveQuestGroups = new();
    private readonly Dictionary<uint, int> _fishingBaseForAreaStorage = new();
    private readonly Dictionary<ulong, GameObjectAddon> _gameObjectAddonStorage = new();
    private readonly List<uint> _gameObjectForQuestStorage = new();
    private readonly Dictionary<uint, GameObjectLocale> _gameObjectLocaleStorage = new();
    private readonly Dictionary<ulong, GameObjectOverride> _gameObjectOverrideStorage = new();
    private readonly MultiMap<uint, uint> _gameObjectQuestItemStorage = new();
    private readonly Dictionary<ulong, GameObjectTemplateAddon> _gameObjectTemplateAddonStorage = new();
    private readonly MultiMap<uint, uint> _goQuestInvolvedRelations = new();
    private readonly MultiMap<uint, uint> _goQuestInvolvedRelationsReverse = new();
    private readonly MultiMap<uint, uint> _goQuestRelations = new();
    private readonly Dictionary<uint, GossipMenuAddon> _gossipMenuAddonStorage = new();
    private readonly Dictionary<Tuple<uint, uint>, GossipMenuItemsLocale> _gossipMenuItemsLocaleStorage = new();
    private readonly MultiMap<uint, GossipMenuItems> _gossipMenuItemsStorage = new();
    private readonly MultiMap<uint, GossipMenus> _gossipMenusStorage = new();
    private readonly MultiMap<ushort, InstanceSpawnGroupInfo> _instanceSpawnGroupStorage = new();
    private readonly Dictionary<int, JumpChargeParams> _jumpChargeParams = new();
    private readonly LoginDatabase _loginDatabase;
    private readonly MultiMap<byte, MailLevelReward> _mailLevelRewardStorage = new();
    private readonly Dictionary<uint, NpcText> _npcTextStorage = new();
    private readonly ObjectGuidGeneratorFactory _objectGuidGeneratorFactory;
    private readonly Dictionary<uint, PageTextLocale> _pageTextLocaleStorage = new();
    private readonly MultiMap<uint, string> _petHalfName0 = new();
    private readonly MultiMap<uint, string> _petHalfName1 = new();
    private readonly Dictionary<uint, PetLevelInfo[]> _petInfoStore = new();
    private readonly MultiMap<uint, PhaseAreaInfo> _phaseInfoByArea = new();
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
    private readonly Dictionary<uint, string> _realmNameStorage = new();
    private readonly Dictionary<uint, ReputationOnKillEntry> _repOnKillStorage = new();
    private readonly Dictionary<uint, RepRewardRate> _repRewardRateStorage = new();
    private readonly Dictionary<uint, RepSpilloverTemplate> _repSpilloverTemplateStorage = new();
    private readonly List<string> _reservedNamesStorage = new();
    private readonly Dictionary<uint, SceneTemplate> _sceneTemplateStorage = new();
    private readonly ScriptManager _scriptManager;
    private readonly Dictionary<uint, SkillTiersEntry> _skillTiers = new();
    private readonly List<uint> _tavernAreaTriggerStorage = new();
    private readonly MultiMap<Tuple<uint, SummonerType, byte>, TempSummonData> _tempSummonDataStorage = new();
    private readonly Dictionary<uint, TerrainSwapInfo> _terrainSwapInfoById = new();
    private readonly Dictionary<uint, Trainer> _trainers = new();



    private readonly WorldDatabase _worldDatabase;

    // first free id for selected id type
    private uint _auctionId;

    private AzeriteEmpoweredItemFactory _azeriteEmpoweredItemFactory;
    private AzeriteItemFactory _azeriteItemFactory;
    private ConditionManager _conditionManager;
    private ulong _creatureSpawnId;
    private DB2Manager _db2Manager;
    private DisableManager _disableManager;
    private ulong _equipmentSetGuid;
    private ulong _gameObjectSpawnId;
    private GridDefines _gridDefines;
    private uint _hiPetNumber;
    private ItemFactory _itemFactory;
    private LFGManager _lfgManager;
    private LootStoreBox _lootStoreBox;
    private ulong _mailId;
    private MapManager _mapManager;
    private ObjectAccessor _objectAccessor;
    private PhasingHandler _phasingHandler;
    private uint[] _playerXPperLevel;
    private QuestPoolManager _questPoolManager;
    private SpellManager _spellManager;
    private TerrainManager _terrainManager;
    private TransportManager _transportManager;
    private ulong _voidItemId;
    private WorldManager _worldManager;
    private readonly WorldSafeLocationsCache _worldSafeLocationsCache;
    private readonly ItemTemplateCache _itemTemplateCache;
    private readonly VendorItemCache _vendorItemCache;
    private readonly MapSpawnGroupCache _mapSpawnGroupCache;
    private readonly CreatureTemplateCache _creatureTemplateCache;
    private readonly CreatureModelCache _creatureModelCache;
    private readonly CreatureMovementOverrideCache _creatureMovementOverrideCache;
    private readonly SpawnGroupDataCache _spawnGroupDataCache;
    private readonly GameObjectTemplateCache _gameObjectTemplateCache;
    private readonly PageTextCache _pageTextCache;
    private readonly EquipmentInfoCache _equipmentInfoCache;
    private readonly MapObjectCache _mapObjectCache;
    private readonly SpawnDataCacheRouter _spawnDataCacheRouter;
    private readonly CreatureAddonCache _creatureAddonCache;

    public GameObjectManager(CliDB cliDB, WorldDatabase worldDatabase, IConfiguration configuration, ClassFactory classFactory,
                             CharacterDatabase characterDatabase, LoginDatabase loginDatabase, ScriptManager scriptManager, 
                             ObjectGuidGeneratorFactory objectGuidGeneratorFactory, WorldSafeLocationsCache worldSafeLocationsCache,
                             ItemTemplateCache itemTemplateCache)
    {
        _cliDB = cliDB;
        _worldDatabase = worldDatabase;
        _configuration = configuration;
        _classFactory = classFactory;
        _characterDatabase = characterDatabase;
        _loginDatabase = loginDatabase;
        _scriptManager = scriptManager;
        _objectGuidGeneratorFactory = objectGuidGeneratorFactory;
        _worldSafeLocationsCache = worldSafeLocationsCache;
        _itemTemplateCache = itemTemplateCache;

        
    }

    public List<RaceClassAvailability> ClassExpansionRequirements { get; } = new();

    public Dictionary<uint, uint> FactionChangeAchievements { get; set; } = new();

    public Dictionary<uint, uint> FactionChangeItemsAllianceToHorde { get; set; } = new();

    public Dictionary<uint, uint> FactionChangeItemsHordeToAlliance { get; set; } = new();

    public Dictionary<uint, uint> FactionChangeQuests { get; set; } = new();

    public Dictionary<uint, uint> FactionChangeReputation { get; set; } = new();

    public Dictionary<uint, uint> FactionChangeSpells { get; set; } = new();

    public Dictionary<uint, uint> FactionChangeTitles { get; set; } = new();

    public MultiMap<uint, GraveYardData> GraveYardStorage { get; set; } = new();

    public Dictionary<Race, Dictionary<PlayerClass, PlayerInfo>> PlayerInfos { get; } = new();

    public Dictionary<uint, Quest.Quest> QuestTemplates { get; } = new();

    public List<Quest.Quest> QuestTemplatesAutoPush { get; } = new();

    public Dictionary<byte, RaceUnlockRequirement> RaceUnlockRequirements { get; } = new();

    public MultiMap<uint, TerrainSwapInfo> TerrainSwaps { get; } = new();

    public VendorItemCache VendorItemCache
    {
        get { return _vendorItemCache; }
    }

    public MapSpawnGroupCache MapSpawnGroupCache
    {
        get { return _mapSpawnGroupCache; }
    }

    public CreatureTemplateCache CreatureTemplateCache
    {
        get { return _creatureTemplateCache; }
    }

    public CreatureModelCache CreatureModelCache
    {
        get { return _creatureModelCache; }
    }

    public CreatureMovementOverrideCache CreatureMovementOverrideCache
    {
        get { return _creatureMovementOverrideCache; }
    }

    public SpawnGroupDataCache SpawnGroupDataCache
    {
        get { return _spawnGroupDataCache; }
    }

    public GameObjectTemplateCache GameObjectTemplateCache
    {
        get { return _gameObjectTemplateCache; }
    }

    public PageTextCache PageTextCache
    {
        get { return _pageTextCache; }
    }

    public EquipmentInfoCache EquipmentInfoCache
    {
        get { return _equipmentInfoCache; }
    }

    public MapObjectCache MapObjectCache
    {
        get { return _mapObjectCache; }
    }

    public SpawnDataCacheRouter SpawnDataCacheRouter
    {
        get { return _spawnDataCacheRouter; }
    }

    public CreatureAddonCache CreatureAddonCache
    {
        get { return _creatureAddonCache; }
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
        if (!persist)
            return true;

        var stmt = _worldDatabase.GetPreparedStatement(WorldStatements.INS_GRAVEYARD_ZONE);

        stmt.AddValue(0, id);
        stmt.AddValue(1, zoneId);
        stmt.AddValue(2, (uint)team);

        _worldDatabase.Execute(stmt);

        return true;
    }

    public void AddLocaleString(string value, Locale locale, StringArray data)
    {
        if (!string.IsNullOrEmpty(value))
            data[(int)locale] = value;
    }

    public PetNameInvalidReason CheckPetName(string name)
    {
        if (name.Length > 12)
            return PetNameInvalidReason.TooLong;

        var minName = _configuration.GetDefaultValue("MinPetName", 2);

        if (name.Length < minName)
            return PetNameInvalidReason.TooShort;

        var strictMask = _configuration.GetDefaultValue("StrictPetNames", 0u);

        return !IsValidString(name, strictMask, false) ? PetNameInvalidReason.MixedLanguages : PetNameInvalidReason.Success;
    }

    public ResponseCodes CheckPlayerName(string name, Locale locale, bool create = false)
    {
        if (name.Length > 12)
            return ResponseCodes.CharNameTooLong;

        var minName = _configuration.GetDefaultValue("MinPlayerName", 2);

        if (name.Length < minName)
            return ResponseCodes.CharNameTooShort;

        var strictMask = _configuration.GetDefaultValue("StrictPlayerNames", 0u);

        if (!IsValidString(name, strictMask, false, create))
            return ResponseCodes.CharNameMixedLanguages;

        name = name.ToLower();

        for (var i = 2; i < name.Length; ++i)
            if (name[i] == name[i - 1] && name[i] == name[i - 2])
                return ResponseCodes.CharNameThreeConsecutive;

        return _db2Manager.ValidateName(name, locale);
    }

    public void ChooseCreatureFlags(CreatureTemplate cInfo, out ulong npcFlag, out uint unitFlags, out uint unitFlags2, out uint unitFlags3, out uint dynamicFlags, CreatureData data = null)
    {
        npcFlag = data != null && data.Npcflag != 0 ? data.Npcflag : cInfo.Npcflag;
        unitFlags = data != null && data.UnitFlags != 0 ? data.UnitFlags : (uint)cInfo.UnitFlags;
        unitFlags2 = data != null && data.UnitFlags2 != 0 ? data.UnitFlags2 : cInfo.UnitFlags2;
        unitFlags3 = data != null && data.UnitFlags3 != 0 ? data.UnitFlags3 : cInfo.UnitFlags3;
        dynamicFlags = data != null && data.Dynamicflags != 0 ? data.Dynamicflags : cInfo.DynamicFlags;
    }

    public CreatureModel ChooseDisplayId(CreatureTemplate cinfo, CreatureData data = null)
    {
        // Load creature model (display id)
        if (data != null && data.Displayid != 0)
        {
            var model = cinfo.GetModelWithDisplayId(data.Displayid);

            if (model != null)
                return model;
        }

        if (cinfo.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.Trigger))
            return cinfo.GetFirstInvisibleModel();

        {
            var model = cinfo.GetRandomValidModel();

            if (model != null)
                return model;
        }

        // Triggers by default receive the invisible model
        return cinfo.GetFirstInvisibleModel();
    }

    public ExtendedPlayerName ExtractExtendedPlayerName(string name)
    {
        var pos = name.IndexOf('-');

        return pos != -1 ? new ExtendedPlayerName(name.Substring(0, pos), name[(pos + 1)..]) : new ExtendedPlayerName(name, "");
    }

    public void FillSpellSummary()
    {
        UnitAI.FillAISpellInfo(_classFactory.Resolve<SpellManager>());
    }

    public GraveYardData FindGraveYardData(uint id, uint zoneId)
    {
        var range = GraveYardStorage.LookupByKey(zoneId);

        return range.FirstOrDefault(data => data.SafeLocId == id);
    }

    public uint GenerateAuctionID()
    {
        return Interlocked.Increment(ref _auctionId);
    }

    public ulong GenerateCreatureSpawnId()
    {
        return Interlocked.Increment(ref _creatureSpawnId);
    }

    public ulong GenerateEquipmentSetGuid()
    {
        return Interlocked.Increment(ref _equipmentSetGuid);
    }

    public ulong GenerateGameObjectSpawnId()
    {
        return Interlocked.Increment(ref _gameObjectSpawnId);
    }

    public ulong GenerateMailID()
    {
        return Interlocked.Increment(ref _mailId);
    }

    public string GeneratePetName(uint entry)
    {
        var list0 = _petHalfName0[entry];
        var list1 = _petHalfName1[entry];

        if (!list0.Empty() && !list1.Empty())
            return list0[RandomHelper.IRand(0, list0.Count - 1)] + list1[RandomHelper.IRand(0, list1.Count - 1)];

        var cinfo = CreatureTemplateCache.GetCreatureTemplate(entry);

        if (cinfo == null)
            return "";

        var petname = _db2Manager.GetCreatureFamilyPetName(cinfo.Family, _worldManager.DefaultDbcLocale);

        return !string.IsNullOrEmpty(petname) ? petname : cinfo.Name;
    }

    public uint GeneratePetNumber()
    {
        if (_hiPetNumber < 0xFFFFFFFE)
            return _hiPetNumber++;

        Log.Logger.Error("_hiPetNumber Id overflow!! Can't continue, shutting down server. ");
        _worldManager.StopNow();

        return _hiPetNumber++;
    }

    public ulong GenerateVoidStorageItemId()
    {
        if (_voidItemId < 0xFFFFFFFFFFFFFFFE)
            return _voidItemId++;

        Log.Logger.Error("_voidItemId overflow!! Can't continue, shutting down server. ");
        _worldManager.StopNow();

        return _voidItemId++;
    }

    public ClassAvailability GetClassExpansionRequirement(Race raceId, PlayerClass classId)
    {
        var raceClassAvailability = ClassExpansionRequirements.Find(raceClass => raceClass.RaceID == (byte)raceId);

        var classAvailability = raceClassAvailability?.Classes.Find(availability => availability.ClassID == (byte)classId);

        return classAvailability;
    }

    public ClassAvailability GetClassExpansionRequirementFallback(byte classId)
    {
        foreach (var raceClassAvailability in ClassExpansionRequirements)
            foreach (var classAvailability in raceClassAvailability.Classes.Where(classAvailability => classAvailability.ClassID == classId))
                return classAvailability;

        return null;
    }

    public WorldSafeLocsEntry GetClosestGraveYard(WorldLocation location, TeamFaction team, WorldObject conditionObject)
    {
        var mapId = location.MapId;

        // search for zone associated closest graveyard
        var zoneId = _terrainManager.GetZoneId(conditionObject != null ? conditionObject.Location.PhaseShift : _phasingHandler.EmptyPhaseShift, mapId, location);

        if (zoneId == 0)
            if (location.Z > -500)
            {
                Log.Logger.Error("ZoneId not found for map {0} coords ({1}, {2}, {3})", mapId, location.X, location.Y, location.Z);

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
        var mapEntry = _cliDB.MapStorage.LookupByKey(mapId);

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
            var entry = _worldSafeLocationsCache.GetWorldSafeLoc(data.SafeLocId);

            if (entry == null)
            {
                Log.Logger.Error("Table `game_graveyard_zone` has record for not existing graveyard (WorldSafeLocs.dbc id) {0}, skipped.", data.SafeLocId);

                continue;
            }

            // skip enemy faction graveyard
            // team == 0 case can be at call from .neargrave
            if (data.Team != 0 && team != 0 && data.Team != (uint)team)
                continue;

            if (conditionObject != null)
            {
                if (!_conditionManager.IsObjectMeetingNotGroupedConditions(ConditionSourceType.Graveyard, data.SafeLocId, conditionSource))
                    continue;

                if (entry.Location.MapId == mapEntry.ParentMapID && !conditionObject.Location.PhaseShift.HasVisibleMapId(entry.Location.MapId))
                    continue;
            }

            // find now nearest graveyard at other map
            if (mapId != entry.Location.MapId && mapEntry != null && entry.Location.MapId != mapEntry.ParentMapID)
            {
                // if find graveyard at different map from where entrance placed (or no entrance data), use any first
                if (mapEntry.CorpseMapID < 0 || mapEntry.CorpseMapID != entry.Location.MapId || mapEntry.Corpse is { X: 0, Y: 0 })
                {
                    // not have any corrdinates for check distance anyway
                    entryFar = entry;

                    continue;
                }

                // at entrance map calculate distance (2D);
                var dist2 = (entry.Location.X - mapEntry.Corpse.X) * (entry.Location.X - mapEntry.Corpse.X) + (entry.Location.Y - mapEntry.Corpse.Y) * (entry.Location.Y - mapEntry.Corpse.Y);

                if (foundEntr)
                {
                    if (!(dist2 < distEntr))
                        continue;

                    distEntr = dist2;
                    entryEntr = entry;
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
                var dist2 = (entry.Location.X - location.X) * (entry.Location.X - location.X) + (entry.Location.Y - location.Y) * (entry.Location.Y - location.Y) + (entry.Location.Z - location.Z) * (entry.Location.Z - location.Z);

                if (foundNear)
                {
                    if (!(dist2 < distNear))
                        continue;

                    distNear = dist2;
                    entryNear = entry;
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

        return entryEntr ?? entryFar;
    }

    public CreatureBaseStats GetCreatureBaseStats(uint level, uint unitClass)
    {
        return _creatureBaseStatsStorage.TryGetValue(MathFunctions.MakePair16(level, unitClass), out var stats) ? stats : new DefaultCreatureBaseStats();
    }

    public uint GetCreatureDefaultTrainer(uint creatureId)
    {
        return GetCreatureTrainerForGossipOption(creatureId, 0, 0);
    }

    public CreatureLocale GetCreatureLocale(uint entry)
    {
        return _creatureLocaleStorage.LookupByKey(entry);
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

    public MultiMap<uint, uint> GetCreatureQuestRelationMapHack()
    {
        return _creatureQuestRelations;
    }

    public QuestRelationResult GetCreatureQuestRelations(uint entry)
    {
        return GetQuestRelationsFrom(_creatureQuestRelations, entry, true);
    }

    public CreatureAddon GetCreatureTemplateAddon(uint entry)
    {
        return _creatureTemplateAddonStorage.LookupByKey(entry);
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
        return team switch
        {
            TeamFaction.Horde    => _worldSafeLocationsCache.GetWorldSafeLoc(10),
            TeamFaction.Alliance => _worldSafeLocationsCache.GetWorldSafeLoc(4),
            _                    => null
        };
    }

    public List<DungeonEncounter> GetDungeonEncounterList(uint mapId, Difficulty difficulty)
    {
        return _dungeonEncounterStorage.LookupByKey(MathFunctions.MakePair64(mapId, (uint)difficulty));
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

    public GameObjectTemplateAddon GetGameObjectTemplateAddon(uint entry)
    {
        return _gameObjectTemplateAddonStorage.LookupByKey(entry);
    }

    public List<uint> GetGOQuestInvolvedRelationReverseBounds(uint questId)
    {
        return _goQuestInvolvedRelationsReverse.LookupByKey(questId);
    }

    public QuestRelationResult GetGOQuestInvolvedRelations(uint entry)
    {
        return GetQuestRelationsFrom(_goQuestInvolvedRelations, entry, false);
    }

    public MultiMap<uint, uint> GetGOQuestRelationMapHack()
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

    public JumpChargeParams GetJumpChargeParams(int id)
    {
        return _jumpChargeParams.LookupByKey(id);
    }

    public void GetLocaleString(StringArray data, Locale locale, ref string value)
    {
        if (data.Length > (int)locale && !string.IsNullOrEmpty(data[(int)locale]))
            value = data[(int)locale];
    }

    public MailLevelReward GetMailLevelReward(uint level, ulong raceMask)
    {
        if (_mailLevelRewardStorage.TryGetValue((byte)level, out var mailList))
            return null;

        foreach (var mailReward in mailList)
            if (Convert.ToBoolean(mailReward.RaceMask & raceMask))
                return mailReward;

        return null;
    }

    public uint GetMaxLevelForExpansion(Expansion expansion)
    {
        return expansion switch
        {
            Expansion.Classic => _configuration.GetDefaultValue<uint>("Character.MaxLevelFor.Classic", 30),
            Expansion.BurningCrusade => _configuration.GetDefaultValue<uint>("Character.MaxLevelFor.BurningCrusade", 30),
            Expansion.WrathOfTheLichKing => _configuration.GetDefaultValue<uint>("Character.MaxLevelFor.WrathOfTheLichKing", 30),
            Expansion.Cataclysm => _configuration.GetDefaultValue<uint>("Character.MaxLevelFor.Cataclysm", 35),
            Expansion.MistsOfPandaria => _configuration.GetDefaultValue<uint>("Character.MaxLevelFor.MistsOfPandaria", 35),
            Expansion.WarlordsOfDraenor => _configuration.GetDefaultValue<uint>("Character.MaxLevelFor.WarlordsOfDraenor", 40),
            Expansion.Legion => _configuration.GetDefaultValue<uint>("Character.MaxLevelFor.Legion", 45),
            Expansion.BattleForAzeroth => _configuration.GetDefaultValue<uint>("Character.MaxLevelFor.BattleForAzeroth", 50),
            Expansion.ShadowLands => _configuration.GetDefaultValue<uint>("Character.MaxLevelFor.ShadowLands", 60),
            Expansion.Dragonflight => _configuration.GetDefaultValue<uint>("Character.MaxLevelFor.Dragonflight", 70),
            _ => 0
        };
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

    public PageTextLocale GetPageTextLocale(uint entry)
    {
        return _pageTextLocaleStorage.LookupByKey(entry);
    }

    public PetLevelInfo GetPetLevelInfo(uint creatureid, uint level)
    {
        var configMaxLevel = _configuration.GetDefaultValue("MaxPlayerLevel", (uint)SharedConst.DefaultMaxLevel);

        if (level > configMaxLevel)
            level = configMaxLevel;

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

    public PlayerChoiceLocale GetPlayerChoiceLocale(int choiceID)
    {
        return _playerChoiceLocales.LookupByKey((uint)choiceID);
    }

    public void GetPlayerClassLevelInfo(PlayerClass @class, uint level, out uint baseMana)
    {
        baseMana = 0;

        if (level < 1 || @class >= PlayerClass.Max)
            return;

        if (level > _configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel))
            level = (byte)_configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel);

        var mp = _cliDB.BaseMPGameTable.GetRow(level);

        if (mp == null)
        {
            Log.Logger.Error("Tried to get non-existant Class-Level combination data for base mp. Class {0} Level {1}", @class, level);

            return;
        }

        baseMana = (uint)CliDB.GetGameTableColumnForClass(mp, @class);
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

    public PlayerLevelInfo GetPlayerLevelInfo(Race race, PlayerClass @class, uint level)
    {
        if (level < 1 || race >= Race.Max || @class >= PlayerClass.Max)
            return null;

        if (!PlayerInfos.TryGetValue(race, @class, out var pInfo))
            return null;

        if (level <= _configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel))
            return pInfo.LevelInfo[level - 1];

        return BuildPlayerLevelInfo(race, @class, level);
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
        return QuestTemplates.LookupByKey(questId);
    }

    public RaceUnlockRequirement GetRaceUnlockRequirement(Race race)
    {
        return RaceUnlockRequirements.LookupByKey((byte)race);
    }

    public string GetRealmName(uint realm)
    {
        return _realmNameStorage.LookupByKey(realm);
    }

    public bool GetRealmName(uint realmId, ref string name, ref string normalizedName)
    {
        if (_realmNameStorage.TryGetValue(realmId, out var realmName))
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

    public string GetScriptsTableNameByType(ScriptsType type)
    {
        return type switch
        {
            ScriptsType.Spell => "spell_scripts",
            ScriptsType.Event => "event_scripts",
            ScriptsType.Waypoint => "waypoint_scripts",
            _ => ""
        };
    }

    public SkillTiersEntry GetSkillTier(uint skillTierId)
    {
        return _skillTiers.LookupByKey(skillTierId);
    }

    public List<TempSummonData> GetSummonGroup(uint summonerId, SummonerType summonerType, byte group)
    {
        var key = Tuple.Create(summonerId, summonerType, group);

        return _tempSummonDataStorage.LookupByKey(key);
    }

    public uint GetTaxiMountDisplayId(uint id, TeamFaction team, bool allowedAltTeam = false)
    {
        CreatureModel mountModel = new();
        CreatureTemplate mountInfo = null;

        // select mount creature id
        if (_cliDB.TaxiNodesStorage.TryGetValue(id, out var node))
        {
            var mountEntry = team == TeamFaction.Alliance ? node.MountCreatureID[1] : node.MountCreatureID[0];

            // Fix for Alliance not being able to use Acherus taxi
            // only one mount type for both sides
            if (mountEntry == 0 && allowedAltTeam)
                // Simply reverse the selection. At least one team in theory should have a valid mount ID to choose.
                mountEntry = team == TeamFaction.Alliance ? node.MountCreatureID[0] : node.MountCreatureID[1];

            mountInfo = CreatureTemplateCache.GetCreatureTemplate(mountEntry);

            if (mountInfo != null)
            {
                var model = mountInfo.GetRandomValidModel();

                if (model == null)
                {
                    Log.Logger.Error($"No displayid found for the taxi mount with the entry {mountEntry}! Can't load it!");

                    return 0;
                }

                mountModel = model;
            }
        }

        // minfo is not actually used but the mount_id was updated
        CreatureModelCache.GetCreatureModelRandomGender(ref mountModel, mountInfo);

        return mountModel.CreatureDisplayId;
    }

    public void GetTaxiPath(uint source, uint destination, out uint path, out uint cost)
    {
        if (!_cliDB.TaxiPathSetBySource.TryGetValue(source, out var pathSet))
        {
            path = 0;
            cost = 0;

            return;
        }

        if (!pathSet.TryGetValue(destination, out var destI))
        {
            path = 0;
            cost = 0;

            return;
        }

        cost = destI.price;
        path = destI.Id;
    }

    public TerrainSwapInfo GetTerrainSwapInfo(uint terrainSwapId)
    {
        return _terrainSwapInfoById.LookupByKey(terrainSwapId);
    }

    public Trainer GetTrainer(uint trainerId)
    {
        return _trainers.LookupByKey(trainerId);
    }

    public uint GetXPForLevel(uint level)
    {
        return level < _playerXPperLevel.Length ? _playerXPperLevel[level] : 0;
    }

    public void Initialize(ClassFactory cf)
    {
        _itemFactory = cf.Resolve<ItemFactory>();
        _azeriteItemFactory = cf.Resolve<AzeriteItemFactory>();
        _azeriteEmpoweredItemFactory = cf.Resolve<AzeriteEmpoweredItemFactory>();
        _db2Manager = _classFactory.Resolve<DB2Manager>();
        _spellManager = _classFactory.Resolve<SpellManager>();
        _worldManager = _classFactory.Resolve<WorldManager>();
        _terrainManager = _classFactory.Resolve<TerrainManager>();
        _conditionManager = _classFactory.Resolve<ConditionManager>();
        _lootStoreBox = _classFactory.Resolve<LootStoreBox>();
        _lfgManager = _classFactory.Resolve<LFGManager>();
        _mapManager = _classFactory.Resolve<MapManager>();
        _transportManager = _classFactory.Resolve<TransportManager>();
        _disableManager = _classFactory.Resolve<DisableManager>();
        _objectAccessor = _classFactory.Resolve<ObjectAccessor>();
        _questPoolManager = _classFactory.Resolve<QuestPoolManager>();
        _gridDefines = _classFactory.Resolve<GridDefines>();
        _phasingHandler = _classFactory.Resolve<PhasingHandler>();

        FillSpellSummary();
        SetHighestGuids();

        if (!LoadCypherStrings())
            Environment.Exit(1);

        LoadCreatureLocales();
        LoadGameObjectLocales();
        LoadQuestTemplateLocale();
        LoadQuestOfferRewardLocale();
        LoadQuestRequestItemsLocale();
        LoadQuestObjectivesLocale();
        LoadPageTextLocales();
        LoadGossipMenuItemsLocales();
        LoadPointOfInterestLocales();
        LoadGameObjectTemplateAddons();
        LoadNPCText();
        LoadCreatureTemplateAddons();
        LoadCreatureScalingData();
        LoadReputationRewardRate();
        LoadReputationOnKill();
        LoadReputationSpilloverTemplate();
        LoadPointsOfInterest();
        LoadCreatureClassLevelStats();
        LoadTempSummons(); // must be after LoadCreatureTemplates() and LoadGameObjectTemplates()
        LoadInstanceSpawnGroups();
        LoadGameObjectAddons(); // must be after LoadGameObjects()
        LoadGameObjectOverrides(); // must be after LoadGameObjects()
        LoadGameObjectQuestItems();
        LoadCreatureQuestItems();
        LoadQuests();
        LoadQuestPOI();
        LoadQuestStartersAndEnders(); // must be after quest load
        LoadQuestGreetings();
        LoadQuestGreetingLocales();
        LoadQuestAreaTriggers(); // must be after LoadQuests
        LoadTavernAreaTriggers();
        LoadAreaTriggerScripts();
        LoadInstanceEncounters();
        LoadGraveyardZones();
        LoadSceneTemplates(); // must be before LoadPlayerInfo
        LoadPlayerInfo();
        LoadPetNames();
        LoadPlayerChoices();
        LoadPlayerChoicesLocale();
        LoadJumpChargeParams();
        LoadPetNumber();
        LoadPetLevelInfo();
        LoadMailLevelRewards();
        LoadFishingBaseSkillLevel();
        LoadSkillTiers();
        LoadReservedPlayersNames();
        LoadGameObjectForQuests();
        LoadTrainers(); // must be after load CreatureTemplate
        LoadGossipMenu();
        LoadGossipMenuItems();
        LoadGossipMenuAddon();
        LoadCreatureTrainers(); // must be after LoadGossipMenuItems
        LoadPhases();
        LoadFactionChangeAchievements();
        LoadFactionChangeSpells();
        LoadFactionChangeItems();
        LoadFactionChangeQuests();
        LoadFactionChangeReputations();
        LoadFactionChangeTitles();
        ReturnOrDeleteOldMails(false);
        InitializeQueriesData(QueryDataGroup.All);
        LoadRaceAndClassExpansionRequirements();
        LoadRealmNames();
        LoadPhaseNames();
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
            foreach (var creaturePair in CreatureTemplateCache.CreatureTemplates)
                creaturePair.Value.InitializeQueryData();

        // Initialize Query Data for gameobjects
        if (mask.HasAnyFlag(QueryDataGroup.Gameobjects))
            foreach (var gameobjectPair in GameObjectTemplateCache.GameObjectTemplates)
                gameobjectPair.Value.InitializeQueryData(this);

        // Initialize Query Data for quests
        if (mask.HasAnyFlag(QueryDataGroup.Quests))
            foreach (var questPair in QuestTemplates)
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

    public bool IsTavernAreaTrigger(uint triggerID)
    {
        return _tavernAreaTriggerStorage.Contains(triggerID);
    }

 
    public bool IsValidCharterName(string name)
    {
        if (name.Length > 24)
            return false;

        var minName = _configuration.GetDefaultValue("MinCharterName", 2);

        if (name.Length < minName)
            return false;

        var strictMask = _configuration.GetDefaultValue("StrictCharterNames", 0u);

        return IsValidString(name, strictMask, true);
    }

    //Scripts

    public void LoadAreaTriggerScripts()
    {
        var oldMSTime = Time.MSTime;

        _scriptManager.AreaTriggerScriptStorage.Clear(); // need for reload case
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

            if (!_cliDB.AreaTriggerStorage.TryGetValue(id, out _))
            {
                Log.Logger.Error("Area trigger (Id:{0}) does not exist in `AreaTrigger.dbc`.", id);

                continue;
            }

            ++count;
            _scriptManager.AreaTriggerScriptStorage.AddUnique(id, _scriptManager.GetScriptId(scriptName));
        } while (result.NextRow());

        _scriptManager.AreaTriggerScriptStorage.RemoveIfMatching(script =>
        {
            var areaTriggerScriptLoaders = _scriptManager.CreateAreaTriggerScriptLoaders(script.Key);

            foreach (var pair in areaTriggerScriptLoaders)
            {
                var areaTriggerScript = pair.Key.GetAreaTriggerScript();
                var valid = true;

                if (areaTriggerScript == null)
                {
                    Log.Logger.Error("Functions LoadAreaTriggerScripts() of script `{0}` do not return object - script skipped", _scriptManager.GetScriptName(pair.Value));
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
            var level = result.Read<byte>(0);
            var @class = result.Read<byte>(1);

            if (@class == 0 || ((1 << (@class - 1)) & (int)PlayerClass.ClassMaskAllCreatures) == 0)
                Log.Logger.Error("Creature base stats for level {0} has invalid class {1}", level, @class);

            CreatureBaseStats stats = new()
            {
                BaseMana = result.Read<uint>(2),
                AttackPower = result.Read<ushort>(3),
                RangedAttackPower = result.Read<ushort>(4)
            };

            _creatureBaseStatsStorage.Add(MathFunctions.MakePair16(level, @class), stats);

            ++count;
        } while (result.NextRow());

        foreach (var creatureTemplate in CreatureTemplateCache.CreatureTemplates.Values)
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

    public void LoadCreatureQuestEnders()
    {
        LoadQuestRelationsHelper(_creatureQuestInvolvedRelations, _creatureQuestInvolvedRelationsReverse, "creature_questender");

        foreach (var pair in _creatureQuestInvolvedRelations.KeyValueList)
        {
            var cInfo = CreatureTemplateCache.GetCreatureTemplate(pair.Key);

            if (cInfo == null)
                Log.Logger.Error("Table `creature_questender` have data for not existed creature entry ({0}) and existed quest {1}", pair.Key, pair.Value);
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

            if (!CreatureTemplateCache.CreatureTemplates.ContainsKey(entry))
            {
                if (_configuration.GetDefaultValue("load:autoclean", false))
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
            var cInfo = CreatureTemplateCache.GetCreatureTemplate(pair.Key);

            if (cInfo == null)
                Log.Logger.Debug("Table `creature_queststarter` have data for not existed creature entry ({0}) and existed quest {1}", pair.Key, pair.Value);
            else if (!Convert.ToBoolean(cInfo.Npcflag & (uint)NPCFlags.QuestGiver))
            {
                Log.Logger.Verbose("Table `creature_queststarter` has creature entry ({0}) for quest {1}, but npcflag does not include UNIT_NPC_FLAG_QUESTGIVER", pair.Key, pair.Value);
                cInfo.Npcflag &= (uint)NPCFlags.QuestGiver;
            }
        }
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

            if (!CreatureTemplateCache.CreatureTemplates.TryGetValue(entry, out var template))
            {
                if (_configuration.GetDefaultValue("load:autoclean", false))
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

            if (CreatureTemplateCache.GetCreatureTemplate(entry) == null)
            {
                if (_configuration.GetDefaultValue("load:autoclean", false))
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

                var additionalSpellInfo = _spellManager.GetSpellInfo(spellId);

                if (additionalSpellInfo == null)
                {
                    if (_configuration.GetDefaultValue("load:autoclean", false))
                        _worldDatabase.Execute($"DELETE FROM creature_template_addon WHERE entry = {entry}");
                    else
                        Log.Logger.Error($"Creature (Entry: {entry}) has wrong spell {spellId} defined in `auras` field in `creature_template_addon`.");

                    continue;
                }

                if (additionalSpellInfo.HasAura(AuraType.ControlVehicle))
                    Log.Logger.Debug($"Creature (Entry: {entry}) has SPELL_AURA_CONTROL_VEHICLE aura {spellId} defined in `auras` field in `creature_template_addon`.");

                if (creatureAddon.Auras.Contains(spellId))
                {
                    if (_configuration.GetDefaultValue("load:autoclean", false))
                        _worldDatabase.Execute($"DELETE FROM creature_template_addon WHERE entry = {entry}");
                    else
                        Log.Logger.Error($"Creature (Entry: {entry}) has duplicate aura (spell {spellId}) in `auras` field in `creature_template_addon`.");

                    continue;
                }

                if (additionalSpellInfo.Duration > 0)
                {
                    if (_configuration.GetDefaultValue("load:autoclean", false))
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

                if (CreatureTemplateCache.GetCreatureTemplate(creatureId) == null)
                {
                    if (_configuration.GetDefaultValue("load:autoclean", false))
                        _worldDatabase.Execute($"DELETE FROM creature_trainer WHERE CreatureID = {creatureId}");
                    else
                        Log.Logger.Error($"Table `creature_trainer` references non-existing creature template (CreatureId: {creatureId}), ignoring");

                    continue;
                }

                if (GetTrainer(trainerId) == null)
                {
                    if (_configuration.GetDefaultValue("load:autoclean", false))
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
                        if (_configuration.GetDefaultValue("load:autoclean", false))
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

    public void LoadEventScripts()
    {
        LoadScripts(ScriptsType.Event);

        List<uint> evtScripts = new();

        // Load all possible script entries from gameobjects
        foreach (var go in GameObjectTemplateCache.GameObjectTemplates)
        {
            var eventId = go.Value.GetEventScriptId();

            if (eventId != 0)
                evtScripts.Add(eventId);
        }

        // Load all possible script entries from spells
        foreach (var spellNameEntry in _cliDB.SpellNameStorage.Values)
        {
            var spell = _spellManager.GetSpellInfo(spellNameEntry.Id);

            if (spell != null)
                foreach (var spellEffectInfo in spell.Effects)
                    if (spellEffectInfo.IsEffectName(SpellEffectName.SendEvent))
                        if (spellEffectInfo.MiscValue != 0)
                            evtScripts.Add((uint)spellEffectInfo.MiscValue);
        }

        foreach (var pathIdx in _cliDB.TaxiPathNodesByPath)
            for (uint nodeIdx = 0; nodeIdx < pathIdx.Value.Length; ++nodeIdx)
            {
                var node = pathIdx.Value[nodeIdx];

                if (node.ArrivalEventID != 0)
                    evtScripts.Add(node.ArrivalEventID);

                if (node.DepartureEventID != 0)
                    evtScripts.Add(node.DepartureEventID);
            }

        // Then check if all scripts are in above list of possible script entries
        foreach (var script in _scriptManager.EventScripts)
        {
            var id = evtScripts.Find(p => p == script.Key);

            if (id == 0)
                Log.Logger.Error("Table `event_scripts` has script (Id: {0}) not referring to any gameobject_template type 10 data2 field, type 3 data6 field, type 13 data 2 field or any spell effect {1}",
                                 script.Key,
                                 SpellEffectName.SendEvent);
        }
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

        foreach (var itemPair in _itemTemplateCache.ItemTemplates)
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

            if (!_spellManager.HasSpellInfo(alliance))
                Log.Logger.Error("Spell {0} (alliance_id) referenced in `player_factionchange_spells` does not exist, pair skipped!", alliance);
            else if (!_spellManager.HasSpellInfo(horde))
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

            if (!_cliDB.AreaTableStorage.TryGetValue(entry, out _))
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

            var goData = GameObjectCache.GetGameObjectData(guid);

            if (goData == null)
            {
                if (_configuration.GetDefaultValue("load:autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM gameobject_addon WHERE guid = {guid}");
                else
                    Log.Logger.Error($"GameObject (GUID: {guid}) does not exist but has a record in `gameobject_addon`");

                continue;
            }

            GameObjectAddon gameObjectAddon = new()
            {
                ParentRotation = new Quaternion(result.Read<float>(1), result.Read<float>(2), result.Read<float>(3), result.Read<float>(4)),
                InvisibilityType = (InvisibilityType)result.Read<byte>(5),
                InvisibilityValue = result.Read<uint>(6),
                WorldEffectID = result.Read<uint>(7),
                AIAnimKitID = result.Read<uint>(8)
            };

            if (gameObjectAddon.InvisibilityType >= InvisibilityType.Max)
            {
                Log.Logger.Error($"GameObject (GUID: {guid}) has invalid InvisibilityType in `gameobject_addon`, disabled invisibility");
                gameObjectAddon.InvisibilityType = InvisibilityType.General;
                gameObjectAddon.InvisibilityValue = 0;
            }

            if (gameObjectAddon.InvisibilityType != 0 && gameObjectAddon.InvisibilityValue == 0)
            {
                Log.Logger.Error($"GameObject (GUID: {guid}) has InvisibilityType set but has no InvisibilityValue in `gameobject_addon`, set to 1");
                gameObjectAddon.InvisibilityValue = 1;
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

        if (GameObjectTemplateCache.GameObjectTemplates.Empty())
        {
            Log.Logger.Information("Loaded 0 GameObjects for quests");

            return;
        }

        uint count = 0;

        // collect GO entries for GO that must activated
        foreach (var pair in GameObjectTemplateCache.GameObjectTemplates)
        {
            switch (pair.Value.type)
            {
                case GameObjectTypes.QuestGiver:
                    break;

                case GameObjectTypes.Chest:
                {
                    // scan GO chest with loot including quest items
                    // find quest loot for GO
                    if (pair.Value.Chest.questID != 0 || _lootStoreBox.Gameobject.HaveQuestLootFor(pair.Value.Chest.chestLoot) || _lootStoreBox.Gameobject.HaveQuestLootFor(pair.Value.Chest.chestPersonalLoot) || _lootStoreBox.Gameobject.HaveQuestLootFor(pair.Value.Chest.chestPushLoot))
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
                    if (_lootStoreBox.Gameobject.HaveQuestLootFor(pair.Value.GatheringNode.chestLoot))
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
            var goData = GameObjectCache.GetGameObjectData(spawnId);

            if (goData == null)
            {
                if (_configuration.GetDefaultValue("load:autoclean", false))
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
            var goInfo = GameObjectTemplateCache.GetGameObjectTemplate(pair.Key);

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

            if (!GameObjectTemplateCache.GameObjectTemplates.ContainsKey(entry))
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
            var goInfo = GameObjectTemplateCache.GetGameObjectTemplate(pair.Key);

            if (goInfo == null)
                Log.Logger.Error("Table `gameobject_queststarter` have data for not existed gameobject entry ({0}) and existed quest {1}", pair.Key, pair.Value);
            else if (goInfo.type != GameObjectTypes.QuestGiver)
                Log.Logger.Error("Table `gameobject_queststarter` have data gameobject entry ({0}) for quest {1}, but GO is not GAMEOBJECT_TYPE_QUESTGIVER", pair.Key, pair.Value);
        }
    }

    //GameObjects

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

            var got = GameObjectTemplateCache.GetGameObjectTemplate(entry);

            if (got == null)
            {
                if (_configuration.GetDefaultValue("load:autoclean", false))
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
                if (_configuration.GetDefaultValue("load:autoclean", false))
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

            if (_cliDB.FactionStorage.TryGetValue((uint)addon.FriendshipFactionId, out var faction))
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
                    if (_configuration.GetDefaultValue("load:autoclean", false))
                        _worldDatabase.Execute($"UPDATE gossip_menu_option SET OptionBroadcastTextID = 0 WHERE MenuID = {gMenuItem.MenuId}");
                    else
                        Log.Logger.Error($"Table `gossip_menu_option` for MenuId {gMenuItem.MenuId}, OptionIndex {gMenuItem.OrderIndex} has non-existing or incompatible OptionBroadcastTextId {gMenuItem.OptionBroadcastTextId}, ignoring.");

                    gMenuItem.OptionBroadcastTextId = 0;
                }

            if (gMenuItem.Language != 0 && !_cliDB.LanguagesStorage.ContainsKey(gMenuItem.Language))
            {
                if (_configuration.GetDefaultValue("load:autoclean", false))
                    _worldDatabase.Execute($"UPDATE gossip_menu_option SET OptionID = 0 WHERE MenuID = {gMenuItem.MenuId}");
                else
                    Log.Logger.Error($"Table `gossip_menu_option` for menu {gMenuItem.MenuId}, id {gMenuItem.OrderIndex} use non-existing Language {gMenuItem.Language}, ignoring");

                gMenuItem.Language = 0;
            }

            if (gMenuItem.ActionMenuId != 0 && gMenuItem.OptionNpc != GossipOptionNpc.None)
            {
                if (_configuration.GetDefaultValue("load:autoclean", false))
                    _worldDatabase.Execute($"UPDATE gossip_menu_option SET ActionMenuID = 0 WHERE MenuID = {gMenuItem.MenuId}");
                else
                    Log.Logger.Error($"Table `gossip_menu_option` for menu {gMenuItem.MenuId}, id {gMenuItem.OrderIndex} can not use ActionMenuID for GossipOptionNpc different from GossipOptionNpc.None, ignoring");

                gMenuItem.ActionMenuId = 0;
            }

            if (gMenuItem.ActionPoiId != 0)
            {
                if (gMenuItem.OptionNpc != GossipOptionNpc.None)
                {
                    if (_configuration.GetDefaultValue("load:autoclean", false))
                        _worldDatabase.Execute($"UPDATE gossip_menu_option SET ActionPoiID = 0 WHERE MenuID = {gMenuItem.MenuId}");
                    else
                        Log.Logger.Error($"Table `gossip_menu_option` for menu {gMenuItem.MenuId}, id {gMenuItem.OrderIndex} can not use ActionPoiID for GossipOptionNpc different from GossipOptionNpc.None, ignoring");

                    gMenuItem.ActionPoiId = 0;
                }
                else if (GetPointOfInterest(gMenuItem.ActionPoiId) == null)
                {
                    if (_configuration.GetDefaultValue("load:autoclean", false))
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
                    if (_configuration.GetDefaultValue("load:autoclean", false))
                        _worldDatabase.Execute($"UPDATE gossip_menu_option SET GossipNpcOptionID = 0 WHERE MenuID = {gMenuItem.MenuId}");
                    else
                        Log.Logger.Error($"Table `gossip_menu_option` for menu {gMenuItem.MenuId}, id {gMenuItem.OrderIndex} use non-existing GossipNPCOption {gMenuItem.GossipNpcOptionId}, ignoring");

                    gMenuItem.GossipNpcOptionId = null;
                }
            }
            else
            {
                if (optionToNpcOption.TryGetValue(gMenuItem.GossipOptionId, out var npcOptionId))
                    gMenuItem.GossipNpcOptionId = (int)npcOptionId;
            }

            if (gMenuItem.BoxBroadcastTextId != 0)
                if (!_cliDB.BroadcastTextStorage.ContainsKey(gMenuItem.BoxBroadcastTextId))
                {
                    if (_configuration.GetDefaultValue("load:autoclean", false))
                        _worldDatabase.Execute($"UPDATE gossip_menu_option SET BoxBroadcastTextID = 0 WHERE MenuID = {gMenuItem.MenuId}");
                    else
                        Log.Logger.Error($"Table `gossip_menu_option` for MenuId {gMenuItem.MenuId}, OptionIndex {gMenuItem.OrderIndex} has non-existing or incompatible BoxBroadcastTextId {gMenuItem.BoxBroadcastTextId}, ignoring.");

                    gMenuItem.BoxBroadcastTextId = 0;
                }

            if (gMenuItem.SpellId.HasValue)
                if (!_spellManager.HasSpellInfo((uint)gMenuItem.SpellId.Value))
                {
                    if (_configuration.GetDefaultValue("load:autoclean", false))
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

            var entry = _worldSafeLocationsCache.GetWorldSafeLoc(safeLocId);

            if (entry == null)
            {
                Log.Logger.Error("Table `graveyard_zone` has a record for not existing graveyard (WorldSafeLocs.dbc id) {0}, skipped.", safeLocId);

                continue;
            }

            if (!_cliDB.AreaTableStorage.TryGetValue(zoneId, out _))
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

            if (!_cliDB.DungeonEncounterStorage.TryGetValue(entry, out var dungeonEncounter))
            {
                Log.Logger.Error("Table `instance_encounters` has an invalid encounter id {0}, skipped!", entry);

                continue;
            }

            if (lastEncounterDungeon != 0 && _lfgManager.GetLFGDungeonEntry(lastEncounterDungeon) == 0)
            {
                Log.Logger.Error("Table `instance_encounters` has an encounter {0} ({1}) marked as final for invalid dungeon id {2}, skipped!",
                                 entry,
                                 dungeonEncounter.Name[_worldManager.DefaultDbcLocale],
                                 lastEncounterDungeon);

                continue;
            }

            if (dungeonLastBosses.TryGetValue(lastEncounterDungeon, out var pair))
            {
                if (pair != null)
                {
                    Log.Logger.Error("Table `instance_encounters` specified encounter {0} ({1}) as last encounter but {2} ({3}) is already marked as one, skipped!",
                                     entry,
                                     dungeonEncounter.Name[_worldManager.DefaultDbcLocale],
                                     pair.Item1,
                                     pair.Item2.Name[_worldManager.DefaultDbcLocale]);

                    continue;
                }

                dungeonLastBosses[lastEncounterDungeon] = Tuple.Create(entry, dungeonEncounter);
            }

            switch (creditType)
            {
                case EncounterCreditType.KillCreature:
                {
                    var creatureInfo = CreatureTemplateCache.GetCreatureTemplate(creditEntry);

                    if (creatureInfo == null)
                    {
                        Log.Logger.Error("Table `instance_encounters` has an invalid creature (entry {0}) linked to the encounter {1} ({2}), skipped!",
                                         creditEntry,
                                         entry,
                                         dungeonEncounter.Name[_worldManager.DefaultDbcLocale]);

                        continue;
                    }

                    creatureInfo.FlagsExtra |= CreatureFlagsExtra.DungeonBoss;

                    for (byte diff = 0; diff < SharedConst.MaxCreatureDifficulties; ++diff)
                    {
                        var diffEntry = creatureInfo.DifficultyEntry[diff];

                        if (diffEntry != 0)
                        {
                            var diffInfo = CreatureTemplateCache.GetCreatureTemplate(diffEntry);

                            if (diffInfo != null)
                                diffInfo.FlagsExtra |= CreatureFlagsExtra.DungeonBoss;
                        }
                    }

                    break;
                }
                case EncounterCreditType.CastSpell:
                    if (!_spellManager.HasSpellInfo(creditEntry))
                    {
                        Log.Logger.Error("Table `instance_encounters` has an invalid spell (entry {0}) linked to the encounter {1} ({2}), skipped!",
                                         creditEntry,
                                         entry,
                                         dungeonEncounter.Name[_worldManager.DefaultDbcLocale]);

                        continue;
                    }

                    break;

                default:
                    Log.Logger.Error("Table `instance_encounters` has an invalid credit type ({0}) for encounter {1} ({2}), skipped!",
                                     creditType,
                                     entry,
                                     dungeonEncounter.Name[_worldManager.DefaultDbcLocale]);

                    continue;
            }

            if (dungeonEncounter.DifficultyID == 0)
            {
                foreach (var difficulty in _cliDB.DifficultyStorage.Values)
                    if (_db2Manager.GetMapDifficultyData((uint)dungeonEncounter.MapID, (Difficulty)difficulty.Id) != null)
                        _dungeonEncounterStorage.Add(MathFunctions.MakePair64((uint)dungeonEncounter.MapID, difficulty.Id), new DungeonEncounter(dungeonEncounter, creditType, creditEntry, lastEncounterDungeon));
            }
            else
                _dungeonEncounterStorage.Add(MathFunctions.MakePair64((uint)dungeonEncounter.MapID, (uint)dungeonEncounter.DifficultyID), new DungeonEncounter(dungeonEncounter, creditType, creditEntry, lastEncounterDungeon));

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
            var spawnGroupTemplate = SpawnGroupDataCache.GetSpawnGroupData(spawnGroupId);

            if (spawnGroupTemplate == null || spawnGroupTemplate.Flags.HasAnyFlag(SpawnGroupFlags.System))
            {
                if (_configuration.GetDefaultValue("load:autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM instance_spawn_groups WHERE instanceMapId = {instanceMapId} AND spawnGroupId = {spawnGroupId}");
                else
                    Log.Logger.Error($"Invalid spawn group {spawnGroupId} specified for instance {instanceMapId}. Skipped.");

                continue;
            }

            if (spawnGroupTemplate.MapId != instanceMapId)
            {
                if (_configuration.GetDefaultValue("load:autoclean", false))
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

            byte allStates = (1 << (int)EncounterState.ToBeDecided) - 1;
            var states = result.Read<byte>(2);

            if ((states & ~allStates) != 0)
            {
                info.BossStates = (byte)(states & allStates);
                Log.Logger.Error($"Instance spawn group ({instanceMapId},{spawnGroupId}) had invalid boss state mask {states} - truncated to {info.BossStates}.");
            }
            else
                info.BossStates = states;

            var flags = (InstanceSpawnGroupFlags)result.Read<byte>(4);

            if ((flags & ~InstanceSpawnGroupFlags.All) != 0)
            {
                info.Flags = flags & InstanceSpawnGroupFlags.All;
                Log.Logger.Error($"Instance spawn group ({instanceMapId},{spawnGroupId}) had invalid flags {flags} - truncated to {info.Flags}.");
            }
            else
                info.Flags = flags;

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

    //Items

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

            if (CreatureTemplateCache.GetCreatureTemplate(senderEntry) == null)
            {
                Log.Logger.Error("Table `mail_level_reward` have not existed sender creature entry ({0}) for level {1} that invalid not include any player races, ignoring.", senderEntry, level);

                continue;
            }

            _mailLevelRewardStorage.Add(level, new MailLevelReward(raceMask, mailTemplateId, senderEntry));

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} level dependent mail rewards in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
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

            var probabilitySum = npcText.Data.Aggregate(0f, (sum, data) => sum + data.Probability);

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

            if (CreatureTemplateCache.GetCreatureTemplate(creatureid) == null)
            {
                if (_configuration.GetDefaultValue("load:autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM pet_levelstats WHERE creature_entry = {creatureid}");
                else
                    Log.Logger.Error("Wrong creature id {0} in `pet_levelstats` table, ignoring.", creatureid);

                continue;
            }

            var currentlevel = result.Read<uint>(1);

            if (currentlevel > _configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel))
            {
                if (currentlevel > SharedConst.StrongMaxLevel) // hardcoded level maximum
                    Log.Logger.Error("Wrong (> {0}) level {1} in `pet_levelstats` table, ignoring.", SharedConst.StrongMaxLevel, currentlevel);
                else
                {
                    Log.Logger.Warning("Unused (> MaxPlayerLevel in worldserver.conf) level {0} in `pet_levelstats` table, ignoring.", currentlevel);
                    ++count; // make result loading percent "expected" correct in case disabled detail mode for example.
                }

                continue;
            }

            if (currentlevel < 1)
            {
                Log.Logger.Error("Wrong (<1) level {0} in `pet_levelstats` table, ignoring.", currentlevel);

                continue;
            }

            if (!_petInfoStore.TryGetValue(creatureid, out var pInfoMapEntry))
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
        foreach (var (creatureId, pInfo) in _petInfoStore)
        {
            // fatal error if no level 1 data
            if (pInfo == null || pInfo[0].Health == 0)
            {
                Log.Logger.Error("Creature {0} does not have pet stats data for Level 1!", creatureId);
                _worldManager.StopNow();
            }

            // fill level gaps
            for (byte level = 1; level < _configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel); ++level)
                if (pInfo?[level] != null && pInfo[level].Health == 0)
                {
                    Log.Logger.Error("Creature {0} has no data for Level {1} pet stats data, using data of Level {2}.", creatureId, level + 1, level);
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

        var result = _characterDatabase.Query("SELECT MAX(id) FROM character_pet");

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
            _phaseInfoById.Add(phase.Id, new PhaseInfoStruct(phase.Id, _db2Manager));

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

                if (reward.PackageId != 0 && _db2Manager.GetQuestPackageItems((uint)reward.PackageId) == null)
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

                if (_itemTemplateCache.GetItemTemplate(itemId) == null)
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

                if (!_playerChoices.TryGetValue(choiceId, out var choice))
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

                if (!_playerChoices.TryGetValue(choiceId, out var choice))
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

                if (!_playerChoices.TryGetValue(choiceId, out var choice))
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

                if (_itemTemplateCache.GetItemTemplate(itemId) == null)
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

                if (!_playerChoices.TryGetValue(choiceId, out var choice))
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

                if (!_playerChoiceLocales.TryGetValue(choiceId, out var playerChoiceLocale))
                {
                    if (_configuration.GetDefaultValue("load:autoclean", false))
                        _worldDatabase.Execute($"DELETE FROM playerchoice_response_locale WHERE ChoiceID = {choiceId} AND ResponseID = {responseId} AND locale = \"{localeName}\"");
                    else
                        Log.Logger.Error($"Table `playerchoice_locale` references non-existing ChoiceId: {choiceId} for ResponseId {responseId} locale {localeName}, skipped");

                    continue;
                }

                var playerChoice = GetPlayerChoice(choiceId);

                if (playerChoice.GetResponse(responseId) == null)
                {
                    if (_configuration.GetDefaultValue("load:autoclean", false))
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
                    if (_configuration.GetDefaultValue("load:autoclean", false))
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
                if (!_gridDefines.IsValidMapCoord(mapId, positionX, positionY, positionZ, orientation))
                {
                    Log.Logger.Error($"Wrong home position for class {currentclass} race {currentrace} pair in `playercreateinfo` table, ignoring.");

                    continue;
                }

                if (_cliDB.MapStorage.LookupByKey(mapId).Instanceable())
                {
                    Log.Logger.Error($"Home position in instanceable map for class {currentclass} race {currentrace} pair in `playercreateinfo` table, ignoring.");

                    continue;
                }

                if (_db2Manager.GetChrModel((Race)currentrace, Gender.Male) == null)
                {
                    Log.Logger.Error($"Missing male model for race {currentrace}, ignoring.");

                    continue;
                }

                if (_db2Manager.GetChrModel((Race)currentrace, Gender.Female) == null)
                {
                    Log.Logger.Error($"Missing female model for race {currentrace}, ignoring.");

                    continue;
                }

                PlayerInfo info = new(_configuration);
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

                    if (info.CreatePositionNpe is { TransportGuid: { } } && _transportManager.GetTransportSpawn(info.CreatePositionNpe.Value.TransportGuid.Value) == null)
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
                var itemTemplate = _itemTemplateCache.GetItemTemplate(characterLoadoutItem.ItemID);

                if (itemTemplate != null)
                    itemsByCharacterLoadout.Add(characterLoadoutItem.CharacterLoadoutID, itemTemplate);
            }

            foreach (var characterLoadout in _cliDB.CharacterLoadoutStorage.Values)
            {
                if (!characterLoadout.IsForNewCharacter())
                    continue;

                if (itemsByCharacterLoadout.TryGetValue(characterLoadout.Id, out var items))
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
                Log.Logger.Information("Loaded 0 custom player create items. DB table `playercreateinfo_item` is empty.");
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

                    if (_itemTemplateCache.GetItemTemplate(itemid).Id == 0)
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
                        PlayerCreateInfoAddItemHelper(currentrace, currentclass, itemid, amount);

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
                Log.Logger.Information("Loaded 0 player create custom spells. DB table `playercreateinfo_spell_custom` is empty.");
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
                Log.Logger.Information("Loaded 0 player create cast spells. DB table `playercreateinfo_cast_spell` is empty.");
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
                Log.Logger.Information("Loaded 0 player create actions. DB table `playercreateinfo_action` is empty.");
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
                _worldManager.StopNow();

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
                _worldManager.StopNow();

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

                for (PlayerClass @class = 0; @class < PlayerClass.Max; ++@class)
                {
                    // skip non existed classes
                    if (_cliDB.ChrClassesStorage.LookupByKey(@class) == null)
                        continue;

                    if (!PlayerInfos.TryGetValue(race, @class, out var playerInfo))
                        continue;

                    if (_configuration.GetDefaultValue("character:EnforceRaceAndClassExpations", true))
                    {
                        // skip expansion races if not playing with expansion
                        if (_configuration.GetDefaultValue("Expansion", (int)Expansion.Dragonflight) < (int)Expansion.BurningCrusade && race is Race.BloodElf or Race.Draenei)
                            continue;

                        // skip expansion classes if not playing with expansion
                        if (_configuration.GetDefaultValue("Expansion", (int)Expansion.Dragonflight) < (int)Expansion.WrathOfTheLichKing && @class == PlayerClass.Deathknight)
                            continue;

                        if (_configuration.GetDefaultValue("Expansion", (int)Expansion.Dragonflight) < (int)Expansion.MistsOfPandaria && race is Race.PandarenNeutral or Race.PandarenHorde or Race.PandarenAlliance)
                            continue;

                        if (_configuration.GetDefaultValue("Expansion", (int)Expansion.Dragonflight) < (int)Expansion.Legion && @class == PlayerClass.DemonHunter)
                            continue;

                        if (_configuration.GetDefaultValue("Expansion", (int)Expansion.Dragonflight) < (int)Expansion.Dragonflight && @class == PlayerClass.Evoker)
                            continue;
                    }

                    // fatal error if no level 1 data
                    if (playerInfo.LevelInfo[0].Stats[0] == 0)
                    {
                        Log.Logger.Error("Race {0} Class {1} Level 1 does not have stats data!", race, @class);
                        Environment.Exit(1);

                        return;
                    }

                    // fill level gaps
                    for (var level = 1; level < _configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel); ++level)
                        if (playerInfo.LevelInfo[level].Stats[0] == 0)
                        {
                            Log.Logger.Error("Race {0} Class {1} Level {2} does not have stats data. Using stats data of level {3}.", race, @class, level + 1, level);
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
            _playerXPperLevel = new uint[_cliDB.XpGameTable.TableRowCount + 1];

            //                                          0      1
            var result = _worldDatabase.Query("SELECT Level, Experience FROM player_xp_for_level");

            // load the DBC's levels at first...
            for (uint level = 1; level < _cliDB.XpGameTable.TableRowCount; ++level)
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
                            Log.Logger.Error("Wrong (> {0}) level {1} in `player_xp_for_level` table, ignoring.", 255, currentlevel);
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

            PointOfInterest poi = new()
            {
                Id = id,
                Pos = new Vector3(result.Read<float>(1), result.Read<float>(2), result.Read<float>(3)),
                Icon = result.Read<uint>(4),
                Flags = result.Read<uint>(5),
                Importance = result.Read<uint>(6),
                Name = result.Read<string>(7),
                WmoGroupId = result.Read<uint>(8)
            };

            if (!_gridDefines.IsValidMapCoord(poi.Pos.X, poi.Pos.Y, poi.Pos.Z))
            {
                Log.Logger.Error($"Table `points_of_interest` (ID: {id}) have invalid coordinates (PositionX: {poi.Pos.X} PositionY: {poi.Pos.Y} PositionZ: {poi.Pos.Z}), ignored.");

                continue;
            }

            _pointsOfInterestStorage[id] = poi;

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

            var triggerID = result.Read<uint>(0);
            var questID = result.Read<uint>(1);

            if (!_cliDB.AreaTriggerStorage.TryGetValue(triggerID, out _))
            {
                Log.Logger.Error("Area trigger (ID:{0}) does not exist in `AreaTrigger.dbc`.", triggerID);

                continue;
            }

            var quest = GetQuestTemplate(questID);

            if (quest == null)
            {
                Log.Logger.Error("Table `areatrigger_involvedrelation` has record (id: {0}) for not existing quest {1}", triggerID, questID);

                continue;
            }

            if (!quest.HasSpecialFlag(QuestSpecialFlags.ExplorationOrEvent))
            {
                Log.Logger.Error("Table `areatrigger_involvedrelation` has record (id: {0}) for not quest {1}, but quest not have Id QUEST_SPECIAL_FLAGS_EXPLORATION_OR_EVENT. Trigger or quest flags must be fixed, quest modified to require objective.", triggerID, questID);

                // this will prevent quest completing without objective
                quest.SetSpecialFlag(QuestSpecialFlags.ExplorationOrEvent);

                // continue; - quest modified to required objective and trigger can be allowed.
            }

            _questAreaTriggerStorage.Add(triggerID, questID);
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
                    if (CreatureTemplateCache.GetCreatureTemplate(id) == null)
                    {
                        Log.Logger.Error($"Table `quest_greeting_locale`: creature template entry {id} does not exist.");

                        continue;
                    }

                    break;

                case 1: // GameObject
                    if (GameObjectTemplateCache.GetGameObjectTemplate(id) == null)
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
                    if (CreatureTemplateCache.GetCreatureTemplate(id) == null)
                    {
                        Log.Logger.Error("Table `quest_greeting`: creature template entry {0} does not exist.", id);

                        continue;
                    }

                    break;

                case 1: // GameObject
                    if (GameObjectTemplateCache.GetGameObjectTemplate(id) == null)
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
                var idx1 = pointsResult.Read<int>(1);
                var x = pointsResult.Read<int>(2);
                var y = pointsResult.Read<int>(3);
                var z = pointsResult.Read<int>(4);

                if (!allPoints.ContainsKey(questId))
                    allPoints[questId] = new MultiMap<int, QuestPOIBlobPoint>();

                allPoints[questId].Add(idx1, new QuestPOIBlobPoint(x, y, z));
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
                if (_configuration.GetDefaultValue("load:autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM quest_poi WHERE QuestID = {questID}");
                else
                    Log.Logger.Error($"`quest_poi` quest id ({questID}) Idx1 ({idx1}) does not exist in `quest_template`");

            if (allPoints.TryGetValue(questID, out var blobs))
                if (blobs.TryGetValue(idx1, out var points))
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
        QuestTemplates.Clear();
        QuestTemplatesAutoPush.Clear();
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
            var newQuest = _classFactory.ResolveWithPositionalParameters<Quest.Quest>(result.GetFields());
            QuestTemplates[newQuest.Id] = newQuest;

            if (newQuest.IsAutoPush)
                QuestTemplatesAutoPush.Add(newQuest);
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

                if (QuestTemplates.TryGetValue(questId, out var quest))
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

                if (QuestTemplates.TryGetValue(questId, out var quest))
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

                if (QuestTemplates.TryGetValue(questId, out var quest))
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

                if (QuestTemplates.TryGetValue(questId, out var quest))
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

                if (QuestTemplates.TryGetValue(questId, out var quest))
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

                if (QuestTemplates.TryGetValue(questId, out var quest))
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

                if (QuestTemplates.TryGetValue(questId, out var quest))
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

                if (QuestTemplates.TryGetValue(questId, out var quest))
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
                var quest = QuestTemplates.LookupByKey(questId);

                quest?.LoadQuestObjectiveVisualEffect(result.GetFields());
            } while (result.NextRow());

        Dictionary<uint, uint> usedMailTemplates = new();

        // Post processing
        foreach (var qinfo in QuestTemplates.Values)
        {
            // skip post-loading checks for disabled quests
            if (_disableManager.IsDisabledFor(DisableType.Quest, qinfo.Id, null))
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
                if (!_cliDB.QuestSortStorage.TryGetValue((uint)-qinfo.QuestSortID, out _))
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
                if (qinfo.RequiredSkillPoints > _worldManager.ConfigMaxSkillValue)
                    Log.Logger.Error("QuestId {0} has `RequiredSkillPoints` = {1} but max possible skill is {2}, quest can't be done.",
                                     qinfo.Id,
                                     qinfo.RequiredSkillPoints,
                                     _worldManager.ConfigMaxSkillValue);
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
                if (_itemTemplateCache.GetItemTemplate(qinfo.SourceItemId) == null)
                {
                    Log.Logger.Error("QuestId {0} has `SourceItemId` = {1} but item with entry {2} does not exist, quest can't be done.",
                                     qinfo.Id,
                                     qinfo.SourceItemId,
                                     qinfo.SourceItemId);

                    qinfo.SourceItemId = 0; // quest can't be done for this requirement
                }
                else if (qinfo.SourceItemIdCount == 0)
                {
                    if (_configuration.GetDefaultValue("load:autoclean", false))
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
                var spellInfo = _spellManager.GetSpellInfo(qinfo.SourceSpellID);

                if (spellInfo == null)
                {
                    Log.Logger.Error("QuestId {0} has `SourceSpellid` = {1} but spell {1} doesn't exist, quest can't be done.",
                                     qinfo.Id,
                                     qinfo.SourceSpellID);

                    qinfo.SourceSpellID = 0; // quest can't be done for this requirement
                }
                else if (!_spellManager.IsSpellValid(spellInfo))
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
                    }

                switch (obj.Type)
                {
                    case QuestObjectiveType.Item:
                        if (_itemTemplateCache.GetItemTemplate((uint)obj.ObjectID) == null)
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error($"QuestId {qinfo.Id} objective {obj.Id} has non existing item entry {obj.ObjectID}, quest can't be done.");

                        break;

                    case QuestObjectiveType.Monster:
                        if (CreatureTemplateCache.GetCreatureTemplate((uint)obj.ObjectID) == null)
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error($"QuestId {qinfo.Id} objective {obj.Id} has non existing creature entry {obj.ObjectID}, quest can't be done.");

                        break;

                    case QuestObjectiveType.GameObject:
                        if (GameObjectTemplateCache.GetGameObjectTemplate((uint)obj.ObjectID) == null)
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error($"QuestId {qinfo.Id} objective {obj.Id} has non existing gameobject entry {obj.ObjectID}, quest can't be done.");

                        break;

                    case QuestObjectiveType.TalkTo:
                        if (CreatureTemplateCache.GetCreatureTemplate((uint)obj.ObjectID) == null)
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error($"QuestId {qinfo.Id} objective {obj.Id} has non existing creature entry {obj.ObjectID}, quest can't be done.");

                        break;

                    case QuestObjectiveType.MinReputation:
                    case QuestObjectiveType.MaxReputation:
                    case QuestObjectiveType.IncreaseReputation:
                        if (!_cliDB.FactionStorage.ContainsKey((uint)obj.ObjectID))
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error("QuestId {0} objective {1} has non existing faction id {2}", qinfo.Id, obj.Id, obj.ObjectID);

                        break;

                    case QuestObjectiveType.PlayerKills:
                        if (obj.Amount <= 0)
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error("QuestId {0} objective {1} has invalid player kills count {2}", qinfo.Id, obj.Id, obj.Amount);

                        break;

                    case QuestObjectiveType.Currency:
                    case QuestObjectiveType.HaveCurrency:
                    case QuestObjectiveType.ObtainCurrency:
                        if (!_cliDB.CurrencyTypesStorage.ContainsKey((uint)obj.ObjectID))
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error("QuestId {0} objective {1} has non existing currency {2}", qinfo.Id, obj.Id, obj.ObjectID);

                        if (obj.Amount <= 0)
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error("QuestId {0} objective {1} has invalid currency amount {2}", qinfo.Id, obj.Id, obj.Amount);

                        break;

                    case QuestObjectiveType.LearnSpell:
                        if (!_spellManager.HasSpellInfo((uint)obj.ObjectID))
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error("QuestId {0} objective {1} has non existing spell id {2}", qinfo.Id, obj.Id, obj.ObjectID);

                        break;

                    case QuestObjectiveType.WinPetBattleAgainstNpc:
                        if (obj.ObjectID != 0 && CreatureTemplateCache.GetCreatureTemplate((uint)obj.ObjectID) == null)
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error("QuestId {0} objective {1} has non existing creature entry {2}, quest can't be done.", qinfo.Id, obj.Id, obj.ObjectID);

                        break;

                    case QuestObjectiveType.DefeatBattlePet:
                        if (!_cliDB.BattlePetSpeciesStorage.ContainsKey((uint)obj.ObjectID))
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error("QuestId {0} objective {1} has non existing battlepet species id {2}", qinfo.Id, obj.Id, obj.ObjectID);

                        break;

                    case QuestObjectiveType.CriteriaTree:
                        if (!_cliDB.CriteriaTreeStorage.ContainsKey((uint)obj.ObjectID))
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error("QuestId {0} objective {1} has non existing criteria tree id {2}", qinfo.Id, obj.Id, obj.ObjectID);

                        break;

                    case QuestObjectiveType.AreaTrigger:
                        if (!_cliDB.AreaTriggerStorage.ContainsKey((uint)obj.ObjectID) && obj.ObjectID != -1)
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error("QuestId {0} objective {1} has non existing AreaTrigger.db2 id {2}", qinfo.Id, obj.Id, obj.ObjectID);

                        break;

                    case QuestObjectiveType.AreaTriggerEnter:
                    case QuestObjectiveType.AreaTriggerExit:
                        if (SpawnDataCacheRouter._areaTriggerDataStorage.GetAreaTriggerTemplate(new AreaTriggerId((uint)obj.ObjectID, false)) == null && SpawnDataCacheRouter._areaTriggerDataStorage.GetAreaTriggerTemplate(new AreaTriggerId((uint)obj.ObjectID, true)) != null)
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error("QuestId {0} objective {1} has non existing areatrigger id {2}", qinfo.Id, obj.Id, obj.ObjectID);

                        break;

                    case QuestObjectiveType.Money:
                    case QuestObjectiveType.WinPvpPetBattles:
                    case QuestObjectiveType.ProgressBar:
                        break;

                    default:
                        if (_configuration.GetDefaultValue("load:autoclean", false))
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
                    if (_itemTemplateCache.GetItemTemplate(id) == null)
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
                            if (_itemTemplateCache.GetItemTemplate(id) == null)
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
                    Log.Logger.Error($"QuestId {qinfo.Id} has `RewardChoiceItemId{j + 1}` = 0 but `RewardChoiceItemCount{j + 1}` = {qinfo.RewardChoiceItemCount[j]}.");
                // no changes, quest ignore this data
            }

            for (var j = 0; j < SharedConst.QuestRewardItemCount; ++j)
            {
                var id = qinfo.RewardItemId[j];

                if (id != 0)
                {
                    if (_itemTemplateCache.GetItemTemplate(id) == null)
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
                    Log.Logger.Error("QuestId {0} has `RewardItemId{1}` = 0 but `RewardItemIdCount{2}` = {3}.",
                                     qinfo.Id,
                                     j + 1,
                                     j + 1,
                                     qinfo.RewardItemCount[j]);
                // no changes, quest ignore this data
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
                    Log.Logger.Error("QuestId {0} has `RewardFactionId{1}` = 0 but `RewardFactionValueIdOverride{2}` = {3}.",
                                     qinfo.Id,
                                     j + 1,
                                     j + 1,
                                     qinfo.RewardFactionOverride[j]);

            // no changes, quest ignore this data
            if (qinfo.RewardSpell > 0)
            {
                var spellInfo = _spellManager.GetSpellInfo(qinfo.RewardSpell);

                if (spellInfo == null)
                {
                    Log.Logger.Error("QuestId {0} has `RewardSpellCast` = {1} but spell {2} does not exist, quest will not have a spell reward.",
                                     qinfo.Id,
                                     qinfo.RewardSpell,
                                     qinfo.RewardSpell);

                    qinfo.RewardSpell = 0; // no spell will be casted on player
                }
                else if (!_spellManager.IsSpellValid(spellInfo))
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
                    usedMailTemplates[qinfo.RewardMailTemplateId] = qinfo.Id;
            }

            if (qinfo.NextQuestInChain != 0)
                if (!QuestTemplates.ContainsKey(qinfo.NextQuestInChain))
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
                if (qinfo.RewardSkillPoints > _worldManager.ConfigMaxSkillValue)
                    Log.Logger.Error("QuestId {0} has `RewardSkillPoints` = {1} but max possible skill is {2}, quest can't be done.",
                                     qinfo.Id,
                                     qinfo.RewardSkillPoints,
                                     _worldManager.ConfigMaxSkillValue);

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
                if (!QuestTemplates.TryGetValue(prevQuestId, out var prevQuestItr))
                    Log.Logger.Error($"QuestId {qinfo.Id} has PrevQuestId {prevQuestId}, but no such quest");
                else if (prevQuestItr.BreadcrumbForQuestId != 0)
                    Log.Logger.Error($"QuestId {qinfo.Id} should not be unlocked by breadcrumb quest {prevQuestId}");
                else if (qinfo.PrevQuestId > 0)
                    qinfo.DependentPreviousQuests.Add(prevQuestId);
            }

            if (qinfo.NextQuestId != 0)
            {
                if (!QuestTemplates.TryGetValue(qinfo.NextQuestId, out var nextquest))
                    Log.Logger.Error("QuestId {0} has NextQuestId {1}, but no such quest", qinfo.Id, qinfo.NextQuestId);
                else
                    nextquest.DependentPreviousQuests.Add(qinfo.Id);
            }

            var breadcrumbForQuestId = (uint)Math.Abs(qinfo.BreadcrumbForQuestId);

            if (breadcrumbForQuestId != 0)
            {
                if (!QuestTemplates.ContainsKey(breadcrumbForQuestId))
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

        foreach (var questPair in QuestTemplates)
        {
            // skip post-loading checks for disabled quests
            if (_disableManager.IsDisabledFor(DisableType.Quest, questPair.Key, null))
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
            var spellInfo = _spellManager.GetSpellInfo(spellNameEntry.Id);

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

        Log.Logger.Information("Loaded {0} quests definitions in {1} ms", QuestTemplates.Count, Time.GetMSTimeDiffToNow(oldMSTime));
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
        RaceUnlockRequirements.Clear();

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

                if (!_cliDB.ChrRacesStorage.TryGetValue(raceID, out _))
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

                RaceUnlockRequirements[raceID] = raceUnlockRequirement;

                ++count;
            } while (result.NextRow());

            Log.Logger.Information("Loaded {0} race expansion requirements in {1} ms.", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }
        else
            Log.Logger.Information("Loaded 0 race expansion requirements. DB table `race_expansion_requirement` is empty.");

        oldMSTime = Time.MSTime;
        ClassExpansionRequirements.Clear();

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

                if (!_cliDB.ChrClassesStorage.TryGetValue(classID, out _))
                {
                    Log.Logger.Error($"Class {classID} (race {raceID}) defined in `class_expansion_requirement` does not exists, skipped.");

                    continue;
                }

                if (!_cliDB.ChrRacesStorage.TryGetValue(raceID, out _))
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

                foreach (var @class in race.Value)
                {
                    ClassAvailability classAvailability = new()
                    {
                        ClassID = @class.Key,
                        ActiveExpansionLevel = @class.Value.Item1,
                        AccountExpansionLevel = @class.Value.Item2,
                        MinActiveExpansionLevel = minRequirementForClass[@class.Key]
                    };

                    raceClassAvailability.Classes.Add(classAvailability);
                }

                ClassExpansionRequirements.Add(raceClassAvailability);
            }

            Log.Logger.Information($"Loaded {count} class expansion requirements in {Time.GetMSTimeDiffToNow(oldMSTime)} ms.");
        }
        else
            Log.Logger.Information("Loaded 0 class expansion requirements. DB table `class_expansion_requirement` is empty.");
    }

    public void LoadRealmNames()
    {
        var oldMSTime = Time.MSTime;
        _realmNameStorage.Clear();

        //                                         0   1
        var result = _loginDatabase.Query("SELECT id, name FROM `realmlist`");

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
            var creatureID = result.Read<uint>(0);

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

            if (CreatureTemplateCache.GetCreatureTemplate(creatureID) == null)
            {
                Log.Logger.Error("Table `creature_onkill_reputation` have data for not existed creature entry ({0}), skipped", creatureID);

                continue;
            }

            if (repOnKill.RepFaction1 != 0)
                if (!_cliDB.FactionStorage.TryGetValue(repOnKill.RepFaction1, out _))
                {
                    Log.Logger.Error("Faction (faction.dbc) {0} does not exist but is used in `creature_onkill_reputation`", repOnKill.RepFaction1);

                    continue;
                }

            if (repOnKill.RepFaction2 != 0)
                if (!_cliDB.FactionStorage.TryGetValue(repOnKill.RepFaction2, out _))
                {
                    Log.Logger.Error("Faction (faction.dbc) {0} does not exist but is used in `creature_onkill_reputation`", repOnKill.RepFaction2);

                    continue;
                }

            _repOnKillStorage[creatureID] = repOnKill;

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

            if (!_cliDB.FactionStorage.TryGetValue(factionId, out _))
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

            if (!_cliDB.FactionStorage.TryGetValue(factionId, out var factionEntry))
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

        var result = _characterDatabase.Query("SELECT name FROM reserved_name");

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
                ScriptId = _scriptManager.GetScriptId(result.Read<string>(4))
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

    public void LoadSpellScriptNames()
    {
        var oldMSTime = Time.MSTime;

        _scriptManager.SpellScriptsStorage.Clear(); // need for reload case

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
        foreach (var script in _scriptManager.SpellScripts)
        {
            var spellId = script.Key & 0x00FFFFFF;
            var spellInfo = _spellManager.GetSpellInfo(spellId);

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

            var triggerID = result.Read<uint>(0);

            if (!_cliDB.AreaTriggerStorage.TryGetValue(triggerID, out _))
            {
                Log.Logger.Error("Area trigger (ID:{0}) does not exist in `AreaTrigger.dbc`.", triggerID);

                continue;
            }

            _tavernAreaTriggerStorage.Add(triggerID);
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
                    if (CreatureTemplateCache.GetCreatureTemplate(summonerId) == null)
                    {
                        Log.Logger.Error("Table `creature_summon_groups` has summoner with non existing entry {0} for creature summoner type, skipped.", summonerId);

                        continue;
                    }

                    break;

                case SummonerType.GameObject:
                    if (GameObjectTemplateCache.GetGameObjectTemplate(summonerId) == null)
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
                Entry = result.Read<uint>(3)
            };

            if (CreatureTemplateCache.GetCreatureTemplate(data.Entry) == null)
            {
                Log.Logger.Error("Table `creature_summon_groups` has creature in group [Summoner ID: {0}, Summoner Type: {1}, Group ID: {2}] with non existing creature entry {3}, skipped.",
                                 summonerId,
                                 summonerType,
                                 group,
                                 data.Entry);

                continue;
            }

            var posX = result.Read<float>(4);
            var posY = result.Read<float>(5);
            var posZ = result.Read<float>(6);
            var orientation = result.Read<float>(7);

            data.Pos = new Position(posX, posY, posZ, orientation);

            data.Type = (TempSummonType)result.Read<byte>(8);

            if (data.Type > TempSummonType.ManualDespawn)
            {
                Log.Logger.Error("Table `creature_summon_groups` has unhandled temp summon type {0} in group [Summoner ID: {1}, Summoner Type: {2}, Group ID: {3}] for creature entry {4}, skipped.",
                                 data.Type,
                                 summonerId,
                                 summonerType,
                                 group,
                                 data.Entry);

                continue;
            }

            data.Time = result.Read<uint>(9);

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

                var spellInfo = _spellManager.GetSpellInfo(spell.SpellId);

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

                    if (requiredSpell != 0 && !_spellManager.HasSpellInfo(requiredSpell))
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

                if (spellsByTrainer.TryGetValue(trainerId, out var spellList))
                {
                    spells = spellList;
                    spellsByTrainer.Remove(trainerId);
                }

                _trainers.Add(trainerId, new Trainer(trainerId, trainerType, greeting, spells, _classFactory.Resolve<ConditionManager>(), _classFactory.Resolve<BattlePetData>(), _classFactory.Resolve<SpellManager>()));
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

                if (_trainers.TryGetValue(trainerId, out var trainer))
                    trainer.AddGreetingLocale(locale, trainerLocalesResult.Read<string>(2));
                else
                    Log.Logger.Error($"Table `trainer_locale` references non-existing trainer (TrainerId: {trainerId}) for locale {localeName}, ignoring");
            } while (trainerLocalesResult.NextRow());

        Log.Logger.Information($"Loaded {_trainers.Count} Trainers in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    //Vehicles

    //Load WP Scripts
    public void LoadWaypointScripts()
    {
        LoadScripts(ScriptsType.Waypoint);

        List<uint> actionSet = new();

        foreach (var script in _scriptManager.WaypointScripts)
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

    //Methods
    public bool NormalizePlayerName(ref string name)
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
        var spellInfo = _spellManager.GetSpellInfo(spellId);

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
                _scriptManager.SpellScriptsStorage.AddUnique(spellInfo.Id, _scriptManager.GetScriptId(scriptName));
                spellInfo = spellInfo.NextRankSpell;
            }
        }
        else
        {
            if (spellInfo.IsRanked)
                Log.Logger.Debug("Scriptname: `{0}` spell (Id: {1}) is ranked spell. Perhaps not all ranks are assigned to this script.", scriptName, spellId);

            _scriptManager.SpellScriptsStorage.AddUnique(spellInfo.Id, _scriptManager.GetScriptId(scriptName));
        }

        return true;
    }

    public void RemoveCreatureFromGrid(CreatureData data)
    {
        MapObjectCache.RemoveSpawnDataFromGrid(data);
    }

    public void RemoveGameObjectFromGrid(GameObjectData data)
    {
        MapObjectCache.RemoveSpawnDataFromGrid(data);
    }

    public void RemoveGraveYardLink(uint id, uint zoneId, TeamFaction team, bool persist = false)
    {
        if (GraveYardStorage.TryGetValue(zoneId, out var range))
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
        if (!persist)
            return;

        var stmt = _worldDatabase.GetPreparedStatement(WorldStatements.DEL_GRAVEYARD_ZONE);

        stmt.AddValue(0, id);
        stmt.AddValue(1, zoneId);
        stmt.AddValue(2, (uint)team);

        _worldDatabase.Execute(stmt);
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
            stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_EMPTY_EXPIRED_MAIL);
            stmt.AddValue(0, curTime);
            _characterDatabase.Execute(stmt);
        }

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_EXPIRED_MAIL);
        stmt.AddValue(0, curTime);
        var result = _characterDatabase.Query(stmt);

        if (result.IsEmpty())
        {
            Log.Logger.Information("No expired mails found.");

            return; // any mails need to be returned or deleted
        }

        MultiMap<ulong, MailItemInfo> itemsCache = new();
        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_EXPIRED_MAIL_ITEMS);
        stmt.AddValue(0, curTime);
        var items = _characterDatabase.Query(stmt);

        if (!items.IsEmpty())
        {
            MailItemInfo item = new();

            do
            {
                item.ItemGUID = result.Read<uint>(0);
                item.ItemTemplate = result.Read<uint>(1);
                var mailId = result.Read<ulong>(2);
                itemsCache.Add(mailId, item);
            } while (items.NextRow());
        }

        uint deletedCount = 0;
        uint returnedCount = 0;

        do
        {
            var receiver = result.Read<ulong>(3);

            if (serverUp && _objectAccessor.FindConnectedPlayer(ObjectGuid.Create(HighGuid.Player, receiver)) != null)
                continue;

            Mail m = new()
            {
                MessageID = result.Read<ulong>(0),
                MessageType = (MailMessageType)result.Read<byte>(1),
                Sender = result.Read<uint>(2),
                Receiver = receiver
            };

            var hasItems = result.Read<bool>(4);
            m.ExpireTime = result.Read<long>(5);
            m.DeliverTime = 0;
            m.Cod = result.Read<ulong>(6);
            m.CheckMask = (MailCheckMask)result.Read<byte>(7);
            m.MailTemplateId = result.Read<ushort>(8);

            // Delete or return mail
            if (hasItems)
            {
                // read items from cache
                m.Items = itemsCache[m.MessageID];
                // Extensions.Swap(ref m.Items, ref temp);

                // if it is mail from non-player, or if it's already return mail, it shouldn't be returned, but deleted
                if (m.MessageType != MailMessageType.Normal || (m.CheckMask.HasAnyFlag(MailCheckMask.CodPayment | MailCheckMask.Returned)))
                {
                    // mail open and then not returned
                    foreach (var itemInfo in m.Items)
                    {
                        _itemFactory.DeleteFromDB(null, itemInfo.ItemGUID);
                        _azeriteItemFactory.DeleteFromDB(null, itemInfo.ItemGUID);
                        _azeriteEmpoweredItemFactory.DeleteFromDB(null, itemInfo.ItemGUID);
                    }

                    stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM_BY_ID);
                    stmt.AddValue(0, m.MessageID);
                    _characterDatabase.Execute(stmt);
                }
                else
                {
                    // Mail will be returned
                    stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_MAIL_RETURNED);
                    stmt.AddValue(0, m.Receiver);
                    stmt.AddValue(1, m.Sender);
                    stmt.AddValue(2, curTime + 30 * Time.DAY);
                    stmt.AddValue(3, curTime);
                    stmt.AddValue(4, (byte)MailCheckMask.Returned);
                    stmt.AddValue(5, m.MessageID);
                    _characterDatabase.Execute(stmt);

                    foreach (var itemInfo in m.Items)
                    {
                        // Update receiver in mail items for its proper delivery, and in instance_item for avoid lost item at sender delete
                        stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_MAIL_ITEM_RECEIVER);
                        stmt.AddValue(0, m.Sender);
                        stmt.AddValue(1, itemInfo.ItemGUID);
                        _characterDatabase.Execute(stmt);

                        stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_ITEM_OWNER);
                        stmt.AddValue(0, m.Sender);
                        stmt.AddValue(1, itemInfo.ItemGUID);
                        _characterDatabase.Execute(stmt);
                    }

                    ++returnedCount;

                    continue;
                }
            }

            stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_MAIL_BY_ID);
            stmt.AddValue(0, m.MessageID);
            _characterDatabase.Execute(stmt);
            ++deletedCount;
        } while (result.NextRow());

        Log.Logger.Information("Processed {0} expired mails: {1} deleted and {2} returned in {3} ms", deletedCount + returnedCount, deletedCount, returnedCount, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void SetHighestGuids()
    {
        var result = _characterDatabase.Query("SELECT MAX(guid) FROM characters");
        var playerGenerator = _objectGuidGeneratorFactory.GetGenerator(HighGuid.Player);
        var itemGenerator = _objectGuidGeneratorFactory.GetGenerator(HighGuid.Item);
        var transportGenerator = _objectGuidGeneratorFactory.GetGenerator(HighGuid.Transport);

        if (!result.IsEmpty())
            playerGenerator.Set(result.Read<ulong>(0) + 1);

        result = _characterDatabase.Query("SELECT MAX(guid) FROM item_instance");

        if (!result.IsEmpty())
            itemGenerator.Set(result.Read<ulong>(0) + 1);

        // Cleanup other tables from not existed guids ( >= hiItemGuid)
        _characterDatabase.Execute("DELETE FROM character_inventory WHERE item >= {0}", itemGenerator.GetNextAfterMaxUsed()); // One-time query
        _characterDatabase.Execute("DELETE FROM mail_items WHERE item_guid >= {0}", itemGenerator.GetNextAfterMaxUsed());     // One-time query

        _characterDatabase.Execute("DELETE a, ab, ai FROM auctionhouse a LEFT JOIN auction_bidders ab ON ab.auctionId = a.id LEFT JOIN auction_items ai ON ai.auctionId = a.id WHERE ai.itemGuid >= '{0}'",
                                   itemGenerator.GetNextAfterMaxUsed()); // One-time query

        _characterDatabase.Execute("DELETE FROM guild_bank_item WHERE item_guid >= {0}", itemGenerator.GetNextAfterMaxUsed()); // One-time query

        result = _worldDatabase.Query("SELECT MAX(guid) FROM transports");

        if (!result.IsEmpty())
            transportGenerator.Set(result.Read<ulong>(0) + 1);

        result = _characterDatabase.Query("SELECT MAX(id) FROM auctionhouse");

        if (!result.IsEmpty())
            _auctionId = result.Read<uint>(0) + 1;

        result = _characterDatabase.Query("SELECT MAX(id) FROM mail");

        if (!result.IsEmpty())
            _mailId = result.Read<ulong>(0) + 1;

        result = _characterDatabase.Query("SELECT MAX(arenateamid) FROM arena_team");

        if (!result.IsEmpty())
            _classFactory.Resolve<ArenaTeamManager>().SetNextArenaTeamId(result.Read<uint>(0) + 1);

        result = _characterDatabase.Query("SELECT MAX(maxguid) FROM ((SELECT MAX(setguid) AS maxguid FROM character_equipmentsets) UNION (SELECT MAX(setguid) AS maxguid FROM character_transmog_outfits)) allsets");

        if (!result.IsEmpty())
            _equipmentSetGuid = result.Read<ulong>(0) + 1;

        result = _characterDatabase.Query("SELECT MAX(guildId) FROM guild");

        if (!result.IsEmpty())
            _classFactory.Resolve<GuildManager>().SetNextGuildId(result.Read<uint>(0) + 1);

        result = _characterDatabase.Query("SELECT MAX(itemId) from character_void_storage");

        if (!result.IsEmpty())
            _voidItemId = result.Read<ulong>(0) + 1;

        result = _worldDatabase.Query("SELECT MAX(guid) FROM creature");

        if (!result.IsEmpty())
            _creatureSpawnId = result.Read<ulong>(0) + 1;

        result = _worldDatabase.Query("SELECT MAX(guid) FROM gameobject");

        if (!result.IsEmpty())
            _gameObjectSpawnId = result.Read<ulong>(0) + 1;
    }

    public bool TryGetQuestTemplate(uint questId, out Quest.Quest quest)
    {
        return QuestTemplates.TryGetValue(questId, out quest);
    }

    public void UnloadPhaseConditions()
    {
        foreach (var pair in _phaseInfoByArea.KeyValueList)
            pair.Value.Conditions.Clear();
    }

    public void ValidateSpellScripts()
    {
        var oldMSTime = Time.MSTime;

        if (_scriptManager.SpellScriptsStorage.Empty())
        {
            Log.Logger.Information("Validated 0 scripts.");

            return;
        }

        uint count = 0;

        _scriptManager.SpellScriptsStorage.RemoveIfMatching(script =>
        {
            var spellEntry = _spellManager.GetSpellInfo(script.Key);

            var spellScriptLoaders = _scriptManager.CreateSpellScriptLoaders(script.Key);

            foreach (var pair in spellScriptLoaders)
            {
                var spellScript = pair.Key.GetSpellScript();
                var valid = true;

                if (spellScript == null)
                {
                    Log.Logger.Error("Functions GetSpellScript() of script `{0}` do not return object - script skipped", _scriptManager.GetScriptName(pair.Value));
                    valid = false;
                }

                if (spellScript != null)
                {
                    spellScript._Init(pair.Key.GetName(), spellEntry.Id, _classFactory);
                    spellScript._Register();

                    if (!spellScript._Validate(spellEntry))
                        valid = false;
                }

                if (!valid)
                    return true;
            }

            var auraScriptLoaders = _scriptManager.CreateAuraScriptLoaders(script.Key);

            foreach (var pair in auraScriptLoaders)
            {
                var auraScript = pair.Key.GetAuraScript();
                var valid = true;

                if (auraScript == null)
                {
                    Log.Logger.Error("Functions GetAuraScript() of script `{0}` do not return object - script skipped", _scriptManager.GetScriptName(pair.Value));
                    valid = false;
                }

                if (auraScript != null)
                {
                    auraScript._Init(pair.Key.GetName(), spellEntry.Id, _classFactory);
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

    private PlayerLevelInfo BuildPlayerLevelInfo(Race race, PlayerClass @class, uint level)
    {
        // base data (last known level)
        var info = PlayerInfos[race][@class].LevelInfo[_configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel) - 1];

        for (var lvl = _configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel) - 1; lvl < level; ++lvl)
            switch (@class)
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


    private QuestRelationResult GetQuestRelationsFrom(MultiMap<uint, uint> map, uint key, bool onlyActive)
    {
        return new QuestRelationResult(map.LookupByKey(key), onlyActive, _questPoolManager);
    }

    private LanguageType GetRealmLanguageType(bool create)
    {
        return (RealmZones)_configuration.GetDefaultValue("RealmZone", (int)RealmZones.Development) switch
        {
            RealmZones.Unknown => // any language
                LanguageType.Any,
            RealmZones.Development => LanguageType.Any,
            RealmZones.TestServer => LanguageType.Any,
            RealmZones.QaServer => LanguageType.Any,
            RealmZones.UnitedStates => // extended-Latin
                LanguageType.ExtendenLatin,
            RealmZones.Oceanic => LanguageType.ExtendenLatin,
            RealmZones.LatinAmerica => LanguageType.ExtendenLatin,
            RealmZones.English => LanguageType.ExtendenLatin,
            RealmZones.German => LanguageType.ExtendenLatin,
            RealmZones.French => LanguageType.ExtendenLatin,
            RealmZones.Spanish => LanguageType.ExtendenLatin,
            RealmZones.Korea => // East-Asian
                LanguageType.EastAsia,
            RealmZones.Taiwan => LanguageType.EastAsia,
            RealmZones.China => LanguageType.EastAsia,
            RealmZones.Russian => // Cyrillic
                LanguageType.Cyrillic,
            _ => create ? LanguageType.BasicLatin : LanguageType.Any
        };
    }

    private bool IsCultureString(LanguageType culture, string str, bool numericOrSpace)
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

                    return wchar >= 0xFF01 && wchar <= 0xFFEE; // Halfwidth forms
            }
        }

        return false;
    }

    private bool IsValidString(string str, uint strictMask, bool numericOrSpace, bool create = false)
    {
        if (strictMask == 0) // any language, ignore realm
        {
            if (IsCultureString(LanguageType.BasicLatin, str, numericOrSpace))
                return true;

            if (IsCultureString(LanguageType.ExtendenLatin, str, numericOrSpace))
                return true;

            return IsCultureString(LanguageType.Cyrillic, str, numericOrSpace) || IsCultureString(LanguageType.EastAsia, str, numericOrSpace);
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

        if (!Convert.ToBoolean(strictMask & 0x1)) // basic Latin
            return false;

        return IsCultureString(LanguageType.BasicLatin, str, numericOrSpace);
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

        PhaseInfoStruct GetOrCreatePhaseIfMissing(uint phaseId)
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

            var phase = GetOrCreatePhaseIfMissing(phaseId);
            phase.Areas.Add(area);
            _phaseInfoByArea.Add(area, new PhaseAreaInfo(phase));

            ++count;
        } while (result.NextRow());

        foreach (var pair in _phaseInfoByArea.KeyValueList)
        {
            var parentAreaId = pair.Key;

            do
            {
                if (!_cliDB.AreaTableStorage.TryGetValue(parentAreaId, out var area))
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

            if (!QuestTemplates.ContainsKey(quest))
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

    private void LoadScripts(ScriptsType type)
    {
        var oldMSTime = Time.MSTime;

        var scripts = _scriptManager.GetScriptsMapByType(type);

        if (scripts == null)
            return;

        var tableName = GetScriptsTableNameByType(type);

        if (string.IsNullOrEmpty(tableName))
            return;

        if (_mapManager.IsScriptScheduled()) // function cannot be called when scripts are in use.
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
                        if (_configuration.GetDefaultValue("load:autoclean", false))
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
                        if (_configuration.GetDefaultValue("load:autoclean", false))
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
                        if (_configuration.GetDefaultValue("load:autoclean", false))
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
                        if (_configuration.GetDefaultValue("load:autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                        else
                            Log.Logger.Error("Table `{0}` has invalid map (Id: {1}) in SCRIPT_COMMAND_TELEPORT_TO for script id {2}",
                                             tableName,
                                             tmp.TeleportTo.MapID,
                                             tmp.id);

                        continue;
                    }

                    if (!_gridDefines.IsValidMapCoord(tmp.TeleportTo.DestX, tmp.TeleportTo.DestY, tmp.TeleportTo.DestZ, tmp.TeleportTo.Orientation))
                    {
                        if (_configuration.GetDefaultValue("load:autoclean", false))
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
                        if (_configuration.GetDefaultValue("load:autoclean", false))
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
                        if (_configuration.GetDefaultValue("load:autoclean", false))
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
                        if (_configuration.GetDefaultValue("load:autoclean", false))
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
                    if (CreatureTemplateCache.GetCreatureTemplate(tmp.KillCredit.CreatureEntry) == null)
                    {
                        if (_configuration.GetDefaultValue("load:autoclean", false))
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
                    var data = SpawnDataCacheRouter.GameObjectCache.GetGameObjectData(tmp.RespawnGameObject.GOGuid);

                    if (data == null)
                    {
                        if (_configuration.GetDefaultValue("load:autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                        else
                            Log.Logger.Error("Table `{0}` has invalid gameobject (GUID: {1}) in SCRIPT_COMMAND_RESPAWN_GAMEOBJECT for script id {2}",
                                             tableName,
                                             tmp.RespawnGameObject.GOGuid,
                                             tmp.id);

                        continue;
                    }

                    var info = GameObjectTemplateCache.GetGameObjectTemplate(data.Id);

                    if (info == null)
                    {
                        if (_configuration.GetDefaultValue("load:autoclean", false))
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
                        if (_configuration.GetDefaultValue("load:autoclean", false))
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
                    if (!_gridDefines.IsValidMapCoord(tmp.TempSummonCreature.PosX, tmp.TempSummonCreature.PosY, tmp.TempSummonCreature.PosZ, tmp.TempSummonCreature.Orientation))
                    {
                        if (_configuration.GetDefaultValue("load:autoclean", false))
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

                    if (CreatureTemplateCache.GetCreatureTemplate(tmp.TempSummonCreature.CreatureEntry) == null)
                    {
                        if (_configuration.GetDefaultValue("load:autoclean", false))
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
                    var data = SpawnDataCacheRouter.GameObjectCache.GetGameObjectData(tmp.ToggleDoor.GOGuid);

                    if (data == null)
                    {
                        if (_configuration.GetDefaultValue("load:autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                        else
                            Log.Logger.Error("Table `{0}` has invalid gameobject (GUID: {1}) in {2} for script id {3}",
                                             tableName,
                                             tmp.ToggleDoor.GOGuid,
                                             tmp.command,
                                             tmp.id);

                        continue;
                    }

                    var info = GameObjectTemplateCache.GetGameObjectTemplate(data.Id);

                    if (info == null)
                    {
                        if (_configuration.GetDefaultValue("load:autoclean", false))
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
                        if (_configuration.GetDefaultValue("load:autoclean", false))
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
                    if (!_spellManager.HasSpellInfo(tmp.RemoveAura.SpellID))
                    {
                        if (_configuration.GetDefaultValue("load:autoclean", false))
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
                        if (_configuration.GetDefaultValue("load:autoclean", false))
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
                    if (!_spellManager.HasSpellInfo(tmp.CastSpell.SpellID))
                    {
                        if (_configuration.GetDefaultValue("load:autoclean", false))
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
                        if (_configuration.GetDefaultValue("load:autoclean", false))
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
                        if (_configuration.GetDefaultValue("load:autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                        else
                            Log.Logger.Error("Table `{0}` using unknown flags in dataint ({1}) in SCRIPT_COMMAND_CAST_SPELL for script id {2}",
                                             tableName,
                                             tmp.CastSpell.CreatureEntry,
                                             tmp.id);

                        continue;
                    }

                    if ((int)tmp.CastSpell.Flags == 4 && CreatureTemplateCache.GetCreatureTemplate((uint)tmp.CastSpell.CreatureEntry) == null)
                    {
                        if (_configuration.GetDefaultValue("load:autoclean", false))
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
                    if (_itemTemplateCache.GetItemTemplate(tmp.CreateItem.ItemEntry) == null)
                    {
                        if (_configuration.GetDefaultValue("load:autoclean", false))
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
                        if (_configuration.GetDefaultValue("load:autoclean", false))
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
                        if (_configuration.GetDefaultValue("load:autoclean", false))
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
                    if (_configuration.GetDefaultValue("load:autoclean", false))
                        _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                    else
                        Log.Logger.Error($"Table `{tableName}` uses deprecated direct updatefield modify command {tmp.command} for script id {tmp.id}");

                    continue;
                }
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
            TerrainSwaps.Add(mapId, terrainSwapInfo);

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

            if (!_db2Manager.IsUiMapPhase((int)uiMapPhaseId))
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

    private void PlayerCreateInfoAddItemHelper(uint race, uint @class, uint itemId, int count)
    {
        if (!PlayerInfos.TryGetValue((Race)race, (PlayerClass)@class, out var playerInfo))
            return;

        if (count > 0)
            playerInfo.Items.Add(new PlayerCreateInfoItem(itemId, (uint)count));
        else
        {
            if (count < -1)
                Log.Logger.Error("Invalid count {0} specified on item {1} be removed from original player create info (use -1)!", count, itemId);

            playerInfo.Items.RemoveAll(item => item.ItemId == itemId);
        }
    }
}