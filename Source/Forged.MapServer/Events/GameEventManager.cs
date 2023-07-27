// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.BattleGrounds;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps;
using Forged.MapServer.Pools;
using Forged.MapServer.World;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Events;

public class GameEventManager
{
    public List<ulong>[] GameEventCreatureGuids;
    public List<ulong>[] GameEventGameobjectGuids;
    private readonly List<ushort> _activeEvents = new();
    private readonly BattlegroundManager _battlegroundManager;
    private readonly CharacterDatabase _characterDatabase;
    private readonly CliDB _cliDB;
    private readonly IConfiguration _configuration;
    private readonly CreatureFactory _creatureFactory;
    private readonly GameObjectFactory _gameObjectFactory;
    private readonly MapManager _mapManager;
    private readonly GameObjectManager _objectManager;
    private readonly PoolManager _poolManager;
    private readonly Dictionary<uint, GameEventQuestToEventConditionNum> _questToEventConditionNums = new();
    private readonly WorldDatabase _worldDatabase;
    private readonly WorldManager _worldManager;
    private readonly WorldStateManager _worldStateManager;
    private GameEventData[] _gameEvent;
    private uint[] _gameEventBattlegroundHolidays;
    private List<Tuple<uint, uint>>[] _gameEventCreatureQuests;
    private List<Tuple<uint, uint>>[] _gameEventGameObjectQuests;
    private List<Tuple<ulong, ModelEquip>>[] _gameEventModelEquip;
    private List<(ulong guid, ulong npcflag)>[] _gameEventNpcFlags;
    private List<uint>[] _gameEventPoolIds;
    private Dictionary<uint, VendorItem>[] _gameEventVendors;
    private bool _isSystemInit;

    public GameEventManager(WorldManager worldManager, CharacterDatabase characterDatabase, WorldDatabase worldDatabase, CliDB cliDB,
                            BattlegroundManager battlegroundManager, WorldStateManager worldStateManager, MapManager mapManager, GameObjectManager objectManager,
                            PoolManager poolManager, IConfiguration configuration, CreatureFactory creatureFactory, GameObjectFactory gameObjectFactory)
    {
        _worldManager = worldManager;
        _characterDatabase = characterDatabase;
        _worldDatabase = worldDatabase;
        _cliDB = cliDB;
        _battlegroundManager = battlegroundManager;
        _worldStateManager = worldStateManager;
        _mapManager = mapManager;
        _objectManager = objectManager;
        _poolManager = poolManager;
        _configuration = configuration;
        _creatureFactory = creatureFactory;
        _gameObjectFactory = gameObjectFactory;
    }

    public List<ushort> GetActiveEventList()
    {
        return _activeEvents;
    }

    public GameEventData[] GetEventMap()
    {
        return _gameEvent;
    }

    public ulong GetNPCFlag(Creature cr)
    {
        ulong mask = 0;
        var guid = cr.SpawnId;

        foreach (var id in _activeEvents)
        {
            foreach (var pair in _gameEventNpcFlags[id])
                if (pair.guid == guid)
                    mask |= pair.npcflag;
        }

        return mask;
    }

    public void HandleQuestComplete(uint questID)
    {
        // translate the quest to event and condition
        var questToEvent = _questToEventConditionNums.LookupByKey(questID);

        // quest is registered
        if (questToEvent == null)
            return;

        var eventID = questToEvent.EventID;
        var condition = questToEvent.Condition;
        var num = questToEvent.Num;

        // the event is not active, so return, don't increase condition finishes
        if (!IsActiveEvent(eventID))
            return;

        // not in correct phase, return
        if (_gameEvent[eventID].State != GameEventState.WorldConditions)
            return;

        var eventFinishCond = _gameEvent[eventID].Conditions.LookupByKey(condition);

        // condition is registered
        if (eventFinishCond == null)
            return;

        // increase the done count, only if less then the req
        if (!(eventFinishCond.Done < eventFinishCond.ReqNum))
            return;

        eventFinishCond.Done += num;

        // check max limit
        if (eventFinishCond.Done > eventFinishCond.ReqNum)
            eventFinishCond.Done = eventFinishCond.ReqNum;

        // save the change to db
        SQLTransaction trans = new();

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GAME_EVENT_CONDITION_SAVE);
        stmt.AddValue(0, eventID);
        stmt.AddValue(1, condition);
        trans.Append(stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_GAME_EVENT_CONDITION_SAVE);
        stmt.AddValue(0, eventID);
        stmt.AddValue(1, condition);
        stmt.AddValue(2, eventFinishCond.Done);
        trans.Append(stmt);
        _characterDatabase.CommitTransaction(trans);

        // check if all conditions are met, if so, update the event state
        if (!CheckOneGameEventConditions(eventID))
            return;

        // changed, save to DB the gameevent state
        SaveWorldEventStateToDB(eventID);
        // force update events to set timer
        _worldManager.ForceGameEventUpdate();
    }

    public void Initialize()
    {
        var result = _worldDatabase.Query("SELECT MAX(eventEntry) FROM game_event");

        if (result.IsEmpty())
            return;

        int maxEventId = result.Read<byte>(0);

        // Id starts with 1 and array with 0, thus increment
        maxEventId++;

        _gameEvent = new GameEventData[maxEventId];
        GameEventCreatureGuids = new List<ulong>[maxEventId * 2 - 1];
        GameEventGameobjectGuids = new List<ulong>[maxEventId * 2 - 1];
        _gameEventPoolIds = new List<uint>[maxEventId * 2 - 1];

        for (var i = 0; i < maxEventId * 2 - 1; ++i)
        {
            GameEventCreatureGuids[i] = new List<ulong>();
            GameEventGameobjectGuids[i] = new List<ulong>();
            _gameEventPoolIds[i] = new List<uint>();
        }

        _gameEventCreatureQuests = new List<Tuple<uint, uint>>[maxEventId];
        _gameEventGameObjectQuests = new List<Tuple<uint, uint>>[maxEventId];
        _gameEventVendors = new Dictionary<uint, VendorItem>[maxEventId];
        _gameEventBattlegroundHolidays = new uint[maxEventId];
        _gameEventNpcFlags = new List<(ulong guid, ulong npcflag)>[maxEventId];
        _gameEventModelEquip = new List<Tuple<ulong, ModelEquip>>[maxEventId];

        for (var i = 0; i < maxEventId; ++i)
        {
            _gameEvent[i] = new GameEventData();
            _gameEventCreatureQuests[i] = new List<Tuple<uint, uint>>();
            _gameEventGameObjectQuests[i] = new List<Tuple<uint, uint>>();
            _gameEventVendors[i] = new Dictionary<uint, VendorItem>();
            _gameEventNpcFlags[i] = new List<(ulong guid, ulong npcflag)>();
            _gameEventModelEquip[i] = new List<Tuple<ulong, ModelEquip>>();
        }
    }

    public bool IsActiveEvent(ushort eventID)
    {
        return _activeEvents.Contains(eventID);
    }

    public bool IsEventActive(ushort eventId)
    {
        var ae = GetActiveEventList();

        return ae.Contains(eventId);
    }

    public bool IsHolidayActive(HolidayIds id)
    {
        if (id == HolidayIds.None)
            return false;

        var events = GetEventMap();
        var activeEvents = GetActiveEventList();

        return activeEvents.Any(eventId => events[eventId].HolidayID == id);
    }

    public void LoadFromDB()
    {
        {
            var oldMSTime = Time.MSTime;
            //                                         0           1                           2                         3          4       5        6            7            8             9
            var result = _worldDatabase.Query("SELECT eventEntry, UNIX_TIMESTAMP(start_time), UNIX_TIMESTAMP(end_time), occurence, length, holiday, holidayStage, description, world_event, announce FROM game_event");

            if (result.IsEmpty())
            {
                _gameEvent.Clear();
                Log.Logger.Information("Loaded 0 GameInfo events. DB table `game_event` is empty.");

                return;
            }

            uint count = 0;

            do
            {
                var eventID = result.Read<byte>(0);

                if (eventID == 0)
                {
                    Log.Logger.Error("`game_event` GameInfo event entry 0 is reserved and can't be used.");

                    continue;
                }

                GameEventData pGameEvent = new();
                var starttime = result.Read<ulong>(1);
                pGameEvent.Start = (long)starttime;
                var endtime = result.Read<ulong>(2);
                pGameEvent.End = (long)endtime;
                pGameEvent.Occurence = result.Read<uint>(3);
                pGameEvent.Length = result.Read<uint>(4);
                pGameEvent.HolidayID = (HolidayIds)result.Read<uint>(5);

                pGameEvent.HolidayStage = result.Read<byte>(6);
                pGameEvent.Description = result.Read<string>(7);
                pGameEvent.State = (GameEventState)result.Read<byte>(8);
                pGameEvent.Announce = result.Read<byte>(9);
                pGameEvent.Nextstart = 0;

                ++count;

                if (pGameEvent.Length == 0 && pGameEvent.State == GameEventState.Normal) // length>0 is validity check
                {
                    Log.Logger.Error($"`game_event` GameInfo event id ({eventID}) isn't a world event and has length = 0, thus it can't be used.");

                    continue;
                }

                if (pGameEvent.HolidayID != HolidayIds.None)
                {
                    if (!_cliDB.HolidaysStorage.ContainsKey((uint)pGameEvent.HolidayID))
                    {
                        Log.Logger.Error($"`game_event` GameInfo event id ({eventID}) contains nonexisting holiday id {pGameEvent.HolidayID}.");
                        pGameEvent.HolidayID = HolidayIds.None;

                        continue;
                    }

                    if (pGameEvent.HolidayStage > SharedConst.MaxHolidayDurations)
                    {
                        Log.Logger.Error($"`game_event` GameInfo event id ({eventID}) has out of range holidayStage {pGameEvent.HolidayStage}.");
                        pGameEvent.HolidayStage = 0;

                        continue;
                    }

                    SetHolidayEventTime(pGameEvent);
                }

                _gameEvent[eventID] = pGameEvent;
            } while (result.NextRow());

            Log.Logger.Information("Loaded {0} GameInfo events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        Log.Logger.Information("Loading Game Event Saves Data...");

        {
            var oldMSTime = Time.MSTime;

            //                                                       0       1        2
            var result = _characterDatabase.Query("SELECT eventEntry, state, next_start FROM game_event_save");

            if (result.IsEmpty())
                Log.Logger.Information("Loaded 0 GameInfo event saves in GameInfo events. DB table `game_event_save` is empty.");
            else
            {
                uint count = 0;

                do
                {
                    var eventID = result.Read<byte>(0);

                    if (eventID >= _gameEvent.Length)
                    {
                        Log.Logger.Error("`game_event_save` GameInfo event entry ({0}) not exist in `game_event`", eventID);

                        continue;
                    }

                    if (_gameEvent[eventID].State != GameEventState.Normal && _gameEvent[eventID].State != GameEventState.Internal)
                    {
                        _gameEvent[eventID].State = (GameEventState)result.Read<byte>(1);
                        _gameEvent[eventID].Nextstart = result.Read<uint>(2);
                    }
                    else
                    {
                        Log.Logger.Error("game_event_save includes event save for non-worldevent id {0}", eventID);

                        continue;
                    }

                    ++count;
                } while (result.NextRow());

                Log.Logger.Information("Loaded {0} GameInfo event saves in GameInfo events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
        }

        Log.Logger.Information("Loading Game Event Prerequisite Data...");

        {
            var oldMSTime = Time.MSTime;

            //                                                   0             1
            var result = _worldDatabase.Query("SELECT eventEntry, prerequisite_event FROM game_event_prerequisite");

            if (result.IsEmpty())
                Log.Logger.Information("Loaded 0 GameInfo event prerequisites in GameInfo events. DB table `game_event_prerequisite` is empty.");
            else
            {
                uint count = 0;

                do
                {
                    ushort eventID = result.Read<byte>(0);

                    if (eventID >= _gameEvent.Length)
                    {
                        Log.Logger.Error("`game_event_prerequisite` GameInfo event id ({0}) is out of range compared to max event id in `game_event`", eventID);

                        continue;
                    }

                    if (_gameEvent[eventID].State != GameEventState.Normal && _gameEvent[eventID].State != GameEventState.Internal)
                    {
                        ushort prerequisiteEvent = result.Read<byte>(1);

                        if (prerequisiteEvent >= _gameEvent.Length)
                        {
                            Log.Logger.Error("`game_event_prerequisite` GameInfo event prerequisite id ({0}) not exist in `game_event`", prerequisiteEvent);

                            continue;
                        }

                        _gameEvent[eventID].PrerequisiteEvents.Add(prerequisiteEvent);
                    }
                    else
                    {
                        Log.Logger.Error("game_event_prerequisiste includes event entry for non-worldevent id {0}", eventID);

                        continue;
                    }

                    ++count;
                } while (result.NextRow());

                Log.Logger.Information("Loaded {0} GameInfo event prerequisites in GameInfo events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
        }

        Log.Logger.Information("Loading Game Event Creature Data...");

        {
            var oldMSTime = Time.MSTime;

            //                                                 0        1
            var result = _worldDatabase.Query("SELECT guid, eventEntry FROM game_event_creature");

            if (result.IsEmpty())
                Log.Logger.Information("Loaded 0 creatures in GameInfo events. DB table `game_event_creature` is empty");
            else
            {
                uint count = 0;

                do
                {
                    var guid = result.Read<ulong>(0);
                    short eventID = result.Read<sbyte>(1);
                    var internalEventID = _gameEvent.Length + eventID - 1;

                    var data = _objectManager.GetCreatureData(guid);

                    if (data == null)
                    {
                        if (_configuration.GetDefaultValue("load:autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM game_event_creature WHERE guid = {guid}");
                        else
                            Log.Logger.Error("`game_event_creature` contains creature (GUID: {0}) not found in `creature` table.", guid);

                        continue;
                    }

                    if (internalEventID < 0 || internalEventID >= GameEventCreatureGuids.Length)
                    {
                        Log.Logger.Error("`game_event_creature` GameInfo event id ({0}) not exist in `game_event`", eventID);

                        continue;
                    }

                    // Log error for pooled object, but still spawn it
                    if (data.PoolId != 0)
                        Log.Logger.Error($"`game_event_creature`: GameInfo event id ({eventID}) contains creature ({guid}) which is part of a pool ({data.PoolId}). This should be spawned in game_event_pool");

                    GameEventCreatureGuids[internalEventID].Add(guid);

                    ++count;
                } while (result.NextRow());

                Log.Logger.Information("Loaded {0} creatures in GameInfo events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
        }

        Log.Logger.Information("Loading Game Event GO Data...");

        {
            var oldMSTime = Time.MSTime;

            //                                                0         1
            var result = _worldDatabase.Query("SELECT guid, eventEntry FROM game_event_gameobject");

            if (result.IsEmpty())
                Log.Logger.Information("Loaded 0 gameobjects in GameInfo events. DB table `game_event_gameobject` is empty.");
            else
            {
                uint count = 0;

                do
                {
                    var guid = result.Read<ulong>(0);
                    short eventID = result.Read<byte>(1);
                    var internalEventID = _gameEvent.Length + eventID - 1;

                    var data = _objectManager.GetGameObjectData(guid);

                    if (data == null)
                    {
                        Log.Logger.Error("`game_event_gameobject` contains gameobject (GUID: {0}) not found in `gameobject` table.", guid);

                        continue;
                    }

                    if (internalEventID < 0 || internalEventID >= GameEventGameobjectGuids.Length)
                    {
                        Log.Logger.Error("`game_event_gameobject` GameInfo event id ({0}) not exist in `game_event`", eventID);

                        continue;
                    }

                    // Log error for pooled object, but still spawn it
                    if (data.PoolId != 0)
                        Log.Logger.Error($"`game_event_gameobject`: GameInfo event id ({eventID}) contains GameInfo object ({guid}) which is part of a pool ({data.PoolId}). This should be spawned in game_event_pool");

                    GameEventGameobjectGuids[internalEventID].Add(guid);

                    ++count;
                } while (result.NextRow());

                Log.Logger.Information("Loaded {0} gameobjects in GameInfo events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
        }

        Log.Logger.Information("Loading Game Event Model/Equipment Change Data...");

        {
            var oldMSTime = Time.MSTime;

            //                                                       0           1                       2                                 3                                     4
            var result = _worldDatabase.Query("SELECT creature.guid, creature.id, game_event_model_equip.eventEntry, game_event_model_equip.modelid, game_event_model_equip.equipment_id " +
                                              "FROM creature JOIN game_event_model_equip ON creature.guid=game_event_model_equip.guid");

            if (result.IsEmpty())
                Log.Logger.Information("Loaded 0 model/equipment changes in GameInfo events. DB table `game_event_model_equip` is empty.");
            else
            {
                uint count = 0;

                do
                {
                    var guid = result.Read<ulong>(0);
                    var entry = result.Read<uint>(1);
                    ushort eventID = result.Read<byte>(2);

                    if (eventID >= _gameEventModelEquip.Length)
                    {
                        Log.Logger.Error("`game_event_model_equip` GameInfo event id ({0}) is out of range compared to max event id in `game_event`", eventID);

                        continue;
                    }

                    ModelEquip newModelEquipSet = new()
                    {
                        Modelid = result.Read<uint>(3),
                        EquipmentID = result.Read<byte>(4),
                        EquipementIDPrev = 0,
                        ModelidPrev = 0
                    };

                    if (newModelEquipSet.EquipmentID > 0)
                    {
                        var equipId = (sbyte)newModelEquipSet.EquipmentID;

                        if (_objectManager.GetEquipmentInfo(entry, equipId) == null)
                        {
                            Log.Logger.Error("Table `game_event_model_equip` have creature (Guid: {0}, entry: {1}) with equipment_id {2} not found in table `creature_equip_template`, set to no equipment.",
                                             guid,
                                             entry,
                                             newModelEquipSet.EquipmentID);

                            continue;
                        }
                    }

                    _gameEventModelEquip[eventID].Add(Tuple.Create(guid, newModelEquipSet));

                    ++count;
                } while (result.NextRow());

                Log.Logger.Information("Loaded {0} model/equipment changes in GameInfo events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
        }

        Log.Logger.Information("Loading Game Event QuestId Data...");

        {
            var oldMSTime = Time.MSTime;

            //                                               0     1      2
            var result = _worldDatabase.Query("SELECT id, quest, eventEntry FROM game_event_creature_quest");

            if (result.IsEmpty())
                Log.Logger.Information("Loaded 0 quests additions in GameInfo events. DB table `game_event_creature_quest` is empty.");
            else
            {
                uint count = 0;

                do
                {
                    var id = result.Read<uint>(0);
                    var quest = result.Read<uint>(1);
                    ushort eventID = result.Read<byte>(2);

                    if (eventID >= _gameEventCreatureQuests.Length)
                    {
                        Log.Logger.Error("`game_event_creature_quest` GameInfo event id ({0}) not exist in `game_event`", eventID);

                        continue;
                    }

                    _gameEventCreatureQuests[eventID].Add(Tuple.Create(id, quest));

                    ++count;
                } while (result.NextRow());

                Log.Logger.Information("Loaded {0} quests additions in GameInfo events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
        }

        Log.Logger.Information("Loading Game Event GO QuestId Data...");

        {
            var oldMSTime = Time.MSTime;

            //                                               0     1      2
            var result = _worldDatabase.Query("SELECT id, quest, eventEntry FROM game_event_gameobject_quest");

            if (result.IsEmpty())
                Log.Logger.Information("Loaded 0 go quests additions in GameInfo events. DB table `game_event_gameobject_quest` is empty.");
            else
            {
                uint count = 0;

                do
                {
                    var id = result.Read<uint>(0);
                    var quest = result.Read<uint>(1);
                    ushort eventID = result.Read<byte>(2);

                    if (eventID >= _gameEventGameObjectQuests.Length)
                    {
                        Log.Logger.Error("`game_event_gameobject_quest` GameInfo event id ({0}) not exist in `game_event`", eventID);

                        continue;
                    }

                    _gameEventGameObjectQuests[eventID].Add(Tuple.Create(id, quest));

                    ++count;
                } while (result.NextRow());

                Log.Logger.Information("Loaded {0} quests additions in GameInfo events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
        }

        Log.Logger.Information("Loading Game Event QuestId Condition Data...");

        {
            var oldMSTime = Time.MSTime;

            //                                                 0       1         2             3
            var result = _worldDatabase.Query("SELECT quest, eventEntry, condition_id, num FROM game_event_quest_condition");

            if (result.IsEmpty())
                Log.Logger.Information("Loaded 0 quest event conditions in GameInfo events. DB table `game_event_quest_condition` is empty.");
            else
            {
                uint count = 0;

                do
                {
                    var quest = result.Read<uint>(0);
                    ushort eventID = result.Read<byte>(1);
                    var condition = result.Read<uint>(2);
                    var num = result.Read<float>(3);

                    if (eventID >= _gameEvent.Length)
                    {
                        Log.Logger.Error("`game_event_quest_condition` GameInfo event id ({0}) is out of range compared to max event id in `game_event`", eventID);

                        continue;
                    }

                    if (!_questToEventConditionNums.ContainsKey(quest))
                        _questToEventConditionNums[quest] = new GameEventQuestToEventConditionNum();

                    _questToEventConditionNums[quest].EventID = eventID;
                    _questToEventConditionNums[quest].Condition = condition;
                    _questToEventConditionNums[quest].Num = num;

                    ++count;
                } while (result.NextRow());

                Log.Logger.Information("Loaded {0} quest event conditions in GameInfo events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
        }

        Log.Logger.Information("Loading Game Event Condition Data...");

        {
            var oldMSTime = Time.MSTime;

            //                                                  0          1            2             3                      4
            var result = _worldDatabase.Query("SELECT eventEntry, condition_id, req_num, max_world_state_field, done_world_state_field FROM game_event_condition");

            if (result.IsEmpty())
                Log.Logger.Information("Loaded 0 conditions in GameInfo events. DB table `game_event_condition` is empty.");
            else
            {
                uint count = 0;

                do
                {
                    ushort eventID = result.Read<byte>(0);
                    var condition = result.Read<uint>(1);

                    if (eventID >= _gameEvent.Length)
                    {
                        Log.Logger.Error("`game_event_condition` GameInfo event id ({0}) is out of range compared to max event id in `game_event`", eventID);

                        continue;
                    }

                    _gameEvent[eventID].Conditions[condition].ReqNum = result.Read<float>(2);
                    _gameEvent[eventID].Conditions[condition].Done = 0;
                    _gameEvent[eventID].Conditions[condition].MaxWorldState = result.Read<ushort>(3);
                    _gameEvent[eventID].Conditions[condition].DoneWorldState = result.Read<ushort>(4);

                    ++count;
                } while (result.NextRow());

                Log.Logger.Information("Loaded {0} conditions in GameInfo events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
        }

        Log.Logger.Information("Loading Game Event Condition Save Data...");

        {
            var oldMSTime = Time.MSTime;

            //                                                      0           1         2
            var result = _characterDatabase.Query("SELECT eventEntry, condition_id, done FROM game_event_condition_save");

            if (result.IsEmpty())
                Log.Logger.Information("Loaded 0 condition saves in GameInfo events. DB table `game_event_condition_save` is empty.");
            else
            {
                uint count = 0;

                do
                {
                    ushort eventID = result.Read<byte>(0);
                    var condition = result.Read<uint>(1);

                    if (eventID >= _gameEvent.Length)
                    {
                        Log.Logger.Error("`game_event_condition_save` GameInfo event id ({0}) is out of range compared to max event id in `game_event`", eventID);

                        continue;
                    }

                    if (_gameEvent[eventID].Conditions.ContainsKey(condition))
                        _gameEvent[eventID].Conditions[condition].Done = result.Read<uint>(2);
                    else
                    {
                        Log.Logger.Error("game_event_condition_save contains not present condition evt id {0} cond id {1}", eventID, condition);

                        continue;
                    }

                    ++count;
                } while (result.NextRow());

                Log.Logger.Information("Loaded {0} condition saves in GameInfo events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
        }

        Log.Logger.Information("Loading Game Event NPCflag Data...");

        {
            var oldMSTime = Time.MSTime;

            //                                                0       1        2
            var result = _worldDatabase.Query("SELECT guid, eventEntry, npcflag FROM game_event_npcflag");

            if (result.IsEmpty())
                Log.Logger.Information("Loaded 0 npcflags in GameInfo events. DB table `game_event_npcflag` is empty.");
            else
            {
                uint count = 0;

                do
                {
                    var guid = result.Read<ulong>(0);
                    ushort eventID = result.Read<byte>(1);
                    var npcflag = result.Read<ulong>(2);

                    if (eventID >= _gameEvent.Length)
                    {
                        Log.Logger.Error("`game_event_npcflag` GameInfo event id ({0}) is out of range compared to max event id in `game_event`", eventID);

                        continue;
                    }

                    _gameEventNpcFlags[eventID].Add((guid, npcflag));

                    ++count;
                } while (result.NextRow());

                Log.Logger.Information("Loaded {0} npcflags in GameInfo events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
        }

        Log.Logger.Information("Loading Game Event Seasonal QuestId Relations...");

        {
            var oldMSTime = Time.MSTime;

            //                                                  0          1
            var result = _worldDatabase.Query("SELECT questId, eventEntry FROM game_event_seasonal_questrelation");

            if (result.IsEmpty())
                Log.Logger.Information("Loaded 0 seasonal quests additions in GameInfo events. DB table `game_event_seasonal_questrelation` is empty.");
            else
            {
                uint count = 0;

                do
                {
                    var questId = result.Read<uint>(0);
                    ushort eventEntry = result.Read<byte>(1);

                    var questTemplate = _objectManager.GetQuestTemplate(questId);

                    if (questTemplate == null)
                    {
                        Log.Logger.Error("`game_event_seasonal_questrelation` quest id ({0}) does not exist in `quest_template`", questId);

                        continue;
                    }

                    if (eventEntry >= _gameEvent.Length)
                    {
                        Log.Logger.Error("`game_event_seasonal_questrelation` event id ({0}) not exist in `game_event`", eventEntry);

                        continue;
                    }

                    questTemplate.EventIdForQuest = eventEntry;
                    ++count;
                } while (result.NextRow());

                Log.Logger.Information("Loaded {0} quests additions in GameInfo events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
        }

        Log.Logger.Information("Loading Game Event Vendor Additions Data...");

        {
            var oldMSTime = Time.MSTime;

            //                                               0           1     2     3         4         5             6     7             8                  9
            var result = _worldDatabase.Query("SELECT eventEntry, guid, item, maxcount, incrtime, ExtendedCost, type, BonusListIDs, PlayerConditionId, IgnoreFiltering FROM game_event_npc_vendor ORDER BY guid, slot ASC");

            if (result.IsEmpty())
                Log.Logger.Information("Loaded 0 vendor additions in GameInfo events. DB table `game_event_npc_vendor` is empty.");
            else
            {
                uint count = 0;

                do
                {
                    var eventID = result.Read<byte>(0);
                    var guid = result.Read<ulong>(1);

                    if (eventID >= _gameEventVendors.Length)
                    {
                        Log.Logger.Error("`game_event_npc_vendor` GameInfo event id ({0}) not exist in `game_event`", eventID);

                        continue;
                    }

                    // get the event npc Id for checking if the npc will be vendor during the event or not
                    ulong eventNpcFlag = 0;
                    var flist = _gameEventNpcFlags[eventID];

                    foreach (var pair in flist)
                        if (pair.guid == guid)
                        {
                            eventNpcFlag = pair.npcflag;

                            break;
                        }

                    // get creature entry
                    uint entry = 0;
                    var data = _objectManager.GetCreatureData(guid);

                    if (data != null)
                        entry = data.Id;

                    VendorItem vItem = new()
                    {
                        Item = result.Read<uint>(2),
                        Maxcount = result.Read<uint>(3),
                        Incrtime = result.Read<uint>(4),
                        ExtendedCost = result.Read<uint>(5),
                        Type = (ItemVendorType)result.Read<byte>(6),
                        PlayerConditionId = result.Read<uint>(8),
                        IgnoreFiltering = result.Read<bool>(9)
                    };

                    var bonusListIDsTok = new StringArray(result.Read<string>(7), ' ');

                    if (!bonusListIDsTok.IsEmpty())
                        foreach (uint token in bonusListIDsTok)
                            vItem.BonusListIDs.Add(token);

                    // check validity with event's npcflag
                    if (!_objectManager.VendorItemCache.IsVendorItemValid(entry, vItem, null, null, eventNpcFlag))
                        continue;

                    _gameEventVendors[eventID].Add(entry, vItem);

                    ++count;
                } while (result.NextRow());

                Log.Logger.Information("Loaded {0} vendor additions in GameInfo events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
        }

        Log.Logger.Information("Loading Game Event Battleground Holiday Data...");

        {
            var oldMSTime = Time.MSTime;

            //                                         0           1
            var result = _worldDatabase.Query("SELECT EventEntry, BattlegroundID FROM game_event_battleground_holiday");

            if (result.IsEmpty())
                Log.Logger.Information("Loaded 0 Battlegroundholidays in GameInfo events. DB table `game_event_battleground_holiday` is empty.");
            else
            {
                uint count = 0;

                do
                {
                    ushort eventId = result.Read<byte>(0);

                    if (eventId >= _gameEvent.Length)
                    {
                        Log.Logger.Error("`game_event_battleground_holiday` GameInfo event id ({0}) not exist in `game_event`", eventId);

                        continue;
                    }

                    _gameEventBattlegroundHolidays[eventId] = result.Read<uint>(1);

                    ++count;
                } while (result.NextRow());

                Log.Logger.Information("Loaded {0} Battlegroundholidays in GameInfo events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
        }

        Log.Logger.Information("Loading Game Event Pool Data...");

        {
            var oldMSTime = Time.MSTime;

            //                                                               0                         1
            var result = _worldDatabase.Query("SELECT pool_template.entry, game_event_pool.eventEntry FROM pool_template" +
                                              " JOIN game_event_pool ON pool_template.entry = game_event_pool.pool_entry");

            if (result.IsEmpty())
                Log.Logger.Information("Loaded 0 pools for GameInfo events. DB table `game_event_pool` is empty.");
            else
            {
                uint count = 0;

                do
                {
                    var entry = result.Read<uint>(0);
                    short eventID = result.Read<sbyte>(1);
                    var internalEventID = _gameEvent.Length + eventID - 1;

                    if (internalEventID < 0 || internalEventID >= _gameEventPoolIds.Length)
                    {
                        Log.Logger.Error("`game_event_pool` GameInfo event id ({0}) not exist in `game_event`", eventID);

                        continue;
                    }

                    if (!_poolManager.CheckPool(entry))
                    {
                        Log.Logger.Error("Pool Id ({0}) has all creatures or gameobjects with explicit chance sum <>100 and no equal chance defined. The pool system cannot pick one to spawn.", entry);

                        continue;
                    }


                    _gameEventPoolIds[internalEventID].Add(entry);

                    ++count;
                } while (result.NextRow());

                Log.Logger.Information("Loaded {0} pools for GameInfo events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
        }
    }

    public uint NextCheck(ushort entry)
    {
        var currenttime = GameTime.CurrentTime;

        // for NEXTPHASE state world events, return the delay to start the next event, so the followup event will be checked correctly
        if (_gameEvent[entry].State is GameEventState.WorldNextPhase or GameEventState.WorldFinished && _gameEvent[entry].Nextstart >= currenttime)
            return (uint)(_gameEvent[entry].Nextstart - currenttime);

        // for CONDITIONS state world events, return the length of the wait period, so if the conditions are met, this check will be called again to set the timer as NEXTPHASE event
        if (_gameEvent[entry].State == GameEventState.WorldConditions)
        {
            if (_gameEvent[entry].Length != 0)
                return _gameEvent[entry].Length * 60;

            return Time.DAY;
        }

        // outdated event: we return max
        if (currenttime > _gameEvent[entry].End)
            return Time.DAY;

        // never started event, we return delay before start
        if (_gameEvent[entry].Start > currenttime)
            return (uint)(_gameEvent[entry].Start - currenttime);

        uint delay;

        // in event, we return the end of it
        if ((currenttime - _gameEvent[entry].Start) % (_gameEvent[entry].Occurence * 60) < _gameEvent[entry].Length * 60)
            // we return the delay before it ends
            delay = (uint)(_gameEvent[entry].Length * Time.MINUTE - (currenttime - _gameEvent[entry].Start) % (_gameEvent[entry].Occurence * Time.MINUTE));
        else // not in window, we return the delay before next start
            delay = (uint)(_gameEvent[entry].Occurence * Time.MINUTE - (currenttime - _gameEvent[entry].Start) % (_gameEvent[entry].Occurence * Time.MINUTE));

        // In case the end is before next check
        if (_gameEvent[entry].End < currenttime + delay)
            return (uint)(_gameEvent[entry].End - currenttime);

        return delay;
    }

    public void StartArenaSeason()
    {
        var season = _configuration.GetDefaultValue("Arena:ArenaSeason:ID", 32);
        var result = _worldDatabase.Query("SELECT eventEntry FROM game_event_arena_seasons WHERE season = '{0}'", season);

        if (result.IsEmpty())
        {
            Log.Logger.Error("ArenaSeason ({0}) must be an existant Arena Season", season);

            return;
        }

        ushort eventId = result.Read<byte>(0);

        if (eventId >= _gameEvent.Length)
        {
            Log.Logger.Error("EventEntry {0} for ArenaSeason ({1}) does not exists", eventId, season);

            return;
        }

        StartEvent(eventId, true);
        Log.Logger.Information("Arena Season {0} started...", season);
    }

    public bool StartEvent(ushort eventID, bool overwrite = false)
    {
        var data = _gameEvent[eventID];

        if (data.State is GameEventState.Normal or GameEventState.Internal)
        {
            AddActiveEvent(eventID);
            ApplyNewEvent(eventID);

            if (overwrite)
            {
                _gameEvent[eventID].Start = GameTime.CurrentTime;

                if (data.End <= data.Start)
                    data.End = data.Start + data.Length;
            }

            return false;
        }

        if (data.State == GameEventState.WorldInactive)
            // set to conditions phase
            data.State = GameEventState.WorldConditions;

        // add to active events
        AddActiveEvent(eventID);
        // add spawns
        ApplyNewEvent(eventID);

        // check if can go to next state
        var conditionsMet = CheckOneGameEventConditions(eventID);
        // save to db
        SaveWorldEventStateToDB(eventID);

        // force GameInfo event update to set the update timer if conditions were met from a command
        // this update is needed to possibly start events dependent on the started one
        // or to scedule another update where the next event will be started
        if (overwrite && conditionsMet)
            _worldManager.ForceGameEventUpdate();

        return conditionsMet;
    }

    public uint StartSystem() // return the next event delay in ms
    {
        _activeEvents.Clear();
        var delay = Update();
        _isSystemInit = true;

        return delay;
    }

    public void StopEvent(ushort eventID, bool overwrite = false)
    {
        var data = _gameEvent[eventID];
        var serverwideEvt = data.State != GameEventState.Normal && data.State != GameEventState.Internal;

        RemoveActiveEvent(eventID);
        UnApplyEvent(eventID);

        if (overwrite && !serverwideEvt)
        {
            data.Start = GameTime.CurrentTime - data.Length * Time.MINUTE;

            if (data.End <= data.Start)
                data.End = data.Start + data.Length;
        }
        else if (serverwideEvt)
        {
            // if finished world event, then only gm command can stop it
            if (!overwrite && data.State == GameEventState.WorldFinished)
                return;

            // reset conditions
            data.Nextstart = 0;
            data.State = GameEventState.WorldInactive;

            foreach (var pair in data.Conditions)
                pair.Value.Done = 0;

            SQLTransaction trans = new();
            var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ALL_GAME_EVENT_CONDITION_SAVE);
            stmt.AddValue(0, eventID);
            trans.Append(stmt);

            stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GAME_EVENT_SAVE);
            stmt.AddValue(0, eventID);
            trans.Append(stmt);

            _characterDatabase.CommitTransaction(trans);
        }
    }

    public uint Update() // return the next event delay in ms
    {
        var currenttime = GameTime.CurrentTime;
        uint nextEventDelay = Time.DAY; // 1 day
        List<ushort> activate = new();
        List<ushort> deactivate = new();

        for (ushort id = 1; id < _gameEvent.Length; ++id)
        {
            // must do the activating first, and after that the deactivating
            // so first queue it
            if (CheckOneGameEvent(id))
            {
                // if the world event is in NEXTPHASE state, and the time has passed to finish this event, then do so
                if (_gameEvent[id].State == GameEventState.WorldNextPhase && _gameEvent[id].Nextstart <= currenttime)
                {
                    // set this event to finished, null the nextstart time
                    _gameEvent[id].State = GameEventState.WorldFinished;
                    _gameEvent[id].Nextstart = 0;
                    // save the state of this gameevent
                    SaveWorldEventStateToDB(id);

                    // queue for deactivation
                    if (IsActiveEvent(id))
                        deactivate.Add(id);

                    // go to next event, this no longer needs an event update timer
                    continue;
                }

                if (_gameEvent[id].State == GameEventState.WorldConditions && CheckOneGameEventConditions(id))
                    // changed, save to DB the gameevent state, will be updated in next update cycle
                    SaveWorldEventStateToDB(id);

                Log.Logger.Debug("GameEvent {0} is active", id);

                // queue for activation
                if (!IsActiveEvent(id))
                    activate.Add(id);
            }
            else
            {
                Log.Logger.Debug("GameEvent {0} is not active", id);

                if (IsActiveEvent(id))
                    deactivate.Add(id);
                else
                {
                    if (!_isSystemInit)
                    {
                        var eventNid = (short)(-1 * id);
                        // spawn all negative ones for this event
                        GameEventSpawn(eventNid);
                    }
                }
            }

            var calcDelay = NextCheck(id);

            if (calcDelay < nextEventDelay)
                nextEventDelay = calcDelay;
        }

        // now activate the queue
        // a now activated event can contain a spawn of a to-be-deactivated one
        // following the activate - deactivate order, deactivating the first event later will leave the spawn in (wont disappear then reappear clientside)
        foreach (var eventId in activate)
            // start the event
            // returns true the started event completed
            // in that case, initiate next update in 1 second
            if (StartEvent(eventId))
                nextEventDelay = 0;

        foreach (var eventId in deactivate)
            StopEvent(eventId);

        Log.Logger.Information("Next GameInfo event check in {0} seconds.", nextEventDelay + 1);

        return (nextEventDelay + 1) * Time.IN_MILLISECONDS; // Add 1 second to be sure event has started/stopped at next call
    }

    private void AddActiveEvent(ushort eventID)
    {
        _activeEvents.Add(eventID);
    }

    private void ApplyNewEvent(ushort eventID)
    {
        var announce = _gameEvent[eventID].Announce;

        if (announce == 1) // || (announce == 2 && WorldConfigEventAnnounce))
            _worldManager.SendWorldText(CypherStrings.Eventmessage, _gameEvent[eventID].Description);

        Log.Logger.Information("GameEvent {0} \"{1}\" started.", eventID, _gameEvent[eventID].Description);

        // spawn positive event tagget objects
        GameEventSpawn((short)eventID);
        // un-spawn negative event tagged objects
        var eventNid = (short)(-1 * eventID);
        GameEventUnspawn(eventNid);
        // Change equipement or model
        ChangeEquipOrModel((short)eventID, true);
        // Add quests that are events only to non event npc
        UpdateEventQuests(eventID, true);
        UpdateWorldStates(eventID, true);
        // update npcflags in this event
        UpdateEventNPCFlags(eventID);
        // add vendor items
        UpdateEventNPCVendor(eventID, true);
        // update bg holiday
        UpdateBattlegroundSettings();

        //! Run SAI scripts with SMART_EVENT_GAME_EVENT_START
        RunSmartAIScripts(eventID, true);

        // check for seasonal quest reset.
        _worldManager.ResetEventSeasonalQuests(eventID, GetLastStartTime(eventID));
    }

    private void ChangeEquipOrModel(short eventID, bool activate)
    {
        foreach (var tuple in _gameEventModelEquip[eventID])
        {
            // Remove the creature from grid
            var data = _objectManager.GetCreatureData(tuple.Item1);

            if (data == null)
                continue;

            // Update if spawned
            _mapManager.DoForAllMapsWithMapId(data.MapId,
                                              map =>
                                              {
                                                  var creatureBounds = map.CreatureBySpawnIdStore.LookupByKey(tuple.Item1);

                                                  foreach (var creature in creatureBounds)
                                                      if (activate)
                                                      {
                                                          tuple.Item2.EquipementIDPrev = creature.CurrentEquipmentId;
                                                          tuple.Item2.ModelidPrev = creature.DisplayId;
                                                          creature.LoadEquipment(tuple.Item2.EquipmentID);

                                                          if (tuple.Item2.Modelid <= 0 ||
                                                              tuple.Item2.ModelidPrev == tuple.Item2.Modelid ||
                                                              _objectManager.CreatureModelCache.GetCreatureModelInfo(tuple.Item2.Modelid) == null)
                                                              continue;

                                                          creature.SetDisplayId(tuple.Item2.Modelid);
                                                          creature.SetNativeDisplayId(tuple.Item2.Modelid);
                                                      }
                                                      else
                                                      {
                                                          creature.LoadEquipment(tuple.Item2.EquipementIDPrev);

                                                          if (tuple.Item2.ModelidPrev <= 0 ||
                                                              tuple.Item2.ModelidPrev == tuple.Item2.Modelid ||
                                                              _objectManager.CreatureModelCache.GetCreatureModelInfo(tuple.Item2.ModelidPrev) == null)
                                                              continue;

                                                          creature.SetDisplayId(tuple.Item2.ModelidPrev);
                                                          creature.SetNativeDisplayId(tuple.Item2.ModelidPrev);
                                                      }
                                              });

            // now last step: put in data
            var data2 = _objectManager.NewOrExistCreatureData(tuple.Item1);

            if (activate)
            {
                tuple.Item2.ModelidPrev = data2.Displayid;
                tuple.Item2.EquipementIDPrev = (byte)data2.EquipmentId;
                data2.Displayid = tuple.Item2.Modelid;
                data2.EquipmentId = (sbyte)tuple.Item2.EquipmentID;
            }
            else
            {
                data2.Displayid = tuple.Item2.ModelidPrev;
                data2.EquipmentId = (sbyte)tuple.Item2.EquipementIDPrev;
            }
        }
    }

    private bool CheckOneGameEvent(ushort entry)
    {
        switch (_gameEvent[entry].State)
        {
            default:
            case GameEventState.Normal:
            {
                var currenttime = GameTime.CurrentTime;

                // Get the event information
                return _gameEvent[entry].Start < currenttime && currenttime < _gameEvent[entry].End && (currenttime - _gameEvent[entry].Start) % (_gameEvent[entry].Occurence * Time.MINUTE) < _gameEvent[entry].Length * Time.MINUTE;
            }
            // if the state is conditions or nextphase, then the event should be active
            case GameEventState.WorldConditions:
            case GameEventState.WorldNextPhase:
                return true;
            // finished world events are inactive
            case GameEventState.WorldFinished:
            case GameEventState.Internal:
                return false;
            // if inactive world event, check the prerequisite events
            case GameEventState.WorldInactive:
            {
                var currenttime = GameTime.CurrentTime;

                if (_gameEvent[entry]
                    .PrerequisiteEvents.Any(gameEventId => (_gameEvent[gameEventId].State != GameEventState.WorldNextPhase && _gameEvent[gameEventId].State != GameEventState.WorldFinished) || // if prereq not in nextphase or finished state, then can't start this one
                                                           _gameEvent[gameEventId].Nextstart > currenttime))
                    return false;

                // all prerequisite events are met
                // but if there are no prerequisites, this can be only activated through gm command
                return !_gameEvent[entry].PrerequisiteEvents.Empty();
            }
        }
    }

    private bool CheckOneGameEventConditions(ushort eventID)
    {
        foreach (var pair in _gameEvent[eventID].Conditions)
            if (pair.Value.Done < pair.Value.ReqNum)
                // return false if a condition doesn't match
                return false;

        // set the phase
        _gameEvent[eventID].State = GameEventState.WorldNextPhase;

        // set the followup events' start time
        if (_gameEvent[eventID].Nextstart == 0)
        {
            var currenttime = GameTime.CurrentTime;
            _gameEvent[eventID].Nextstart = currenttime + _gameEvent[eventID].Length * 60;
        }

        return true;
    }

    private void GameEventSpawn(short eventID)
    {
        var internalEventID = _gameEvent.Length + eventID - 1;

        if (internalEventID < 0 || internalEventID >= GameEventCreatureGuids.Length)
        {
            Log.Logger.Error("GameEventMgr.GameEventSpawn attempt access to out of range GameEventCreatureGuids element {0} (size: {1})",
                             internalEventID,
                             GameEventCreatureGuids.Length);

            return;
        }

        foreach (var guid in GameEventCreatureGuids[internalEventID])
        {
            // Add to correct cell
            var data = _objectManager.GetCreatureData(guid);

            if (data == null)
                continue;

            _objectManager.AddSpawnDataToGrid(data);

            // Spawn if necessary (loaded grids only)
            _mapManager.DoForAllMapsWithMapId(data.MapId,
                                              map =>
                                              {
                                                  map.RemoveRespawnTime(SpawnObjectType.Creature, guid);

                                                  // We use spawn coords to spawn
                                                  if (map.IsGridLoaded(data.SpawnPoint))
                                                      _creatureFactory.CreateCreatureFromDB(guid, map);
                                              });
        }

        if (internalEventID >= GameEventGameobjectGuids.Length)
        {
            Log.Logger.Error("GameEventMgr.GameEventSpawn attempt access to out of range GameEventGameobjectGuids element {0} (size: {1})",
                             internalEventID,
                             GameEventGameobjectGuids.Length);

            return;
        }

        foreach (var guid in GameEventGameobjectGuids[internalEventID])
        {
            // Add to correct cell
            var data = _objectManager.GetGameObjectData(guid);

            if (data == null)
                continue;

            _objectManager.AddSpawnDataToGrid(data);

            // Spawn if necessary (loaded grids only)
            // this base map checked as non-instanced and then only existed
            _mapManager.DoForAllMapsWithMapId(data.MapId,
                                              map =>
                                              {
                                                  map.RemoveRespawnTime(SpawnObjectType.GameObject, guid);

                                                  // We use current coords to unspawn, not spawn coords since creature can have changed grid
                                                  if (!map.IsGridLoaded(data.SpawnPoint))
                                                      return;

                                                  var go = _gameObjectFactory.CreateGameObjectFromDb(guid, map, false);

                                                  // @todo find out when it is add to map
                                                  if (go is not { IsSpawnedByDefault: true })
                                                      return;

                                                  // @todo find out when it is add to map

                                                  if (!map.AddToMap(go))
                                                      go.Dispose();
                                              });
        }

        if (internalEventID >= _gameEventPoolIds.Length)
        {
            Log.Logger.Error("GameEventMgr.GameEventSpawn attempt access to out of range _gameEventPoolIds element {0} (size: {1})",
                             internalEventID,
                             _gameEventPoolIds.Length);

            return;
        }

        foreach (var id in _gameEventPoolIds[internalEventID])
        {
            var poolTemplate = _poolManager.GetPoolTemplate(id);

            if (poolTemplate != null)
                _mapManager.DoForAllMapsWithMapId((uint)poolTemplate.MapId, map => { _poolManager.SpawnPool(map.PoolData, id); });
        }
    }

    private void GameEventUnspawn(short eventID)
    {
        var internalEventID = _gameEvent.Length + eventID - 1;

        if (internalEventID < 0 || internalEventID >= GameEventCreatureGuids.Length)
        {
            Log.Logger.Error("GameEventMgr.GameEventUnspawn attempt access to out of range GameEventCreatureGuids element {0} (size: {1})",
                             internalEventID,
                             GameEventCreatureGuids.Length);

            return;
        }

        foreach (var guid in GameEventCreatureGuids[internalEventID])
        {
            // check if it's needed by another event, if so, don't remove
            if (eventID > 0 && HasCreatureActiveEventExcept(guid, (ushort)eventID))
                continue;

            // Remove the creature from grid
            var data = _objectManager.GetCreatureData(guid);

            if (data == null)
                continue;

            _objectManager.RemoveCreatureFromGrid(data);

            _mapManager.DoForAllMapsWithMapId(data.MapId,
                                              map =>
                                              {
                                                  map.RemoveRespawnTime(SpawnObjectType.Creature, guid);
                                                  var creatureBounds = map.CreatureBySpawnIdStore.LookupByKey(guid);

                                                  foreach (var creature in creatureBounds)
                                                      creature.Location.AddObjectToRemoveList();
                                              });
        }

        if (internalEventID >= GameEventGameobjectGuids.Length)
        {
            Log.Logger.Error("GameEventMgr.GameEventUnspawn attempt access to out of range GameEventGameobjectGuids element {0} (size: {1})",
                             internalEventID,
                             GameEventGameobjectGuids.Length);

            return;
        }

        foreach (var guid in GameEventGameobjectGuids[internalEventID])
        {
            // check if it's needed by another event, if so, don't remove
            if (eventID > 0 && HasGameObjectActiveEventExcept(guid, (ushort)eventID))
                continue;

            // Remove the gameobject from grid
            var data = _objectManager.GetGameObjectData(guid);

            if (data == null)
                continue;

            _objectManager.RemoveGameObjectFromGrid(data);

            _mapManager.DoForAllMapsWithMapId(data.MapId,
                                              map =>
                                              {
                                                  map.RemoveRespawnTime(SpawnObjectType.GameObject, guid);
                                                  var gameobjectBounds = map.GameObjectBySpawnIdStore.LookupByKey(guid);

                                                  foreach (var go in gameobjectBounds)
                                                      go.Location.AddObjectToRemoveList();
                                              });
        }

        if (internalEventID >= _gameEventPoolIds.Length)
        {
            Log.Logger.Error("GameEventMgr.GameEventUnspawn attempt access to out of range _gameEventPoolIds element {0} (size: {1})", internalEventID, _gameEventPoolIds.Length);

            return;
        }

        foreach (var poolId in _gameEventPoolIds[internalEventID])
        {
            var poolTemplate = _poolManager.GetPoolTemplate(poolId);

            if (poolTemplate != null)
                _mapManager.DoForAllMapsWithMapId((uint)poolTemplate.MapId, map => { _poolManager.DespawnPool(map.PoolData, poolId, true); });
        }
    }

    private long GetLastStartTime(ushort eventID)
    {
        if (eventID >= _gameEvent.Length)
            return 0;

        if (_gameEvent[eventID].State != GameEventState.Normal)
            return 0;

        var now = GameTime.SystemTime;
        var eventInitialStart = Time.UnixTimeToDateTime(_gameEvent[eventID].Start);
        var occurence = TimeSpan.FromMinutes(_gameEvent[eventID].Occurence);
        var durationSinceLastStart = TimeSpan.FromTicks((now - eventInitialStart).Ticks % occurence.Ticks);

        return Time.DateTimeToUnixTime(now - durationSinceLastStart);
    }

    private bool HasCreatureActiveEventExcept(ulong creatureId, ushort eventId)
    {
        foreach (var activeEventId in _activeEvents)
            if (activeEventId != eventId)
            {
                var internalEventID = _gameEvent.Length + activeEventId - 1;

                foreach (var id in GameEventCreatureGuids[internalEventID])
                    if (id == creatureId)
                        return true;
            }

        return false;
    }

    private bool HasCreatureQuestActiveEventExcept(uint questId, ushort eventId)
    {
        foreach (var activeEventId in _activeEvents)
            if (activeEventId != eventId)
                if (_gameEventCreatureQuests[activeEventId].Any(pair => pair.Item2 == questId))
                    return true;

        return false;
    }

    private bool HasGameObjectActiveEventExcept(ulong goId, ushort eventId)
    {
        foreach (var activeEventId in _activeEvents)
            if (activeEventId != eventId)
            {
                var internalEventID = _gameEvent.Length + activeEventId - 1;

                if (GameEventGameobjectGuids[internalEventID].Any(id => id == goId))
                    return true;
            }

        return false;
    }

    private bool HasGameObjectQuestActiveEventExcept(uint questId, ushort eventId)
    {
        foreach (var activeEventId in _activeEvents)
            if (activeEventId != eventId)
                if (_gameEventGameObjectQuests[activeEventId].Any(pair => pair.Item2 == questId))
                    return true;

        return false;
    }

    private void RemoveActiveEvent(ushort eventID)
    {
        _activeEvents.Remove(eventID);
    }

    private void RunSmartAIScripts(ushort eventID, bool activate)
    {
        //! Iterate over every supported source type (creature and gameobject)
        //! Not entirely sure how this will affect units in non-loaded grids.
        _mapManager.DoForAllMaps(map =>
        {
            GameEventAIHookWorker worker = new(eventID, activate);

            worker.Visit(map.ObjectsStore.Values.ToList());
        });
    }

    private void SaveWorldEventStateToDB(ushort eventID)
    {
        SQLTransaction trans = new();

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GAME_EVENT_SAVE);
        stmt.AddValue(0, eventID);
        trans.Append(stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_GAME_EVENT_SAVE);
        stmt.AddValue(0, eventID);
        stmt.AddValue(1, (byte)_gameEvent[eventID].State);
        stmt.AddValue(2, _gameEvent[eventID].Nextstart != 0 ? _gameEvent[eventID].Nextstart : 0L);
        trans.Append(stmt);
        _characterDatabase.CommitTransaction(trans);
    }

    private void SetHolidayEventTime(GameEventData gameEvent)
    {
        if (gameEvent.HolidayStage == 0) // Ignore holiday
            return;

        var holiday = _cliDB.HolidaysStorage.LookupByKey((uint)gameEvent.HolidayID);

        if (holiday.Date[0] == 0 || holiday.Duration[0] == 0) // Invalid definitions
        {
            Log.Logger.Error($"Missing date or duration for holiday {gameEvent.HolidayID}.");

            return;
        }

        var stageIndex = (byte)(gameEvent.HolidayStage - 1);
        gameEvent.Length = (uint)(holiday.Duration[stageIndex] * Time.HOUR / Time.MINUTE);

        long stageOffset = 0;

        for (var i = 0; i < stageIndex; ++i)
            stageOffset += holiday.Duration[i] * Time.HOUR;

        switch (holiday.CalendarFilterType)
        {
            case -1:                                           // Yearly
                gameEvent.Occurence = Time.YEAR / Time.MINUTE; // Not all too useful

                break;
            case 0: // Weekly
                gameEvent.Occurence = Time.WEEK / Time.MINUTE;

                break;
            case 1: // Defined dates only (Darkmoon Faire)
                break;
            case 2: // Only used for looping events (Call to Arms)
                break;
        }

        if (holiday.Looping != 0)
        {
            gameEvent.Occurence = 0;

            for (var i = 0; i < SharedConst.MaxHolidayDurations && holiday.Duration[i] != 0; ++i)
                gameEvent.Occurence += (uint)(holiday.Duration[i] * Time.HOUR / Time.MINUTE);
        }

        var singleDate = ((holiday.Date[0] >> 24) & 0x1F) == 31; // Events with fixed date within year have - 1

        var curTime = GameTime.CurrentTime;

        for (var i = 0; i < SharedConst.MaxHolidayDates && holiday.Date[i] != 0; ++i)
        {
            var date = holiday.Date[i];

            int year;

            if (singleDate)
                year = Time.UnixTimeToDateTime(curTime).ToLocalTime().Year - 1; // First try last year (event active through New Year)
            else
                year = (int)((date >> 24) & 0x1F) + 100 + 1900;

            var timeInfo = new DateTime(year, (int)((date >> 20) & 0xF) + 1, (int)((date >> 14) & 0x3F) + 1, (int)((date >> 6) & 0x1F), (int)(date & 0x3F), 0);

            var startTime = Time.DateTimeToUnixTime(timeInfo);

            if (curTime < startTime + gameEvent.Length * Time.MINUTE)
            {
                gameEvent.Start = startTime + stageOffset;

                break;
            }

            if (singleDate)
            {
                var tmCopy = timeInfo.AddYears(Time.UnixTimeToDateTime(curTime).ToLocalTime().Year); // This year
                gameEvent.Start = Time.DateTimeToUnixTime(tmCopy) + stageOffset;

                break;
            }
            // date is due and not a singleDate event, try with next DBC date (modified by holiday_dates)
            // if none is found we don't modify start date and use the one in game_event
        }
    }

    private void UnApplyEvent(ushort eventID)
    {
        Log.Logger.Information("GameEvent {0} \"{1}\" removed.", eventID, _gameEvent[eventID].Description);
        //! Run SAI scripts with SMART_EVENT_GAME_EVENT_END
        RunSmartAIScripts(eventID, false);
        // un-spawn positive event tagged objects
        GameEventUnspawn((short)eventID);
        // spawn negative event tagget objects
        var eventNid = (short)(-1 * eventID);
        GameEventSpawn(eventNid);
        // restore equipment or model
        ChangeEquipOrModel((short)eventID, false);
        // Remove quests that are events only to non event npc
        UpdateEventQuests(eventID, false);
        UpdateWorldStates(eventID, false);
        // update npcflags in this event
        UpdateEventNPCFlags(eventID);
        // remove vendor items
        UpdateEventNPCVendor(eventID, false);
        // update bg holiday
        UpdateBattlegroundSettings();
    }

    private void UpdateBattlegroundSettings()
    {
        _battlegroundManager.ResetHolidays();

        foreach (var activeEventId in _activeEvents)
            _battlegroundManager.SetHolidayActive(_gameEventBattlegroundHolidays[activeEventId]);
    }

    private void UpdateEventNPCFlags(ushort eventID)
    {
        MultiMap<uint, ulong> creaturesByMap = new();

        // go through the creatures whose npcflags are changed in the event
        foreach (var (guid, _) in _gameEventNpcFlags[eventID])
        {
            // get the creature data from the low guid to get the entry, to be able to find out the whole guid
            var data = _objectManager.GetCreatureData(guid);

            if (data != null)
                creaturesByMap.Add(data.MapId, guid);
        }

        foreach (var key in creaturesByMap.Keys)
            _mapManager.DoForAllMapsWithMapId(key,
                                              map =>
                                              {
                                                  foreach (var spawnId in creaturesByMap[key])
                                                  {
                                                      var creatureBounds = map.CreatureBySpawnIdStore.LookupByKey(spawnId);

                                                      foreach (var creature in creatureBounds)
                                                      {
                                                          var npcflag = GetNPCFlag(creature);
                                                          var creatureTemplate = creature.Template;

                                                          if (creatureTemplate != null)
                                                              npcflag |= creatureTemplate.Npcflag;

                                                          creature.ReplaceAllNpcFlags((NPCFlags)(npcflag & 0xFFFFFFFF));
                                                          creature.ReplaceAllNpcFlags2((NPCFlags2)(npcflag >> 32));
                                                          // reset gossip options, since the Id change might have added / removed some
                                                          //cr.ResetGossipOptions();
                                                      }
                                                  }
                                              });
    }

    private void UpdateEventNPCVendor(ushort eventId, bool activate)
    {
        foreach (var npcEventVendor in _gameEventVendors[eventId])
            if (activate)
                _objectManager.VendorItemCache.AddVendorItem(npcEventVendor.Key, npcEventVendor.Value, false);
            else
                _objectManager.VendorItemCache.RemoveVendorItem(npcEventVendor.Key, npcEventVendor.Value.Item, npcEventVendor.Value.Type, false);
    }

    private void UpdateEventQuests(ushort eventId, bool activate)
    {
        foreach (var pair in _gameEventCreatureQuests[eventId])
        {
            var creatureQuestMap = _objectManager.GetCreatureQuestRelationMapHack();

            if (activate) // Add the pair(id, quest) to the multimap
                creatureQuestMap.Add(pair.Item1, pair.Item2);
            else
            {
                if (!HasCreatureQuestActiveEventExcept(pair.Item2, eventId))
                    // Remove the pair(id, quest) from the multimap
                    creatureQuestMap.Remove(pair.Item1, pair.Item2);
            }
        }

        foreach (var pair in _gameEventGameObjectQuests[eventId])
        {
            var gameObjectQuestMap = _objectManager.GetGOQuestRelationMapHack();

            if (activate) // Add the pair(id, quest) to the multimap
                gameObjectQuestMap.Add(pair.Item1, pair.Item2);
            else
            {
                if (!HasGameObjectQuestActiveEventExcept(pair.Item2, eventId))
                    // Remove the pair(id, quest) from the multimap
                    gameObjectQuestMap.Remove(pair.Item1, pair.Item2);
            }
        }
    }

    private void UpdateWorldStates(ushort eventID, bool activate)
    {
        var ev = _gameEvent[eventID];

        if (ev.HolidayID == HolidayIds.None)
            return;

        var bgTypeId = _battlegroundManager.WeekendHolidayIdToBGType(ev.HolidayID);

        if (bgTypeId == BattlegroundTypeId.None)
            return;

        if (!_cliDB.BattlemasterListStorage.TryGetValue((uint)_battlegroundManager.WeekendHolidayIdToBGType(ev.HolidayID), out var bl))
            return;

        if (bl.HolidayWorldState != 0)
            _worldStateManager.SetValue(bl.HolidayWorldState, activate ? 1 : 0, false, null);
    }
}