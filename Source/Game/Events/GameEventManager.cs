﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;

namespace Game
{
    public class GameEventManager : Singleton<GameEventManager>
    {
        public List<ulong>[] mGameEventCreatureGuids;
        public List<ulong>[] mGameEventGameobjectGuids;
        private readonly List<ushort> _ActiveEvents = new();
        private readonly Dictionary<uint, GameEventQuestToEventConditionNum> mQuestToEventConditions = new();
        private bool isSystemInit;
        private GameEventData[] mGameEvent;
        private uint[] mGameEventBattlegroundHolidays;

        private List<Tuple<uint, uint>>[] mGameEventCreatureQuests;
        private List<Tuple<uint, uint>>[] mGameEventGameObjectQuests;
        private List<Tuple<ulong, ModelEquip>>[] mGameEventModelEquip;
        private List<(ulong guid, ulong npcflag)>[] mGameEventNPCFlags;
        private List<uint>[] mGameEventPoolIds;
        private Dictionary<uint, VendorItem>[] mGameEventVendors;

        private GameEventManager()
        {
        }

        public uint NextCheck(ushort entry)
        {
            long currenttime = GameTime.GetGameTime();

            // for NEXTPHASE State world events, return the delay to start the next event, so the followup event will be checked correctly
            if ((mGameEvent[entry].State == GameEventState.WorldNextPhase || mGameEvent[entry].State == GameEventState.WorldFinished) &&
                mGameEvent[entry].Nextstart >= currenttime)
                return (uint)(mGameEvent[entry].Nextstart - currenttime);

            // for CONDITIONS State world events, return the length of the wait period, so if the conditions are met, this check will be called again to set the timer as NEXTPHASE event
            if (mGameEvent[entry].State == GameEventState.WorldConditions)
            {
                if (mGameEvent[entry].Length != 0)
                    return mGameEvent[entry].Length * 60;
                else
                    return Time.Day;
            }

            // outdated event: we return max
            if (currenttime > mGameEvent[entry].End)
                return Time.Day;

            // never started event, we return delay before start
            if (mGameEvent[entry].Start > currenttime)
                return (uint)(mGameEvent[entry].Start - currenttime);

            uint delay;

            // in event, we return the end of it
            if ((((currenttime - mGameEvent[entry].Start) % (mGameEvent[entry].Occurence * 60)) < (mGameEvent[entry].Length * 60)))
                // we return the delay before it ends
                delay = (uint)((mGameEvent[entry].Length * Time.Minute) - ((currenttime - mGameEvent[entry].Start) % (mGameEvent[entry].Occurence * Time.Minute)));
            else // not in window, we return the delay before next start
                delay = (uint)((mGameEvent[entry].Occurence * Time.Minute) - ((currenttime - mGameEvent[entry].Start) % (mGameEvent[entry].Occurence * Time.Minute)));

            // In case the end is before next check
            if (mGameEvent[entry].End < currenttime + delay)
                return (uint)(mGameEvent[entry].End - currenttime);
            else
                return delay;
        }

        public bool StartEvent(ushort event_id, bool overwrite = false)
        {
            GameEventData data = mGameEvent[event_id];

            if (data.State == GameEventState.Normal ||
                data.State == GameEventState.Internal)
            {
                AddActiveEvent(event_id);
                ApplyNewEvent(event_id);

                if (overwrite)
                {
                    mGameEvent[event_id].Start = GameTime.GetGameTime();

                    if (data.End <= data.Start)
                        data.End = data.Start + data.Length;
                }

                return false;
            }
            else
            {
                if (data.State == GameEventState.WorldInactive)
                    // set to conditions phase
                    data.State = GameEventState.WorldConditions;

                // add to active events
                AddActiveEvent(event_id);
                // add spawns
                ApplyNewEvent(event_id);

                // check if can go to next State
                bool conditions_met = CheckOneGameEventConditions(event_id);
                // save to db
                SaveWorldEventStateToDB(event_id);

                // Force game event update to set the update timer if conditions were met from a command
                // this update is needed to possibly start events dependent on the started one
                // or to scedule another update where the next event will be started
                if (overwrite && conditions_met)
                    Global.WorldMgr.ForceGameEventUpdate();

                return conditions_met;
            }
        }

        public void StopEvent(ushort event_id, bool overwrite = false)
        {
            GameEventData data = mGameEvent[event_id];
            bool serverwide_evt = data.State != GameEventState.Normal && data.State != GameEventState.Internal;

            RemoveActiveEvent(event_id);
            UnApplyEvent(event_id);

            if (overwrite && !serverwide_evt)
            {
                data.Start = GameTime.GetGameTime() - data.Length * Time.Minute;

                if (data.End <= data.Start)
                    data.End = data.Start + data.Length;
            }
            else if (serverwide_evt)
            {
                // if finished world event, then only gm command can stop it
                if (overwrite || data.State != GameEventState.WorldFinished)
                {
                    // reset conditions
                    data.Nextstart = 0;
                    data.State = GameEventState.WorldInactive;

                    foreach (var pair in data.Conditions)
                        pair.Value.Done = 0;

                    SQLTransaction trans = new();
                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ALL_GAME_EVENT_CONDITION_SAVE);
                    stmt.AddValue(0, event_id);
                    trans.Append(stmt);

                    stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GAME_EVENT_SAVE);
                    stmt.AddValue(0, event_id);
                    trans.Append(stmt);

                    DB.Characters.CommitTransaction(trans);
                }
            }
        }

        public void LoadFromDB()
        {
            {
                uint oldMSTime = Time.GetMSTime();
                //                                         0           1                           2                         3          4       5        6            7            8             9
                SQLResult result = DB.World.Query("SELECT eventEntry, UNIX_TIMESTAMP(start_time), UNIX_TIMESTAMP(end_time), occurence, length, holiday, holidayStage, description, world_event, announce FROM game_event");

                if (result.IsEmpty())
                {
                    mGameEvent.Clear();
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 game events. DB table `game_event` is empty.");

                    return;
                }

                uint count = 0;

                do
                {
                    byte event_id = result.Read<byte>(0);

                    if (event_id == 0)
                    {
                        Log.outError(LogFilter.Sql, "`game_event` game event entry 0 is reserved and can't be used.");

                        continue;
                    }

                    GameEventData pGameEvent = new();
                    ulong starttime = result.Read<ulong>(1);
                    pGameEvent.Start = (long)starttime;
                    ulong endtime = result.Read<ulong>(2);
                    pGameEvent.End = (long)endtime;
                    pGameEvent.Occurence = result.Read<uint>(3);
                    pGameEvent.Length = result.Read<uint>(4);
                    pGameEvent.Holiday_id = (HolidayIds)result.Read<uint>(5);

                    pGameEvent.HolidayStage = result.Read<byte>(6);
                    pGameEvent.Description = result.Read<string>(7);
                    pGameEvent.State = (GameEventState)result.Read<byte>(8);
                    pGameEvent.Announce = result.Read<byte>(9);
                    pGameEvent.Nextstart = 0;

                    ++count;

                    if (pGameEvent.Length == 0 &&
                        pGameEvent.State == GameEventState.Normal) // length>0 is validity check
                    {
                        Log.outError(LogFilter.Sql, $"`game_event` game event Id ({event_id}) isn't a world event and has length = 0, thus it can't be used.");

                        continue;
                    }

                    if (pGameEvent.Holiday_id != HolidayIds.None)
                    {
                        if (!CliDB.HolidaysStorage.ContainsKey((uint)pGameEvent.Holiday_id))
                        {
                            Log.outError(LogFilter.Sql, $"`game_event` game event Id ({event_id}) contains nonexisting holiday Id {pGameEvent.Holiday_id}.");
                            pGameEvent.Holiday_id = HolidayIds.None;

                            continue;
                        }

                        if (pGameEvent.HolidayStage > SharedConst.MaxHolidayDurations)
                        {
                            Log.outError(LogFilter.Sql, "`game_event` game event Id ({event_id}) has out of range holidayStage {pGameEvent.holidayStage}.");
                            pGameEvent.HolidayStage = 0;

                            continue;
                        }

                        SetHolidayEventTime(pGameEvent);
                    }

                    mGameEvent[event_id] = pGameEvent;
                } while (result.NextRow());

                Log.outInfo(LogFilter.ServerLoading, "Loaded {0} game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }

            Log.outInfo(LogFilter.ServerLoading, "Loading Game Event Saves Data...");

            {
                uint oldMSTime = Time.GetMSTime();

                //                                                       0       1        2
                SQLResult result = DB.Characters.Query("SELECT eventEntry, State, next_start FROM game_event_save");

                if (result.IsEmpty())
                {
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 game event saves in game events. DB table `game_event_save` is empty.");
                }
                else
                {
                    uint count = 0;

                    do
                    {
                        byte event_id = result.Read<byte>(0);

                        if (event_id >= mGameEvent.Length)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_save` game event entry ({0}) not exist in `game_event`", event_id);

                            continue;
                        }

                        if (mGameEvent[event_id].State != GameEventState.Normal &&
                            mGameEvent[event_id].State != GameEventState.Internal)
                        {
                            mGameEvent[event_id].State = (GameEventState)result.Read<byte>(1);
                            mGameEvent[event_id].Nextstart = result.Read<uint>(2);
                        }
                        else
                        {
                            Log.outError(LogFilter.Sql, "game_event_save includes event save for non-worldevent Id {0}", event_id);

                            continue;
                        }

                        ++count;
                    } while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Loaded {0} game event saves in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loading Game Event Prerequisite Data...");

            {
                uint oldMSTime = Time.GetMSTime();

                //                                                   0             1
                SQLResult result = DB.World.Query("SELECT eventEntry, prerequisite_event FROM game_event_prerequisite");

                if (result.IsEmpty())
                {
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 game event prerequisites in game events. DB table `game_event_prerequisite` is empty.");
                }
                else
                {
                    uint count = 0;

                    do
                    {
                        ushort event_id = result.Read<byte>(0);

                        if (event_id >= mGameEvent.Length)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_prerequisite` game event Id ({0}) is out of range compared to max event Id in `game_event`", event_id);

                            continue;
                        }

                        if (mGameEvent[event_id].State != GameEventState.Normal &&
                            mGameEvent[event_id].State != GameEventState.Internal)
                        {
                            ushort prerequisite_event = result.Read<byte>(1);

                            if (prerequisite_event >= mGameEvent.Length)
                            {
                                Log.outError(LogFilter.Sql, "`game_event_prerequisite` game event prerequisite Id ({0}) not exist in `game_event`", prerequisite_event);

                                continue;
                            }

                            mGameEvent[event_id].Prerequisite_events.Add(prerequisite_event);
                        }
                        else
                        {
                            Log.outError(LogFilter.Sql, "game_event_prerequisiste includes event entry for non-worldevent Id {0}", event_id);

                            continue;
                        }

                        ++count;
                    } while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Loaded {0} game event prerequisites in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loading Game Event Creature Data...");

            {
                uint oldMSTime = Time.GetMSTime();

                //                                                 0        1
                SQLResult result = DB.World.Query("SELECT Guid, eventEntry FROM game_event_creature");

                if (result.IsEmpty())
                {
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 creatures in game events. DB table `game_event_creature` is empty");
                }
                else
                {
                    uint count = 0;

                    do
                    {
                        ulong guid = result.Read<ulong>(0);
                        short event_id = result.Read<sbyte>(1);
                        int internal_event_id = mGameEvent.Length + event_id - 1;

                        CreatureData data = Global.ObjectMgr.GetCreatureData(guid);

                        if (data == null)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_creature` contains creature (GUID: {0}) not found in `creature` table.", guid);

                            continue;
                        }

                        if (internal_event_id < 0 ||
                            internal_event_id >= mGameEventCreatureGuids.Length)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_creature` game event Id ({0}) not exist in `game_event`", event_id);

                            continue;
                        }

                        // Log error for pooled object, but still spawn it
                        if (data.poolId != 0)
                            Log.outError(LogFilter.Sql, $"`game_event_creature`: game event Id ({event_id}) contains creature ({guid}) which is part of a pool ({data.poolId}). This should be spawned in game_event_pool");

                        mGameEventCreatureGuids[internal_event_id].Add(guid);

                        ++count;
                    } while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Loaded {0} creatures in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loading Game Event GO Data...");

            {
                uint oldMSTime = Time.GetMSTime();

                //                                                0         1
                SQLResult result = DB.World.Query("SELECT Guid, eventEntry FROM game_event_gameobject");

                if (result.IsEmpty())
                {
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 gameobjects in game events. DB table `game_event_gameobject` is empty.");
                }
                else
                {
                    uint count = 0;

                    do
                    {
                        ulong guid = result.Read<ulong>(0);
                        short event_id = result.Read<byte>(1);
                        int internal_event_id = mGameEvent.Length + event_id - 1;

                        GameObjectData data = Global.ObjectMgr.GetGameObjectData(guid);

                        if (data == null)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_gameobject` contains gameobject (GUID: {0}) not found in `gameobject` table.", guid);

                            continue;
                        }

                        if (internal_event_id < 0 ||
                            internal_event_id >= mGameEventGameobjectGuids.Length)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_gameobject` game event Id ({0}) not exist in `game_event`", event_id);

                            continue;
                        }

                        // Log error for pooled object, but still spawn it
                        if (data.poolId != 0)
                            Log.outError(LogFilter.Sql, $"`game_event_gameobject`: game event Id ({event_id}) contains game object ({guid}) which is part of a pool ({data.poolId}). This should be spawned in game_event_pool");

                        mGameEventGameobjectGuids[internal_event_id].Add(guid);

                        ++count;
                    } while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Loaded {0} gameobjects in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loading Game Event Model/Equipment Change Data...");

            {
                uint oldMSTime = Time.GetMSTime();

                //                                                       0           1                       2                                 3                                     4
                SQLResult result = DB.World.Query("SELECT creature.Guid, creature.Id, game_event_model_equip.eventEntry, game_event_model_equip.modelid, game_event_model_equip.equipment_id " +
                                                  "FROM creature JOIN game_event_model_equip ON creature.Guid=game_event_model_equip.Guid");

                if (result.IsEmpty())
                {
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 model/equipment changes in game events. DB table `game_event_model_equip` is empty.");
                }
                else
                {
                    uint count = 0;

                    do
                    {
                        ulong guid = result.Read<ulong>(0);
                        uint entry = result.Read<uint>(1);
                        ushort event_id = result.Read<byte>(2);

                        if (event_id >= mGameEventModelEquip.Length)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_model_equip` game event Id ({0}) is out of range compared to max event Id in `game_event`", event_id);

                            continue;
                        }

                        ModelEquip newModelEquipSet = new();
                        newModelEquipSet.Modelid = result.Read<uint>(3);
                        newModelEquipSet.Equipment_id = result.Read<byte>(4);
                        newModelEquipSet.Equipement_id_prev = 0;
                        newModelEquipSet.Modelid_prev = 0;

                        if (newModelEquipSet.Equipment_id > 0)
                        {
                            sbyte equipId = (sbyte)newModelEquipSet.Equipment_id;

                            if (Global.ObjectMgr.GetEquipmentInfo(entry, equipId) == null)
                            {
                                Log.outError(LogFilter.Sql,
                                             "Table `game_event_model_equip` have creature (Guid: {0}, entry: {1}) with equipment_id {2} not found in table `creature_equip_template`, set to no equipment.",
                                             guid,
                                             entry,
                                             newModelEquipSet.Equipment_id);

                                continue;
                            }
                        }

                        mGameEventModelEquip[event_id].Add(Tuple.Create(guid, newModelEquipSet));

                        ++count;
                    } while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Loaded {0} model/equipment changes in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loading Game Event Quest Data...");

            {
                uint oldMSTime = Time.GetMSTime();

                //                                               0     1      2
                SQLResult result = DB.World.Query("SELECT Id, quest, eventEntry FROM game_event_creature_quest");

                if (result.IsEmpty())
                {
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 quests additions in game events. DB table `game_event_creature_quest` is empty.");
                }
                else
                {
                    uint count = 0;

                    do
                    {
                        uint id = result.Read<uint>(0);
                        uint quest = result.Read<uint>(1);
                        ushort event_id = result.Read<byte>(2);

                        if (event_id >= mGameEventCreatureQuests.Length)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_creature_quest` game event Id ({0}) not exist in `game_event`", event_id);

                            continue;
                        }

                        mGameEventCreatureQuests[event_id].Add(Tuple.Create(id, quest));

                        ++count;
                    } while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Loaded {0} quests additions in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loading Game Event GO Quest Data...");

            {
                uint oldMSTime = Time.GetMSTime();

                //                                               0     1      2
                SQLResult result = DB.World.Query("SELECT Id, quest, eventEntry FROM game_event_gameobject_quest");

                if (result.IsEmpty())
                {
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 go quests additions in game events. DB table `game_event_gameobject_quest` is empty.");
                }
                else
                {
                    uint count = 0;

                    do
                    {
                        uint id = result.Read<uint>(0);
                        uint quest = result.Read<uint>(1);
                        ushort event_id = result.Read<byte>(2);

                        if (event_id >= mGameEventGameObjectQuests.Length)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_gameobject_quest` game event Id ({0}) not exist in `game_event`", event_id);

                            continue;
                        }

                        mGameEventGameObjectQuests[event_id].Add(Tuple.Create(id, quest));

                        ++count;
                    } while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Loaded {0} quests additions in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loading Game Event Quest Condition Data...");

            {
                uint oldMSTime = Time.GetMSTime();

                //                                                 0       1         2             3
                SQLResult result = DB.World.Query("SELECT quest, eventEntry, condition_id, num FROM game_event_quest_condition");

                if (result.IsEmpty())
                {
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 quest event conditions in game events. DB table `game_event_quest_condition` is empty.");
                }
                else
                {
                    uint count = 0;

                    do
                    {
                        uint quest = result.Read<uint>(0);
                        ushort event_id = result.Read<byte>(1);
                        uint condition = result.Read<uint>(2);
                        float num = result.Read<float>(3);

                        if (event_id >= mGameEvent.Length)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_quest_condition` game event Id ({0}) is out of range compared to max event Id in `game_event`", event_id);

                            continue;
                        }

                        if (!mQuestToEventConditions.ContainsKey(quest))
                            mQuestToEventConditions[quest] = new GameEventQuestToEventConditionNum();

                        mQuestToEventConditions[quest].Event_id = event_id;
                        mQuestToEventConditions[quest].Condition = condition;
                        mQuestToEventConditions[quest].Num = num;

                        ++count;
                    } while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Loaded {0} quest event conditions in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loading Game Event Condition Data...");

            {
                uint oldMSTime = Time.GetMSTime();

                //                                                  0          1            2             3                      4
                SQLResult result = DB.World.Query("SELECT eventEntry, condition_id, req_num, max_world_state_field, done_world_state_field FROM game_event_condition");

                if (result.IsEmpty())
                {
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 conditions in game events. DB table `game_event_condition` is empty.");
                }
                else
                {
                    uint count = 0;

                    do
                    {
                        ushort event_id = result.Read<byte>(0);
                        uint condition = result.Read<uint>(1);

                        if (event_id >= mGameEvent.Length)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_condition` game event Id ({0}) is out of range compared to max event Id in `game_event`", event_id);

                            continue;
                        }

                        mGameEvent[event_id].Conditions[condition].ReqNum = result.Read<float>(2);
                        mGameEvent[event_id].Conditions[condition].Done = 0;
                        mGameEvent[event_id].Conditions[condition].Max_world_state = result.Read<ushort>(3);
                        mGameEvent[event_id].Conditions[condition].Done_world_state = result.Read<ushort>(4);

                        ++count;
                    } while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Loaded {0} conditions in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loading Game Event Condition Save Data...");

            {
                uint oldMSTime = Time.GetMSTime();

                //                                                      0           1         2
                SQLResult result = DB.Characters.Query("SELECT eventEntry, condition_id, done FROM game_event_condition_save");

                if (result.IsEmpty())
                {
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 condition saves in game events. DB table `game_event_condition_save` is empty.");
                }
                else
                {
                    uint count = 0;

                    do
                    {
                        ushort event_id = result.Read<byte>(0);
                        uint condition = result.Read<uint>(1);

                        if (event_id >= mGameEvent.Length)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_condition_save` game event Id ({0}) is out of range compared to max event Id in `game_event`", event_id);

                            continue;
                        }

                        if (mGameEvent[event_id].Conditions.ContainsKey(condition))
                        {
                            mGameEvent[event_id].Conditions[condition].Done = result.Read<uint>(2);
                        }
                        else
                        {
                            Log.outError(LogFilter.Sql, "game_event_condition_save contains not present condition evt Id {0} cond Id {1}", event_id, condition);

                            continue;
                        }

                        ++count;
                    } while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Loaded {0} condition saves in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loading Game Event NPCflag Data...");

            {
                uint oldMSTime = Time.GetMSTime();

                //                                                0       1        2
                SQLResult result = DB.World.Query("SELECT Guid, eventEntry, Npcflag FROM game_event_npcflag");

                if (result.IsEmpty())
                {
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 npcflags in game events. DB table `game_event_npcflag` is empty.");
                }
                else
                {
                    uint count = 0;

                    do
                    {
                        ulong guid = result.Read<ulong>(0);
                        ushort event_id = result.Read<byte>(1);
                        ulong npcflag = result.Read<ulong>(2);

                        if (event_id >= mGameEvent.Length)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_npcflag` game event Id ({0}) is out of range compared to max event Id in `game_event`", event_id);

                            continue;
                        }

                        mGameEventNPCFlags[event_id].Add((guid, npcflag));

                        ++count;
                    } while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Loaded {0} npcflags in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loading Game Event Seasonal Quest Relations...");

            {
                uint oldMSTime = Time.GetMSTime();

                //                                                  0          1
                SQLResult result = DB.World.Query("SELECT questId, eventEntry FROM game_event_seasonal_questrelation");

                if (result.IsEmpty())
                {
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 seasonal quests additions in game events. DB table `game_event_seasonal_questrelation` is empty.");
                }
                else
                {
                    uint count = 0;

                    do
                    {
                        uint questId = result.Read<uint>(0);
                        ushort eventEntry = result.Read<byte>(1); // @todo Change to byte

                        Quest questTemplate = Global.ObjectMgr.GetQuestTemplate(questId);

                        if (questTemplate == null)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_seasonal_questrelation` quest Id ({0}) does not exist in `quest_template`", questId);

                            continue;
                        }

                        if (eventEntry >= mGameEvent.Length)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_seasonal_questrelation` event Id ({0}) not exist in `game_event`", eventEntry);

                            continue;
                        }

                        questTemplate.SetEventIdForQuest(eventEntry);
                        ++count;
                    } while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Loaded {0} quests additions in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loading Game Event Vendor Additions Data...");

            {
                uint oldMSTime = Time.GetMSTime();

                //                                               0           1     2     3         4         5             6     7             8                  9
                SQLResult result = DB.World.Query("SELECT eventEntry, Guid, Item, Maxcount, incrtime, ExtendedCost, Type, BonusListIDs, PlayerConditionId, IgnoreFiltering FROM game_event_npc_vendor ORDER BY Guid, Slot ASC");

                if (result.IsEmpty())
                {
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 vendor additions in game events. DB table `game_event_npc_vendor` is empty.");
                }
                else
                {
                    uint count = 0;

                    do
                    {
                        byte event_id = result.Read<byte>(0);
                        ulong guid = result.Read<ulong>(1);

                        if (event_id >= mGameEventVendors.Length)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_npc_vendor` game event Id ({0}) not exist in `game_event`", event_id);

                            continue;
                        }

                        // get the event npc flag for checking if the npc will be vendor during the event or not
                        ulong event_npc_flag = 0;
                        var flist = mGameEventNPCFlags[event_id];

                        foreach (var pair in flist)
                            if (pair.guid == guid)
                            {
                                event_npc_flag = pair.npcflag;

                                break;
                            }

                        // get creature entry
                        uint entry = 0;
                        CreatureData data = Global.ObjectMgr.GetCreatureData(guid);

                        if (data != null)
                            entry = data.Id;

                        VendorItem vItem = new();
                        vItem.Item = result.Read<uint>(2);
                        vItem.Maxcount = result.Read<uint>(3);
                        vItem.Incrtime = result.Read<uint>(4);
                        vItem.ExtendedCost = result.Read<uint>(5);
                        vItem.Type = (ItemVendorType)result.Read<byte>(6);
                        vItem.PlayerConditionId = result.Read<uint>(8);
                        vItem.IgnoreFiltering = result.Read<bool>(9);

                        var bonusListIDsTok = new StringArray(result.Read<string>(7), ' ');

                        if (!bonusListIDsTok.IsEmpty())
                            foreach (uint token in bonusListIDsTok)
                                vItem.BonusListIDs.Add(token);

                        // check validity with event's Npcflag
                        if (!Global.ObjectMgr.IsVendorItemValid(entry, vItem, null, null, event_npc_flag))
                            continue;

                        mGameEventVendors[event_id].Add(entry, vItem);

                        ++count;
                    } while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Loaded {0} vendor additions in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loading Game Event Battleground Holiday Data...");

            {
                uint oldMSTime = Time.GetMSTime();

                //                                         0           1
                SQLResult result = DB.World.Query("SELECT EventEntry, BattlegroundID FROM game_event_battleground_holiday");

                if (result.IsEmpty())
                {
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 Battlegroundholidays in game events. DB table `game_event_battleground_holiday` is empty.");
                }
                else
                {
                    uint count = 0;

                    do
                    {
                        ushort eventId = result.Read<byte>(0);

                        if (eventId >= mGameEvent.Length)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_battleground_holiday` game event Id ({0}) not exist in `game_event`", eventId);

                            continue;
                        }

                        mGameEventBattlegroundHolidays[eventId] = result.Read<uint>(1);

                        ++count;
                    } while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Loaded {0} Battlegroundholidays in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loading Game Event Pool Data...");

            {
                uint oldMSTime = Time.GetMSTime();

                //                                                               0                         1
                SQLResult result = DB.World.Query("SELECT pool_template.entry, game_event_pool.eventEntry FROM pool_template" +
                                                  " JOIN game_event_pool ON pool_template.entry = game_event_pool.pool_entry");

                if (result.IsEmpty())
                {
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 pools for game events. DB table `game_event_pool` is empty.");
                }
                else
                {
                    uint count = 0;

                    do
                    {
                        uint entry = result.Read<uint>(0);
                        short event_id = result.Read<sbyte>(1);
                        int internal_event_id = mGameEvent.Length + event_id - 1;

                        if (internal_event_id < 0 ||
                            internal_event_id >= mGameEventPoolIds.Length)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_pool` game event Id ({0}) not exist in `game_event`", event_id);

                            continue;
                        }

                        if (!Global.PoolMgr.CheckPool(entry))
                        {
                            Log.outError(LogFilter.Sql, "Pool Id ({0}) has all creatures or gameobjects with explicit chance sum <>100 and no equal chance defined. The pool system cannot pick one to spawn.", entry);

                            continue;
                        }


                        mGameEventPoolIds[internal_event_id].Add(entry);

                        ++count;
                    } while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Loaded {0} pools for game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
                }
            }
        }

        public ulong GetNPCFlag(Creature cr)
        {
            ulong mask = 0;
            ulong guid = cr.GetSpawnId();

            foreach (var id in _ActiveEvents)
            {
                foreach (var pair in mGameEventNPCFlags[id])
                    if (pair.guid == guid)
                        mask |= pair.npcflag;
            }

            return mask;
        }

        public void Initialize()
        {
            SQLResult result = DB.World.Query("SELECT MAX(eventEntry) FROM game_event");

            if (!result.IsEmpty())
            {
                int maxEventId = result.Read<byte>(0);

                // Id starts with 1 and array with 0, thus increment
                maxEventId++;

                mGameEvent = new GameEventData[maxEventId];
                mGameEventCreatureGuids = new List<ulong>[maxEventId * 2 - 1];
                mGameEventGameobjectGuids = new List<ulong>[maxEventId * 2 - 1];
                mGameEventPoolIds = new List<uint>[maxEventId * 2 - 1];

                for (var i = 0; i < maxEventId * 2 - 1; ++i)
                {
                    mGameEventCreatureGuids[i] = new List<ulong>();
                    mGameEventGameobjectGuids[i] = new List<ulong>();
                    mGameEventPoolIds[i] = new List<uint>();
                }

                mGameEventCreatureQuests = new List<Tuple<uint, uint>>[maxEventId];
                mGameEventGameObjectQuests = new List<Tuple<uint, uint>>[maxEventId];
                mGameEventVendors = new Dictionary<uint, VendorItem>[maxEventId];
                mGameEventBattlegroundHolidays = new uint[maxEventId];
                mGameEventNPCFlags = new List<(ulong guid, ulong npcflag)>[maxEventId];
                mGameEventModelEquip = new List<Tuple<ulong, ModelEquip>>[maxEventId];

                for (var i = 0; i < maxEventId; ++i)
                {
                    mGameEvent[i] = new GameEventData();
                    mGameEventCreatureQuests[i] = new List<Tuple<uint, uint>>();
                    mGameEventGameObjectQuests[i] = new List<Tuple<uint, uint>>();
                    mGameEventVendors[i] = new Dictionary<uint, VendorItem>();
                    mGameEventNPCFlags[i] = new List<(ulong guid, ulong npcflag)>();
                    mGameEventModelEquip[i] = new List<Tuple<ulong, ModelEquip>>();
                }
            }
        }

        public uint StartSystem() // return the next event delay in ms
        {
            _ActiveEvents.Clear();
            uint delay = Update();
            isSystemInit = true;

            return delay;
        }

        public void StartArenaSeason()
        {
            int season = WorldConfig.GetIntValue(WorldCfg.ArenaSeasonId);
            SQLResult result = DB.World.Query("SELECT eventEntry FROM game_event_arena_seasons WHERE season = '{0}'", season);

            if (result.IsEmpty())
            {
                Log.outError(LogFilter.Gameevent, "ArenaSeason ({0}) must be an existant Arena Season", season);

                return;
            }

            ushort eventId = result.Read<byte>(0);

            if (eventId >= mGameEvent.Length)
            {
                Log.outError(LogFilter.Gameevent, "EventEntry {0} for ArenaSeason ({1}) does not exists", eventId, season);

                return;
            }

            StartEvent(eventId, true);
            Log.outInfo(LogFilter.Gameevent, "Arena Season {0} started...", season);
        }

        public uint Update() // return the next event delay in ms
        {
            long currenttime = GameTime.GetGameTime();
            uint nextEventDelay = Time.Day; // 1 day
            uint calcDelay;
            List<ushort> activate = new();
            List<ushort> deactivate = new();

            for (ushort id = 1; id < mGameEvent.Length; ++id)
            {
                // must do the activating first, and after that the deactivating
                // so first queue it
                if (CheckOneGameEvent(id))
                {
                    // if the world event is in NEXTPHASE State, and the Time has passed to finish this event, then do so
                    if (mGameEvent[id].State == GameEventState.WorldNextPhase &&
                        mGameEvent[id].Nextstart <= currenttime)
                    {
                        // set this event to finished, null the nextstart Time
                        mGameEvent[id].State = GameEventState.WorldFinished;
                        mGameEvent[id].Nextstart = 0;
                        // save the State of this gameevent
                        SaveWorldEventStateToDB(id);

                        // queue for deactivation
                        if (IsActiveEvent(id))
                            deactivate.Add(id);

                        // go to next event, this no longer needs an event update timer
                        continue;
                    }
                    else if (mGameEvent[id].State == GameEventState.WorldConditions &&
                             CheckOneGameEventConditions(id))
                    // changed, save to DB the gameevent State, will be updated in next update cycle
                    {
                        SaveWorldEventStateToDB(id);
                    }

                    Log.outDebug(LogFilter.Misc, "GameEvent {0} is active", id);

                    // queue for activation
                    if (!IsActiveEvent(id))
                        activate.Add(id);
                }
                else
                {
                    Log.outDebug(LogFilter.Misc, "GameEvent {0} is not active", id);

                    if (IsActiveEvent(id))
                    {
                        deactivate.Add(id);
                    }
                    else
                    {
                        if (!isSystemInit)
                        {
                            short event_nid = (short)(-1 * id);
                            // spawn all negative ones for this event
                            GameEventSpawn(event_nid);
                        }
                    }
                }

                calcDelay = NextCheck(id);

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

            Log.outInfo(LogFilter.Gameevent, "Next game event check in {0} seconds.", nextEventDelay + 1);

            return (nextEventDelay + 1) * Time.InMilliseconds; // Add 1 second to be sure event has started/stopped at next call
        }

        public void HandleQuestComplete(uint quest_id)
        {
            // translate the quest to event and condition
            var questToEvent = mQuestToEventConditions.LookupByKey(quest_id);

            // quest is registered
            if (questToEvent != null)
            {
                ushort event_id = questToEvent.Event_id;
                uint condition = questToEvent.Condition;
                float num = questToEvent.Num;

                // the event is not active, so return, don't increase condition finishes
                if (!IsActiveEvent(event_id))
                    return;

                // not in correct phase, return
                if (mGameEvent[event_id].State != GameEventState.WorldConditions)
                    return;

                var eventFinishCond = mGameEvent[event_id].Conditions.LookupByKey(condition);

                // condition is registered
                if (eventFinishCond != null)
                    // increase the done Count, only if less then the req
                    if (eventFinishCond.Done < eventFinishCond.ReqNum)
                    {
                        eventFinishCond.Done += num;

                        // check max limit
                        if (eventFinishCond.Done > eventFinishCond.ReqNum)
                            eventFinishCond.Done = eventFinishCond.ReqNum;

                        // save the change to db
                        SQLTransaction trans = new();

                        PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GAME_EVENT_CONDITION_SAVE);
                        stmt.AddValue(0, event_id);
                        stmt.AddValue(1, condition);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_GAME_EVENT_CONDITION_SAVE);
                        stmt.AddValue(0, event_id);
                        stmt.AddValue(1, condition);
                        stmt.AddValue(2, eventFinishCond.Done);
                        trans.Append(stmt);
                        DB.Characters.CommitTransaction(trans);

                        // check if all conditions are met, if so, update the event State
                        if (CheckOneGameEventConditions(event_id))
                        {
                            // changed, save to DB the gameevent State
                            SaveWorldEventStateToDB(event_id);
                            // Force update events to set timer
                            Global.WorldMgr.ForceGameEventUpdate();
                        }
                    }
            }
        }

        public bool IsHolidayActive(HolidayIds id)
        {
            if (id == HolidayIds.None)
                return false;

            var events = GetEventMap();
            var activeEvents = GetActiveEventList();

            foreach (var eventId in activeEvents)
                if (events[eventId].Holiday_id == id)
                    return true;

            return false;
        }

        public bool IsEventActive(ushort eventId)
        {
            var ae = GetActiveEventList();

            return ae.Contains(eventId);
        }

        public List<ushort> GetActiveEventList()
        {
            return _ActiveEvents;
        }

        public GameEventData[] GetEventMap()
        {
            return mGameEvent;
        }

        public bool IsActiveEvent(ushort event_id)
        {
            return _ActiveEvents.Contains(event_id);
        }

        private bool CheckOneGameEvent(ushort entry)
        {
            switch (mGameEvent[entry].State)
            {
                default:
                case GameEventState.Normal:
                    {
                        long currenttime = GameTime.GetGameTime();

                        // Get the event information
                        return mGameEvent[entry].Start < currenttime && currenttime < mGameEvent[entry].End && (currenttime - mGameEvent[entry].Start) % (mGameEvent[entry].Occurence * Time.Minute) < mGameEvent[entry].Length * Time.Minute;
                    }
                // if the State is conditions or nextphase, then the event should be active
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
                        long currenttime = GameTime.GetGameTime();

                        foreach (var gameEventId in mGameEvent[entry].Prerequisite_events)
                            if ((mGameEvent[gameEventId].State != GameEventState.WorldNextPhase && mGameEvent[gameEventId].State != GameEventState.WorldFinished) || // if prereq not in nextphase or finished State, then can't start this one
                                mGameEvent[gameEventId].Nextstart > currenttime)                                                                                     // if not in nextphase State for long enough, can't start this one
                                return false;

                        // all prerequisite events are met
                        // but if there are no prerequisites, this can be only activated through gm command
                        return !(mGameEvent[entry].Prerequisite_events.Empty());
                    }
            }
        }

        private void StartInternalEvent(ushort event_id)
        {
            if (event_id < 1 ||
                event_id >= mGameEvent.Length)
                return;

            if (!mGameEvent[event_id].IsValid())
                return;

            if (_ActiveEvents.Contains(event_id))
                return;

            StartEvent(event_id);
        }

        private void UnApplyEvent(ushort event_id)
        {
            Log.outInfo(LogFilter.Gameevent, "GameEvent {0} \"{1}\" removed.", event_id, mGameEvent[event_id].Description);
            //! Run SAI scripts with SMART_EVENT_GAME_EVENT_END
            RunSmartAIScripts(event_id, false);
            // un-spawn positive event tagged objects
            GameEventUnspawn((short)event_id);
            // spawn negative event tagget objects
            short event_nid = (short)(-1 * event_id);
            GameEventSpawn(event_nid);
            // restore equipment or model
            ChangeEquipOrModel((short)event_id, false);
            // Remove quests that are events only to non event npc
            UpdateEventQuests(event_id, false);
            UpdateWorldStates(event_id, false);
            // update npcflags in this event
            UpdateEventNPCFlags(event_id);
            // remove vendor items
            UpdateEventNPCVendor(event_id, false);
            // update bg holiday
            UpdateBattlegroundSettings();
        }

        private void ApplyNewEvent(ushort event_id)
        {
            byte announce = mGameEvent[event_id].Announce;

            if (announce == 1) // || (announce == 2 && WorldConfigEventAnnounce))
                Global.WorldMgr.SendWorldText(CypherStrings.Eventmessage, mGameEvent[event_id].Description);

            Log.outInfo(LogFilter.Gameevent, "GameEvent {0} \"{1}\" started.", event_id, mGameEvent[event_id].Description);

            // spawn positive event tagget objects
            GameEventSpawn((short)event_id);
            // un-spawn negative event tagged objects
            short event_nid = (short)(-1 * event_id);
            GameEventUnspawn(event_nid);
            // Change equipement or model
            ChangeEquipOrModel((short)event_id, true);
            // Add quests that are events only to non event npc
            UpdateEventQuests(event_id, true);
            UpdateWorldStates(event_id, true);
            // update npcflags in this event
            UpdateEventNPCFlags(event_id);
            // add vendor items
            UpdateEventNPCVendor(event_id, true);
            // update bg holiday
            UpdateBattlegroundSettings();

            //! Run SAI scripts with SMART_EVENT_GAME_EVENT_START
            RunSmartAIScripts(event_id, true);

            // check for seasonal quest reset.
            Global.WorldMgr.ResetEventSeasonalQuests(event_id, GetLastStartTime(event_id));
        }

        private void UpdateEventNPCFlags(ushort event_id)
        {
            MultiMap<uint, ulong> creaturesByMap = new();

            // go through the creatures whose npcflags are changed in the event
            foreach (var (guid, npcflag) in mGameEventNPCFlags[event_id])
            {
                // get the creature _data from the low Guid to get the entry, to be able to find out the whole Guid
                CreatureData data = Global.ObjectMgr.GetCreatureData(guid);

                if (data != null)
                    creaturesByMap.Add(data.MapId, guid);
            }

            foreach (var key in creaturesByMap.Keys)
                Global.MapMgr.DoForAllMapsWithMapId(key,
                                                    (Map map) =>
                                                    {
                                                        foreach (var spawnId in creaturesByMap[key])
                                                        {
                                                            var creatureBounds = map.GetCreatureBySpawnIdStore().LookupByKey(spawnId);

                                                            foreach (var creature in creatureBounds)
                                                            {
                                                                ulong npcflag = GetNPCFlag(creature);
                                                                CreatureTemplate creatureTemplate = creature.GetCreatureTemplate();

                                                                if (creatureTemplate != null)
                                                                    npcflag |= (ulong)creatureTemplate.Npcflag;

                                                                creature.ReplaceAllNpcFlags((NPCFlags)(npcflag & 0xFFFFFFFF));
                                                                creature.ReplaceAllNpcFlags2((NPCFlags2)(npcflag >> 32));
                                                                // reset gossip options, since the flag change might have added / removed some
                                                                //cr.ResetGossipOptions();
                                                            }
                                                        }
                                                    });
        }

        private void UpdateBattlegroundSettings()
        {
            Global.BattlegroundMgr.ResetHolidays();

            foreach (ushort activeEventId in _ActiveEvents)
                Global.BattlegroundMgr.SetHolidayActive(mGameEventBattlegroundHolidays[activeEventId]);
        }

        private void UpdateEventNPCVendor(ushort eventId, bool activate)
        {
            foreach (var npcEventVendor in mGameEventVendors[eventId])
                if (activate)
                    Global.ObjectMgr.AddVendorItem(npcEventVendor.Key, npcEventVendor.Value, false);
                else
                    Global.ObjectMgr.RemoveVendorItem(npcEventVendor.Key, npcEventVendor.Value.Item, npcEventVendor.Value.Type, false);
        }

        private void GameEventSpawn(short event_id)
        {
            int internal_event_id = mGameEvent.Length + event_id - 1;

            if (internal_event_id < 0 ||
                internal_event_id >= mGameEventCreatureGuids.Length)
            {
                Log.outError(LogFilter.Gameevent,
                             "GameEventMgr.GameEventSpawn attempt access to out of range mGameEventCreatureGuids element {0} (size: {1})",
                             internal_event_id,
                             mGameEventCreatureGuids.Length);

                return;
            }

            foreach (var guid in mGameEventCreatureGuids[internal_event_id])
            {
                // Add to correct cell
                CreatureData data = Global.ObjectMgr.GetCreatureData(guid);

                if (data != null)
                {
                    Global.ObjectMgr.AddCreatureToGrid(data);

                    // Spawn if necessary (loaded grids only)
                    Global.MapMgr.DoForAllMapsWithMapId(data.MapId,
                                                        map =>
                                                        {
                                                            map.RemoveRespawnTime(SpawnObjectType.Creature, guid);

                                                            // We use spawn coords to spawn
                                                            if (map.IsGridLoaded(data.SpawnPoint))
                                                                Creature.CreateCreatureFromDB(guid, map);
                                                        });
                }
            }

            if (internal_event_id < 0 ||
                internal_event_id >= mGameEventGameobjectGuids.Length)
            {
                Log.outError(LogFilter.Gameevent,
                             "GameEventMgr.GameEventSpawn attempt access to out of range mGameEventGameobjectGuids element {0} (size: {1})",
                             internal_event_id,
                             mGameEventGameobjectGuids.Length);

                return;
            }

            foreach (var guid in mGameEventGameobjectGuids[internal_event_id])
            {
                // Add to correct cell
                GameObjectData data = Global.ObjectMgr.GetGameObjectData(guid);

                if (data != null)
                {
                    Global.ObjectMgr.AddGameObjectToGrid(data);

                    // Spawn if necessary (loaded grids only)
                    // this base map checked as non-instanced and then only existed
                    Global.MapMgr.DoForAllMapsWithMapId(data.MapId,
                                                        map =>
                                                        {
                                                            map.RemoveRespawnTime(SpawnObjectType.GameObject, guid);

                                                            // We use current coords to unspawn, not spawn coords since creature can have changed grid
                                                            if (map.IsGridLoaded(data.SpawnPoint))
                                                            {
                                                                GameObject go = GameObject.CreateGameObjectFromDB(guid, map, false);

                                                                // @todo find out when it is add to map
                                                                if (go)
                                                                    // @todo find out when it is add to map
                                                                    if (go.IsSpawnedByDefault())
                                                                        if (!map.AddToMap(go))
                                                                            go.Dispose();
                                                            }
                                                        });
                }
            }

            if (internal_event_id < 0 ||
                internal_event_id >= mGameEventPoolIds.Length)
            {
                Log.outError(LogFilter.Gameevent,
                             "GameEventMgr.GameEventSpawn attempt access to out of range mGameEventPoolIds element {0} (size: {1})",
                             internal_event_id,
                             mGameEventPoolIds.Length);

                return;
            }

            foreach (var id in mGameEventPoolIds[internal_event_id])
            {
                PoolTemplateData poolTemplate = Global.PoolMgr.GetPoolTemplate(id);

                if (poolTemplate != null)
                    Global.MapMgr.DoForAllMapsWithMapId((uint)poolTemplate.MapId, map => { Global.PoolMgr.SpawnPool(map.GetPoolData(), id); });
            }
        }

        private void GameEventUnspawn(short event_id)
        {
            int internal_event_id = mGameEvent.Length + event_id - 1;

            if (internal_event_id < 0 ||
                internal_event_id >= mGameEventCreatureGuids.Length)
            {
                Log.outError(LogFilter.Gameevent,
                             "GameEventMgr.GameEventUnspawn attempt access to out of range mGameEventCreatureGuids element {0} (size: {1})",
                             internal_event_id,
                             mGameEventCreatureGuids.Length);

                return;
            }

            foreach (var guid in mGameEventCreatureGuids[internal_event_id])
            {
                // check if it's needed by another event, if so, don't remove
                if (event_id > 0 &&
                    HasCreatureActiveEventExcept(guid, (ushort)event_id))
                    continue;

                // Remove the creature from grid
                CreatureData data = Global.ObjectMgr.GetCreatureData(guid);

                if (data != null)
                {
                    Global.ObjectMgr.RemoveCreatureFromGrid(data);

                    Global.MapMgr.DoForAllMapsWithMapId(data.MapId,
                                                        map =>
                                                        {
                                                            map.RemoveRespawnTime(SpawnObjectType.Creature, guid);
                                                            var creatureBounds = map.GetCreatureBySpawnIdStore().LookupByKey(guid);

                                                            foreach (var creature in creatureBounds)
                                                                creature.AddObjectToRemoveList();
                                                        });
                }
            }

            if (internal_event_id < 0 ||
                internal_event_id >= mGameEventGameobjectGuids.Length)
            {
                Log.outError(LogFilter.Gameevent,
                             "GameEventMgr.GameEventUnspawn attempt access to out of range mGameEventGameobjectGuids element {0} (size: {1})",
                             internal_event_id,
                             mGameEventGameobjectGuids.Length);

                return;
            }

            foreach (var guid in mGameEventGameobjectGuids[internal_event_id])
            {
                // check if it's needed by another event, if so, don't remove
                if (event_id > 0 &&
                    HasGameObjectActiveEventExcept(guid, (ushort)event_id))
                    continue;

                // Remove the gameobject from grid
                GameObjectData data = Global.ObjectMgr.GetGameObjectData(guid);

                if (data != null)
                {
                    Global.ObjectMgr.RemoveGameObjectFromGrid(data);

                    Global.MapMgr.DoForAllMapsWithMapId(data.MapId,
                                                        map =>
                                                        {
                                                            map.RemoveRespawnTime(SpawnObjectType.GameObject, guid);
                                                            var gameobjectBounds = map.GetGameObjectBySpawnIdStore().LookupByKey(guid);

                                                            foreach (var go in gameobjectBounds)
                                                                go.AddObjectToRemoveList();
                                                        });
                }
            }

            if (internal_event_id < 0 ||
                internal_event_id >= mGameEventPoolIds.Length)
            {
                Log.outError(LogFilter.Gameevent, "GameEventMgr.GameEventUnspawn attempt access to out of range mGameEventPoolIds element {0} (size: {1})", internal_event_id, mGameEventPoolIds.Length);

                return;
            }

            foreach (var poolId in mGameEventPoolIds[internal_event_id])
            {
                PoolTemplateData poolTemplate = Global.PoolMgr.GetPoolTemplate(poolId);

                if (poolTemplate != null)
                    Global.MapMgr.DoForAllMapsWithMapId((uint)poolTemplate.MapId, map => { Global.PoolMgr.DespawnPool(map.GetPoolData(), poolId, true); });
            }
        }

        private void ChangeEquipOrModel(short event_id, bool activate)
        {
            foreach (var tuple in mGameEventModelEquip[event_id])
            {
                // Remove the creature from grid
                CreatureData data = Global.ObjectMgr.GetCreatureData(tuple.Item1);

                if (data == null)
                    continue;

                // Update if spawned
                Global.MapMgr.DoForAllMapsWithMapId(data.MapId,
                                                    map =>
                                                    {
                                                        var creatureBounds = map.GetCreatureBySpawnIdStore().LookupByKey(tuple.Item1);

                                                        foreach (var creature in creatureBounds)
                                                            if (activate)
                                                            {
                                                                tuple.Item2.Equipement_id_prev = creature.GetCurrentEquipmentId();
                                                                tuple.Item2.Modelid_prev = creature.GetDisplayId();
                                                                creature.LoadEquipment(tuple.Item2.Equipment_id, true);

                                                                if (tuple.Item2.Modelid > 0 &&
                                                                    tuple.Item2.Modelid_prev != tuple.Item2.Modelid &&
                                                                    Global.ObjectMgr.GetCreatureModelInfo(tuple.Item2.Modelid) != null)
                                                                {
                                                                    creature.SetDisplayId(tuple.Item2.Modelid);
                                                                    creature.SetNativeDisplayId(tuple.Item2.Modelid);
                                                                }
                                                            }
                                                            else
                                                            {
                                                                creature.LoadEquipment(tuple.Item2.Equipement_id_prev, true);

                                                                if (tuple.Item2.Modelid_prev > 0 &&
                                                                    tuple.Item2.Modelid_prev != tuple.Item2.Modelid &&
                                                                    Global.ObjectMgr.GetCreatureModelInfo(tuple.Item2.Modelid_prev) != null)
                                                                {
                                                                    creature.SetDisplayId(tuple.Item2.Modelid_prev);
                                                                    creature.SetNativeDisplayId(tuple.Item2.Modelid_prev);
                                                                }
                                                            }
                                                    });

                // now last step: put in _data
                CreatureData data2 = Global.ObjectMgr.NewOrExistCreatureData(tuple.Item1);

                if (activate)
                {
                    tuple.Item2.Modelid_prev = data2.Displayid;
                    tuple.Item2.Equipement_id_prev = (byte)data2.EquipmentId;
                    data2.Displayid = tuple.Item2.Modelid;
                    data2.EquipmentId = (sbyte)tuple.Item2.Equipment_id;
                }
                else
                {
                    data2.Displayid = tuple.Item2.Modelid_prev;
                    data2.EquipmentId = (sbyte)tuple.Item2.Equipement_id_prev;
                }
            }
        }

        private bool HasCreatureQuestActiveEventExcept(uint questId, ushort eventId)
        {
            foreach (var activeEventId in _ActiveEvents)
                if (activeEventId != eventId)
                    foreach (var pair in mGameEventCreatureQuests[activeEventId])
                        if (pair.Item2 == questId)
                            return true;

            return false;
        }

        private bool HasGameObjectQuestActiveEventExcept(uint questId, ushort eventId)
        {
            foreach (var activeEventId in _ActiveEvents)
                if (activeEventId != eventId)
                    foreach (var pair in mGameEventGameObjectQuests[activeEventId])
                        if (pair.Item2 == questId)
                            return true;

            return false;
        }

        private bool HasCreatureActiveEventExcept(ulong creatureId, ushort eventId)
        {
            foreach (var activeEventId in _ActiveEvents)
                if (activeEventId != eventId)
                {
                    int internal_event_id = mGameEvent.Length + activeEventId - 1;

                    foreach (var id in mGameEventCreatureGuids[internal_event_id])
                        if (id == creatureId)
                            return true;
                }

            return false;
        }

        private bool HasGameObjectActiveEventExcept(ulong goId, ushort eventId)
        {
            foreach (var activeEventId in _ActiveEvents)
                if (activeEventId != eventId)
                {
                    int internal_event_id = mGameEvent.Length + activeEventId - 1;

                    foreach (var id in mGameEventGameobjectGuids[internal_event_id])
                        if (id == goId)
                            return true;
                }

            return false;
        }

        private void UpdateEventQuests(ushort eventId, bool activate)
        {
            foreach (var pair in mGameEventCreatureQuests[eventId])
            {
                var CreatureQuestMap = Global.ObjectMgr.GetCreatureQuestRelationMapHACK();

                if (activate) // Add the pair(Id, quest) to the multimap
                {
                    CreatureQuestMap.Add(pair.Item1, pair.Item2);
                }
                else
                {
                    if (!HasCreatureQuestActiveEventExcept(pair.Item2, eventId))
                        // Remove the pair(Id, quest) from the multimap
                        CreatureQuestMap.Remove(pair.Item1, pair.Item2);
                }
            }

            foreach (var pair in mGameEventGameObjectQuests[eventId])
            {
                var GameObjectQuestMap = Global.ObjectMgr.GetGOQuestRelationMapHACK();

                if (activate) // Add the pair(Id, quest) to the multimap
                {
                    GameObjectQuestMap.Add(pair.Item1, pair.Item2);
                }
                else
                {
                    if (!HasGameObjectQuestActiveEventExcept(pair.Item2, eventId))
                        // Remove the pair(Id, quest) from the multimap
                        GameObjectQuestMap.Remove(pair.Item1, pair.Item2);
                }
            }
        }

        private void UpdateWorldStates(ushort event_id, bool Activate)
        {
            GameEventData Event = mGameEvent[event_id];

            if (Event.Holiday_id != HolidayIds.None)
            {
                BattlegroundTypeId bgTypeId = Global.BattlegroundMgr.WeekendHolidayIdToBGType(Event.Holiday_id);

                if (bgTypeId != BattlegroundTypeId.None)
                {
                    var bl = CliDB.BattlemasterListStorage.LookupByKey(Global.BattlegroundMgr.WeekendHolidayIdToBGType(Event.Holiday_id));

                    if (bl != null)
                        if (bl.HolidayWorldState != 0)
                            Global.WorldStateMgr.SetValue(bl.HolidayWorldState, Activate ? 1 : 0, false, null);
                }
            }
        }

        private bool CheckOneGameEventConditions(ushort event_id)
        {
            foreach (var pair in mGameEvent[event_id].Conditions)
                if (pair.Value.Done < pair.Value.ReqNum)
                    // return false if a condition doesn't match
                    return false;

            // set the phase
            mGameEvent[event_id].State = GameEventState.WorldNextPhase;

            // set the followup events' start Time
            if (mGameEvent[event_id].Nextstart == 0)
            {
                long currenttime = GameTime.GetGameTime();
                mGameEvent[event_id].Nextstart = currenttime + mGameEvent[event_id].Length * 60;
            }

            return true;
        }

        private void SaveWorldEventStateToDB(ushort event_id)
        {
            SQLTransaction trans = new();

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GAME_EVENT_SAVE);
            stmt.AddValue(0, event_id);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_GAME_EVENT_SAVE);
            stmt.AddValue(0, event_id);
            stmt.AddValue(1, (byte)mGameEvent[event_id].State);
            stmt.AddValue(2, mGameEvent[event_id].Nextstart != 0 ? mGameEvent[event_id].Nextstart : 0L);
            trans.Append(stmt);
            DB.Characters.CommitTransaction(trans);
        }

        private void SendWorldStateUpdate(Player player, ushort event_id)
        {
            foreach (var pair in mGameEvent[event_id].Conditions)
            {
                if (pair.Value.Done_world_state != 0)
                    player.SendUpdateWorldState(pair.Value.Done_world_state, (uint)(pair.Value.Done));

                if (pair.Value.Max_world_state != 0)
                    player.SendUpdateWorldState(pair.Value.Max_world_state, (uint)(pair.Value.ReqNum));
            }
        }

        private void RunSmartAIScripts(ushort event_id, bool activate)
        {
            //! Iterate over every supported source Type (creature and gameobject)
            //! Not entirely sure how this will affect units in non-loaded grids.
            Global.MapMgr.DoForAllMaps(map =>
                                       {
                                           GameEventAIHookWorker worker = new(event_id, activate);
                                           var visitor = new Visitor(worker, GridMapTypeMask.None);
                                           visitor.Visit(map.GetObjectsStore().Values.ToList());
                                       });
        }

        private void SetHolidayEventTime(GameEventData gameEvent)
        {
            if (gameEvent.HolidayStage == 0) // Ignore holiday
                return;

            var holiday = CliDB.HolidaysStorage.LookupByKey(gameEvent.Holiday_id);

            if (holiday.Date[0] == 0 ||
                holiday.Duration[0] == 0) // Invalid definitions
            {
                Log.outError(LogFilter.Sql, $"Missing date or duration for holiday {gameEvent.Holiday_id}.");

                return;
            }

            byte stageIndex = (byte)(gameEvent.HolidayStage - 1);
            gameEvent.Length = (uint)(holiday.Duration[stageIndex] * Time.Hour / Time.Minute);

            long stageOffset = 0;

            for (int i = 0; i < stageIndex; ++i)
                stageOffset += holiday.Duration[i] * Time.Hour;

            switch (holiday.CalendarFilterType)
            {
                case -1:                                           // Yearly
                    gameEvent.Occurence = Time.Year / Time.Minute; // Not all too useful

                    break;
                case 0: // Weekly
                    gameEvent.Occurence = Time.Week / Time.Minute;

                    break;
                case 1: // Defined dates only (Darkmoon Faire)
                    break;
                case 2: // Only used for looping events (Call to Arms)
                    break;
            }

            if (holiday.Looping != 0)
            {
                gameEvent.Occurence = 0;

                for (int i = 0; i < SharedConst.MaxHolidayDurations && holiday.Duration[i] != 0; ++i)
                    gameEvent.Occurence += (uint)(holiday.Duration[i] * Time.Hour / Time.Minute);
            }

            bool singleDate = ((holiday.Date[0] >> 24) & 0x1F) == 31; // Events with fixed date within year have - 1

            long curTime = GameTime.GetGameTime();

            for (int i = 0; i < SharedConst.MaxHolidayDates && holiday.Date[i] != 0; ++i)
            {
                uint date = holiday.Date[i];

                int year;

                if (singleDate)
                    year = Time.UnixTimeToDateTime(curTime).ToLocalTime().Year - 1; // First try last year (event active through New Year)
                else
                    year = (int)((date >> 24) & 0x1F) + 100 + 1900;

                var timeInfo = new DateTime(year, (int)((date >> 20) & 0xF) + 1, (int)((date >> 14) & 0x3F) + 1, (int)((date >> 6) & 0x1F), (int)(date & 0x3F), 0);

                long startTime = Time.DateTimeToUnixTime(timeInfo);

                if (curTime < startTime + gameEvent.Length * Time.Minute)
                {
                    gameEvent.Start = startTime + stageOffset;

                    break;
                }
                else if (singleDate)
                {
                    var tmCopy = timeInfo.AddYears(Time.UnixTimeToDateTime(curTime).ToLocalTime().Year); // This year
                    gameEvent.Start = Time.DateTimeToUnixTime(tmCopy) + stageOffset;

                    break;
                }
                else
                {
                    // date is due and not a singleDate event, try with next DBC date (modified by holiday_dates)
                    // if none is found we don't modify start date and use the one in game_event
                }
            }
        }

        private long GetLastStartTime(ushort event_id)
        {
            if (event_id >= mGameEvent.Length)
                return 0;

            if (mGameEvent[event_id].State != GameEventState.Normal)
                return 0;

            DateTime now = GameTime.GetSystemTime();
            DateTime eventInitialStart = Time.UnixTimeToDateTime(mGameEvent[event_id].Start);
            TimeSpan occurence = TimeSpan.FromMinutes(mGameEvent[event_id].Occurence);
            TimeSpan durationSinceLastStart = TimeSpan.FromTicks((now - eventInitialStart).Ticks % occurence.Ticks);

            return Time.DateTimeToUnixTime(now - durationSinceLastStart);
        }

        private void AddActiveEvent(ushort event_id)
        {
            _ActiveEvents.Add(event_id);
        }

        private void RemoveActiveEvent(ushort event_id)
        {
            _ActiveEvents.Remove(event_id);
        }
    }
}