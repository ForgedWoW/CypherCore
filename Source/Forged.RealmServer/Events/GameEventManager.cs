// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Forged.RealmServer.DataStorage;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Maps;
using Serilog;
using Forged.RealmServer.Globals;
using Microsoft.Extensions.Configuration;
using Framework.Util;
using Forged.RealmServer.BattleGrounds;

namespace Forged.RealmServer;

public class GameEventManager
{
    private List<ulong>[] _gameEventCreatureGuids;
    private List<ulong>[] _gameEventGameobjectGuids;
	readonly Dictionary<uint, GameEventQuestToEventConditionNum> _QuestToEventConditions = new();
	readonly List<ushort> _activeEvents = new();
    private readonly GameTime _gameTime;
    private readonly WorldManager _worldManager;
    private readonly CliDB _cliDb;
    private readonly CharacterDatabase _characterDatabase;
    private readonly WorldDatabase _worldDatabase;
    private readonly GameObjectManager _gameObjectManager;
    private readonly IConfiguration _configuration;
    private readonly WorldConfig _worldConfig;
    private readonly PoolManager _poolManager;
    private readonly BattlegroundManager _battlegroundManager;
    private readonly WorldStateManager _worldStateManager;
    List<Tuple<uint, uint>>[] _gameEventCreatureQuests;
	List<Tuple<uint, uint>>[] _gameEventGameObjectQuests;
	Dictionary<uint, VendorItem>[] _gameEventVendors;
	List<Tuple<ulong, ModelEquip>>[] _gameEventModelEquip;
	List<uint>[] _gameEventPoolIds;
	GameEventData[] _gameEvent;
	uint[] _gameEventBattlegroundHolidays;
	List<(ulong guid, ulong npcflag)>[] _gameEventNPCFlags;
	bool _isSystemInit;

	GameEventManager(GameTime gameTime, WorldManager worldManager, CliDB cliDB, CharacterDatabase characterDatabase, WorldDatabase worldDatabase,
		GameObjectManager gameObjectManager, IConfiguration configuration, WorldConfig worldConfig, PoolManager poolManager, BattlegroundManager battlegroundManager,
		WorldStateManager worldStateManager)
    {
        _gameTime = gameTime;
        _worldManager = worldManager;
        _cliDb = cliDB;
        _characterDatabase = characterDatabase;
        _worldDatabase = worldDatabase;
        _gameObjectManager = gameObjectManager;
        _configuration = configuration;
        _worldConfig = worldConfig;
        _poolManager = poolManager;
        _battlegroundManager = battlegroundManager;
        _worldStateManager = worldStateManager;

		Initialize();
		LoadFromDB();
    }

    public uint NextCheck(ushort entry)
	{
		var currenttime = _gameTime.CurrentGameTime;

		// for NEXTPHASE state world events, return the delay to start the next event, so the followup event will be checked correctly
		if ((_gameEvent[entry].state == GameEventState.WorldNextPhase || _gameEvent[entry].state == GameEventState.WorldFinished) && _gameEvent[entry].nextstart >= currenttime)
			return (uint)(_gameEvent[entry].nextstart - currenttime);

		// for CONDITIONS state world events, return the length of the wait period, so if the conditions are met, this check will be called again to set the timer as NEXTPHASE event
		if (_gameEvent[entry].state == GameEventState.WorldConditions)
		{
			if (_gameEvent[entry].length != 0)
				return _gameEvent[entry].length * 60;
			else
				return Time.Day;
		}

		// outdated event: we return max
		if (currenttime > _gameEvent[entry].end)
			return Time.Day;

		// never started event, we return delay before start
		if (_gameEvent[entry].start > currenttime)
			return (uint)(_gameEvent[entry].start - currenttime);

		uint delay;

		// in event, we return the end of it
		if ((((currenttime - _gameEvent[entry].start) % (_gameEvent[entry].occurence * 60)) < (_gameEvent[entry].length * 60)))
			// we return the delay before it ends
			delay = (uint)((_gameEvent[entry].length * Time.Minute) - ((currenttime - _gameEvent[entry].start) % (_gameEvent[entry].occurence * Time.Minute)));
		else // not in window, we return the delay before next start
			delay = (uint)((_gameEvent[entry].occurence * Time.Minute) - ((currenttime - _gameEvent[entry].start) % (_gameEvent[entry].occurence * Time.Minute)));

		// In case the end is before next check
		if (_gameEvent[entry].end < currenttime + delay)
			return (uint)(_gameEvent[entry].end - currenttime);
		else
			return delay;
	}

	public bool StartEvent(ushort event_id, bool overwrite = false)
	{
		var data = _gameEvent[event_id];

		if (data.state == GameEventState.Normal || data.state == GameEventState.Internal)
		{
			AddActiveEvent(event_id);
			ApplyNewEvent(event_id);

			if (overwrite)
			{
				_gameEvent[event_id].start = _gameTime.CurrentGameTime;

				if (data.end <= data.start)
					data.end = data.start + data.length;
			}

			return false;
		}
		else
		{
			if (data.state == GameEventState.WorldInactive)
				// set to conditions phase
				data.state = GameEventState.WorldConditions;

			// add to active events
			AddActiveEvent(event_id);
			// add spawns
			ApplyNewEvent(event_id);

			// check if can go to next state
			var conditions_met = CheckOneGameEventConditions(event_id);
			// save to db
			SaveWorldEventStateToDB(event_id);

			// force game event update to set the update timer if conditions were met from a command
			// this update is needed to possibly start events dependent on the started one
			// or to scedule another update where the next event will be started
			if (overwrite && conditions_met)
				_worldManager.ForceGameEventUpdate();

			return conditions_met;
		}
	}

	public void StopEvent(ushort event_id, bool overwrite = false)
	{
		var data = _gameEvent[event_id];
		var serverwide_evt = data.state != GameEventState.Normal && data.state != GameEventState.Internal;

		RemoveActiveEvent(event_id);
		UnApplyEvent(event_id);

		if (overwrite && !serverwide_evt)
		{
			data.start = _gameTime.CurrentGameTime - data.length * Time.Minute;

			if (data.end <= data.start)
				data.end = data.start + data.length;
		}
		else if (serverwide_evt)
		{
			// if finished world event, then only gm command can stop it
			if (overwrite || data.state != GameEventState.WorldFinished)
			{
				// reset conditions
				data.nextstart = 0;
				data.state = GameEventState.WorldInactive;

				foreach (var pair in data.conditions)
					pair.Value.done = 0;

				SQLTransaction trans = new();
				var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ALL_GAME_EVENT_CONDITION_SAVE);
				stmt.AddValue(0, event_id);
				trans.Append(stmt);

				stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GAME_EVENT_SAVE);
				stmt.AddValue(0, event_id);
				trans.Append(stmt);

				_characterDatabase.CommitTransaction(trans);
			}
		}
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
				Log.Logger.Information("Loaded 0 game events. DB table `game_event` is empty.");

				return;
			}

			uint count = 0;

			do
			{
				var event_id = result.Read<byte>(0);

				if (event_id == 0)
				{
					Log.Logger.Error("`game_event` game event entry 0 is reserved and can't be used.");

					continue;
				}

				GameEventData pGameEvent = new();
				var starttime = result.Read<ulong>(1);
				pGameEvent.start = (long)starttime;
				var endtime = result.Read<ulong>(2);
				pGameEvent.end = (long)endtime;
				pGameEvent.occurence = result.Read<uint>(3);
				pGameEvent.length = result.Read<uint>(4);
				pGameEvent.holiday_id = (HolidayIds)result.Read<uint>(5);

				pGameEvent.holidayStage = result.Read<byte>(6);
				pGameEvent.description = result.Read<string>(7);
				pGameEvent.state = (GameEventState)result.Read<byte>(8);
				pGameEvent.announce = result.Read<byte>(9);
				pGameEvent.nextstart = 0;

				++count;

				if (pGameEvent.length == 0 && pGameEvent.state == GameEventState.Normal) // length>0 is validity check
				{
					Log.Logger.Error($"`game_event` game event id ({event_id}) isn't a world event and has length = 0, thus it can't be used.");

					continue;
				}

				if (pGameEvent.holiday_id != HolidayIds.None)
				{
					if (!_cliDb.HolidaysStorage.ContainsKey((uint)pGameEvent.holiday_id))
					{
						Log.Logger.Error($"`game_event` game event id ({event_id}) contains nonexisting holiday id {pGameEvent.holiday_id}.");
						pGameEvent.holiday_id = HolidayIds.None;

						continue;
					}

					if (pGameEvent.holidayStage > SharedConst.MaxHolidayDurations)
					{
						Log.Logger.Error("`game_event` game event id ({event_id}) has out of range holidayStage {pGameEvent.holidayStage}.");
						pGameEvent.holidayStage = 0;

						continue;
					}

					SetHolidayEventTime(pGameEvent);
				}

				_gameEvent[event_id] = pGameEvent;
			} while (result.NextRow());

			Log.Logger.Information("Loaded {0} game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
		}

		Log.Logger.Information("Loading Game Event Saves Data...");

		{
			var oldMSTime = Time.MSTime;

			//                                                       0       1        2
			var result = _characterDatabase.Query("SELECT eventEntry, state, next_start FROM game_event_save");

			if (result.IsEmpty())
			{
				Log.Logger.Information("Loaded 0 game event saves in game events. DB table `game_event_save` is empty.");
			}
			else
			{
				uint count = 0;

				do
				{
					var event_id = result.Read<byte>(0);

					if (event_id >= _gameEvent.Length)
					{
						Log.Logger.Error("`game_event_save` game event entry ({0}) not exist in `game_event`", event_id);

						continue;
					}

					if (_gameEvent[event_id].state != GameEventState.Normal && _gameEvent[event_id].state != GameEventState.Internal)
					{
						_gameEvent[event_id].state = (GameEventState)result.Read<byte>(1);
						_gameEvent[event_id].nextstart = result.Read<uint>(2);
					}
					else
					{
						Log.Logger.Error("game_event_save includes event save for non-worldevent id {0}", event_id);

						continue;
					}

					++count;
				} while (result.NextRow());

				Log.Logger.Information("Loaded {0} game event saves in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
			}
		}

		Log.Logger.Information("Loading Game Event Prerequisite Data...");

		{
			var oldMSTime = Time.MSTime;

			//                                                   0             1
			var result = _worldDatabase.Query("SELECT eventEntry, prerequisite_event FROM game_event_prerequisite");

			if (result.IsEmpty())
			{
				Log.Logger.Information("Loaded 0 game event prerequisites in game events. DB table `game_event_prerequisite` is empty.");
			}
			else
			{
				uint count = 0;

				do
				{
					ushort event_id = result.Read<byte>(0);

					if (event_id >= _gameEvent.Length)
					{
						Log.Logger.Error("`game_event_prerequisite` game event id ({0}) is out of range compared to max event id in `game_event`", event_id);

						continue;
					}

					if (_gameEvent[event_id].state != GameEventState.Normal && _gameEvent[event_id].state != GameEventState.Internal)
					{
						ushort prerequisite_event = result.Read<byte>(1);

						if (prerequisite_event >= _gameEvent.Length)
						{
							Log.Logger.Error("`game_event_prerequisite` game event prerequisite id ({0}) not exist in `game_event`", prerequisite_event);

							continue;
						}

						_gameEvent[event_id].prerequisite_events.Add(prerequisite_event);
					}
					else
					{
						Log.Logger.Error("game_event_prerequisiste includes event entry for non-worldevent id {0}", event_id);

						continue;
					}

					++count;
				} while (result.NextRow());

				Log.Logger.Information("Loaded {0} game event prerequisites in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
			}
		}

		Log.Logger.Information("Loading Game Event Creature Data...");

		{
			var oldMSTime = Time.MSTime;

			//                                                 0        1
			var result = _worldDatabase.Query("SELECT guid, eventEntry FROM game_event_creature");

			if (result.IsEmpty())
			{
				Log.Logger.Information("Loaded 0 creatures in game events. DB table `game_event_creature` is empty");
			}
			else
			{
				uint count = 0;

				do
				{
					var guid = result.Read<ulong>(0);
					short event_id = result.Read<sbyte>(1);
					var internal_event_id = _gameEvent.Length + event_id - 1;

					var data = _gameObjectManager.GetCreatureData(guid);

					if (data == null)
					{
						if (_configuration.GetDefaultValue("load.autoclean", false))
							_worldDatabase.Execute($"DELETE FROM game_event_creature WHERE guid = {guid}");
						else
							Log.Logger.Error("`game_event_creature` contains creature (GUID: {0}) not found in `creature` table.", guid);

						continue;
					}

					if (internal_event_id < 0 || internal_event_id >= _gameEventCreatureGuids.Length)
					{
						Log.Logger.Error("`game_event_creature` game event id ({0}) not exist in `game_event`", event_id);

						continue;
					}

					// Log error for pooled object, but still spawn it
					if (data.poolId != 0)
						Log.Logger.Error($"`game_event_creature`: game event id ({event_id}) contains creature ({guid}) which is part of a pool ({data.poolId}). This should be spawned in game_event_pool");

					_gameEventCreatureGuids[internal_event_id].Add(guid);

					++count;
				} while (result.NextRow());

				Log.Logger.Information("Loaded {0} creatures in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
			}
		}

		Log.Logger.Information("Loading Game Event GO Data...");

		{
			var oldMSTime = Time.MSTime;

			//                                                0         1
			var result = _worldDatabase.Query("SELECT guid, eventEntry FROM game_event_gameobject");

			if (result.IsEmpty())
			{
				Log.Logger.Information("Loaded 0 gameobjects in game events. DB table `game_event_gameobject` is empty.");
			}
			else
			{
				uint count = 0;

				do
				{
					var guid = result.Read<ulong>(0);
					short event_id = result.Read<byte>(1);
					var internal_event_id = _gameEvent.Length + event_id - 1;

					var data = _gameObjectManager.GetGameObjectData(guid);

					if (data == null)
					{
						Log.Logger.Error("`game_event_gameobject` contains gameobject (GUID: {0}) not found in `gameobject` table.", guid);

						continue;
					}

					if (internal_event_id < 0 || internal_event_id >= _gameEventGameobjectGuids.Length)
					{
						Log.Logger.Error("`game_event_gameobject` game event id ({0}) not exist in `game_event`", event_id);

						continue;
					}

					// Log error for pooled object, but still spawn it
					if (data.poolId != 0)
						Log.Logger.Error($"`game_event_gameobject`: game event id ({event_id}) contains game object ({guid}) which is part of a pool ({data.poolId}). This should be spawned in game_event_pool");

					_gameEventGameobjectGuids[internal_event_id].Add(guid);

					++count;
				} while (result.NextRow());

				Log.Logger.Information("Loaded {0} gameobjects in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
			}
		}

		Log.Logger.Information("Loading Game Event Model/Equipment Change Data...");

		{
			var oldMSTime = Time.MSTime;

			//                                                       0           1                       2                                 3                                     4
			var result = _worldDatabase.Query("SELECT creature.guid, creature.id, game_event_model_equip.eventEntry, game_event_model_equip.modelid, game_event_model_equip.equipment_id " +
										"FROM creature JOIN game_event_model_equip ON creature.guid=game_event_model_equip.guid");

			if (result.IsEmpty())
			{
				Log.Logger.Information("Loaded 0 model/equipment changes in game events. DB table `game_event_model_equip` is empty.");
			}
			else
			{
				uint count = 0;

				do
				{
					var guid = result.Read<ulong>(0);
					var entry = result.Read<uint>(1);
					ushort event_id = result.Read<byte>(2);

					if (event_id >= _gameEventModelEquip.Length)
					{
						Log.Logger.Error("`game_event_model_equip` game event id ({0}) is out of range compared to max event id in `game_event`", event_id);

						continue;
					}

					ModelEquip newModelEquipSet = new();
					newModelEquipSet.modelid = result.Read<uint>(3);
					newModelEquipSet.equipment_id = result.Read<byte>(4);
					newModelEquipSet.equipement_id_prev = 0;
					newModelEquipSet.modelid_prev = 0;

					if (newModelEquipSet.equipment_id > 0)
					{
						var equipId = (sbyte)newModelEquipSet.equipment_id;

						if (_gameObjectManager.GetEquipmentInfo(entry, equipId) == null)
						{
							Log.Logger.Error(
										"Table `game_event_model_equip` have creature (Guid: {0}, entry: {1}) with equipment_id {2} not found in table `creature_equip_template`, set to no equipment.",
										guid,
										entry,
										newModelEquipSet.equipment_id);

							continue;
						}
					}

					_gameEventModelEquip[event_id].Add(Tuple.Create(guid, newModelEquipSet));

					++count;
				} while (result.NextRow());

				Log.Logger.Information("Loaded {0} model/equipment changes in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
			}
		}

		Log.Logger.Information("Loading Game Event Quest Data...");

		{
			var oldMSTime = Time.MSTime;

			//                                               0     1      2
			var result = _worldDatabase.Query("SELECT id, quest, eventEntry FROM game_event_creature_quest");

			if (result.IsEmpty())
			{
				Log.Logger.Information("Loaded 0 quests additions in game events. DB table `game_event_creature_quest` is empty.");
			}
			else
			{
				uint count = 0;

				do
				{
					var id = result.Read<uint>(0);
					var quest = result.Read<uint>(1);
					ushort event_id = result.Read<byte>(2);

					if (event_id >= _gameEventCreatureQuests.Length)
					{
						Log.Logger.Error("`game_event_creature_quest` game event id ({0}) not exist in `game_event`", event_id);

						continue;
					}

					_gameEventCreatureQuests[event_id].Add(Tuple.Create(id, quest));

					++count;
				} while (result.NextRow());

				Log.Logger.Information("Loaded {0} quests additions in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
			}
		}

		Log.Logger.Information("Loading Game Event GO Quest Data...");

		{
			var oldMSTime = Time.MSTime;

			//                                               0     1      2
			var result = _worldDatabase.Query("SELECT id, quest, eventEntry FROM game_event_gameobject_quest");

			if (result.IsEmpty())
			{
				Log.Logger.Information("Loaded 0 go quests additions in game events. DB table `game_event_gameobject_quest` is empty.");
			}
			else
			{
				uint count = 0;

				do
				{
					var id = result.Read<uint>(0);
					var quest = result.Read<uint>(1);
					ushort event_id = result.Read<byte>(2);

					if (event_id >= _gameEventGameObjectQuests.Length)
					{
						Log.Logger.Error("`game_event_gameobject_quest` game event id ({0}) not exist in `game_event`", event_id);

						continue;
					}

					_gameEventGameObjectQuests[event_id].Add(Tuple.Create(id, quest));

					++count;
				} while (result.NextRow());

				Log.Logger.Information("Loaded {0} quests additions in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
			}
		}

		Log.Logger.Information("Loading Game Event Quest Condition Data...");

		{
			var oldMSTime = Time.MSTime;

			//                                                 0       1         2             3
			var result = _worldDatabase.Query("SELECT quest, eventEntry, condition_id, num FROM game_event_quest_condition");

			if (result.IsEmpty())
			{
				Log.Logger.Information("Loaded 0 quest event conditions in game events. DB table `game_event_quest_condition` is empty.");
			}
			else
			{
				uint count = 0;

				do
				{
					var quest = result.Read<uint>(0);
					ushort event_id = result.Read<byte>(1);
					var condition = result.Read<uint>(2);
					var num = result.Read<float>(3);

					if (event_id >= _gameEvent.Length)
					{
						Log.Logger.Error("`game_event_quest_condition` game event id ({0}) is out of range compared to max event id in `game_event`", event_id);

						continue;
					}

					if (!_QuestToEventConditions.ContainsKey(quest))
						_QuestToEventConditions[quest] = new GameEventQuestToEventConditionNum();

					_QuestToEventConditions[quest].event_id = event_id;
					_QuestToEventConditions[quest].condition = condition;
					_QuestToEventConditions[quest].num = num;

					++count;
				} while (result.NextRow());

				Log.Logger.Information("Loaded {0} quest event conditions in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
			}
		}

		Log.Logger.Information("Loading Game Event Condition Data...");

		{
			var oldMSTime = Time.MSTime;

			//                                                  0          1            2             3                      4
			var result = _worldDatabase.Query("SELECT eventEntry, condition_id, req_num, max_world_state_field, done_world_state_field FROM game_event_condition");

			if (result.IsEmpty())
			{
				Log.Logger.Information("Loaded 0 conditions in game events. DB table `game_event_condition` is empty.");
			}
			else
			{
				uint count = 0;

				do
				{
					ushort event_id = result.Read<byte>(0);
					var condition = result.Read<uint>(1);

					if (event_id >= _gameEvent.Length)
					{
						Log.Logger.Error("`game_event_condition` game event id ({0}) is out of range compared to max event id in `game_event`", event_id);

						continue;
					}

					_gameEvent[event_id].conditions[condition].reqNum = result.Read<float>(2);
					_gameEvent[event_id].conditions[condition].done = 0;
					_gameEvent[event_id].conditions[condition].max_world_state = result.Read<ushort>(3);
					_gameEvent[event_id].conditions[condition].done_world_state = result.Read<ushort>(4);

					++count;
				} while (result.NextRow());

				Log.Logger.Information("Loaded {0} conditions in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
			}
		}

		Log.Logger.Information("Loading Game Event Condition Save Data...");

		{
			var oldMSTime = Time.MSTime;

			//                                                      0           1         2
			var result = _characterDatabase.Query("SELECT eventEntry, condition_id, done FROM game_event_condition_save");

			if (result.IsEmpty())
			{
				Log.Logger.Information("Loaded 0 condition saves in game events. DB table `game_event_condition_save` is empty.");
			}
			else
			{
				uint count = 0;

				do
				{
					ushort event_id = result.Read<byte>(0);
					var condition = result.Read<uint>(1);

					if (event_id >= _gameEvent.Length)
					{
						Log.Logger.Error("`game_event_condition_save` game event id ({0}) is out of range compared to max event id in `game_event`", event_id);

						continue;
					}

					if (_gameEvent[event_id].conditions.ContainsKey(condition))
					{
						_gameEvent[event_id].conditions[condition].done = result.Read<uint>(2);
					}
					else
					{
						Log.Logger.Error("game_event_condition_save contains not present condition evt id {0} cond id {1}", event_id, condition);

						continue;
					}

					++count;
				} while (result.NextRow());

				Log.Logger.Information("Loaded {0} condition saves in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
			}
		}

		Log.Logger.Information("Loading Game Event NPCflag Data...");

		{
			var oldMSTime = Time.MSTime;

			//                                                0       1        2
			var result = _worldDatabase.Query("SELECT guid, eventEntry, npcflag FROM game_event_npcflag");

			if (result.IsEmpty())
			{
				Log.Logger.Information("Loaded 0 npcflags in game events. DB table `game_event_npcflag` is empty.");
			}
			else
			{
				uint count = 0;

				do
				{
					var guid = result.Read<ulong>(0);
					ushort event_id = result.Read<byte>(1);
					var npcflag = result.Read<ulong>(2);

					if (event_id >= _gameEvent.Length)
					{
						Log.Logger.Error("`game_event_npcflag` game event id ({0}) is out of range compared to max event id in `game_event`", event_id);

						continue;
					}

					_gameEventNPCFlags[event_id].Add((guid, npcflag));

					++count;
				} while (result.NextRow());

				Log.Logger.Information("Loaded {0} npcflags in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
			}
		}

		Log.Logger.Information("Loading Game Event Seasonal Quest Relations...");

		{
			var oldMSTime = Time.MSTime;

			//                                                  0          1
			var result = _worldDatabase.Query("SELECT questId, eventEntry FROM game_event_seasonal_questrelation");

			if (result.IsEmpty())
			{
				Log.Logger.Information("Loaded 0 seasonal quests additions in game events. DB table `game_event_seasonal_questrelation` is empty.");
			}
			else
			{
				uint count = 0;

				do
				{
					var questId = result.Read<uint>(0);
					ushort eventEntry = result.Read<byte>(1); // @todo Change to byte

					var questTemplate = _gameObjectManager.GetQuestTemplate(questId);

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

				Log.Logger.Information("Loaded {0} quests additions in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
			}
		}

		Log.Logger.Information("Loading Game Event Vendor Additions Data...");

		{
			var oldMSTime = Time.MSTime;

			//                                               0           1     2     3         4         5             6     7             8                  9
			var result = _worldDatabase.Query("SELECT eventEntry, guid, item, maxcount, incrtime, ExtendedCost, type, BonusListIDs, PlayerConditionId, IgnoreFiltering FROM game_event_npc_vendor ORDER BY guid, slot ASC");

			if (result.IsEmpty())
			{
				Log.Logger.Information("Loaded 0 vendor additions in game events. DB table `game_event_npc_vendor` is empty.");
			}
			else
			{
				uint count = 0;

				do
				{
					var event_id = result.Read<byte>(0);
					var guid = result.Read<ulong>(1);

					if (event_id >= _gameEventVendors.Length)
					{
						Log.Logger.Error("`game_event_npc_vendor` game event id ({0}) not exist in `game_event`", event_id);

						continue;
					}

					// get the event npc flag for checking if the npc will be vendor during the event or not
					ulong event_npc_flag = 0;
					var flist = _gameEventNPCFlags[event_id];

					foreach (var pair in flist)
						if (pair.guid == guid)
						{
							event_npc_flag = pair.npcflag;

							break;
						}

					// get creature entry
					uint entry = 0;
					var data = _gameObjectManager.GetCreatureData(guid);

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

					// check validity with event's npcflag
					if (!_gameObjectManager.IsVendorItemValid(entry, vItem, null, null, event_npc_flag))
						continue;

					_gameEventVendors[event_id].Add(entry, vItem);

					++count;
				} while (result.NextRow());

				Log.Logger.Information("Loaded {0} vendor additions in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
			}
		}

		Log.Logger.Information("Loading Game Event Battleground Holiday Data...");

		{
			var oldMSTime = Time.MSTime;

			//                                         0           1
			var result = _worldDatabase.Query("SELECT EventEntry, BattlegroundID FROM game_event_battleground_holiday");

			if (result.IsEmpty())
			{
				Log.Logger.Information("Loaded 0 Battlegroundholidays in game events. DB table `game_event_battleground_holiday` is empty.");
			}
			else
			{
				uint count = 0;

				do
				{
					ushort eventId = result.Read<byte>(0);

					if (eventId >= _gameEvent.Length)
					{
						Log.Logger.Error("`game_event_battleground_holiday` game event id ({0}) not exist in `game_event`", eventId);

						continue;
					}

					_gameEventBattlegroundHolidays[eventId] = result.Read<uint>(1);

					++count;
				} while (result.NextRow());

				Log.Logger.Information("Loaded {0} Battlegroundholidays in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
			}
		}

		Log.Logger.Information("Loading Game Event Pool Data...");

		{
			var oldMSTime = Time.MSTime;

			//                                                               0                         1
			var result = _worldDatabase.Query("SELECT pool_template.entry, game_event_pool.eventEntry FROM pool_template" +
										" JOIN game_event_pool ON pool_template.entry = game_event_pool.pool_entry");

			if (result.IsEmpty())
			{
				Log.Logger.Information("Loaded 0 pools for game events. DB table `game_event_pool` is empty.");
			}
			else
			{
				uint count = 0;

				do
				{
					var entry = result.Read<uint>(0);
					short event_id = result.Read<sbyte>(1);
					var internal_event_id = _gameEvent.Length + event_id - 1;

					if (internal_event_id < 0 || internal_event_id >= _gameEventPoolIds.Length)
					{
						Log.Logger.Error("`game_event_pool` game event id ({0}) not exist in `game_event`", event_id);

						continue;
					}

					if (!_poolManager.CheckPool(entry))
					{
						Log.Logger.Error("Pool Id ({0}) has all creatures or gameobjects with explicit chance sum <>100 and no equal chance defined. The pool system cannot pick one to spawn.", entry);

						continue;
					}


					_gameEventPoolIds[internal_event_id].Add(entry);

					++count;
				} while (result.NextRow());

				Log.Logger.Information("Loaded {0} pools for game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
			}
		}
	}

	public ulong GetNPCFlag(Creature cr)
	{
		ulong mask = 0;
		var guid = cr.SpawnId;

		foreach (var id in _activeEvents)
		{
			foreach (var pair in _gameEventNPCFlags[id])
				if (pair.guid == guid)
					mask |= pair.npcflag;
		}

		return mask;
	}

	public void Initialize()
	{
		var result = _worldDatabase.Query("SELECT MAX(eventEntry) FROM game_event");

		if (!result.IsEmpty())
		{
			int maxEventId = result.Read<byte>(0);

			// Id starts with 1 and array with 0, thus increment
			maxEventId++;

			_gameEvent = new GameEventData[maxEventId];
			_gameEventCreatureGuids = new List<ulong>[maxEventId * 2 - 1];
			_gameEventGameobjectGuids = new List<ulong>[maxEventId * 2 - 1];
			_gameEventPoolIds = new List<uint>[maxEventId * 2 - 1];

			for (var i = 0; i < maxEventId * 2 - 1; ++i)
			{
				_gameEventCreatureGuids[i] = new List<ulong>();
				_gameEventGameobjectGuids[i] = new List<ulong>();
				_gameEventPoolIds[i] = new List<uint>();
			}

			_gameEventCreatureQuests = new List<Tuple<uint, uint>>[maxEventId];
			_gameEventGameObjectQuests = new List<Tuple<uint, uint>>[maxEventId];
			_gameEventVendors = new Dictionary<uint, VendorItem>[maxEventId];
			_gameEventBattlegroundHolidays = new uint[maxEventId];
			_gameEventNPCFlags = new List<(ulong guid, ulong npcflag)>[maxEventId];
			_gameEventModelEquip = new List<Tuple<ulong, ModelEquip>>[maxEventId];

			for (var i = 0; i < maxEventId; ++i)
			{
				_gameEvent[i] = new GameEventData();
				_gameEventCreatureQuests[i] = new List<Tuple<uint, uint>>();
				_gameEventGameObjectQuests[i] = new List<Tuple<uint, uint>>();
				_gameEventVendors[i] = new Dictionary<uint, VendorItem>();
				_gameEventNPCFlags[i] = new List<(ulong guid, ulong npcflag)>();
				_gameEventModelEquip[i] = new List<Tuple<ulong, ModelEquip>>();
			}
		}
	}

	public uint StartSystem() // return the next event delay in ms
	{
		_activeEvents.Clear();
		var delay = Update();
		_isSystemInit = true;

		return delay;
	}

	public void StartArenaSeason()
	{
		var season = _worldConfig.GetIntValue(WorldCfg.ArenaSeasonId);
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

	public uint Update() // return the next event delay in ms
	{
		var currenttime = _gameTime.CurrentGameTime;
		uint nextEventDelay = Time.Day; // 1 day
		uint calcDelay;
		List<ushort> activate = new();
		List<ushort> deactivate = new();

		for (ushort id = 1; id < _gameEvent.Length; ++id)
		{
			// must do the activating first, and after that the deactivating
			// so first queue it
			if (CheckOneGameEvent(id))
			{
				// if the world event is in NEXTPHASE state, and the time has passed to finish this event, then do so
				if (_gameEvent[id].state == GameEventState.WorldNextPhase && _gameEvent[id].nextstart <= currenttime)
				{
					// set this event to finished, null the nextstart time
					_gameEvent[id].state = GameEventState.WorldFinished;
					_gameEvent[id].nextstart = 0;
					// save the state of this gameevent
					SaveWorldEventStateToDB(id);

					// queue for deactivation
					if (IsActiveEvent(id))
						deactivate.Add(id);

					// go to next event, this no longer needs an event update timer
					continue;
				}
				else if (_gameEvent[id].state == GameEventState.WorldConditions && CheckOneGameEventConditions(id))
					// changed, save to DB the gameevent state, will be updated in next update cycle
				{
					SaveWorldEventStateToDB(id);
				}

				Log.Logger.Debug("GameEvent {0} is active", id);

				// queue for activation
				if (!IsActiveEvent(id))
					activate.Add(id);
			}
			else
			{
				Log.Logger.Debug("GameEvent {0} is not active", id);

				if (IsActiveEvent(id))
				{
					deactivate.Add(id);
				}
				else
				{
					if (!_isSystemInit)
					{
						var event_nid = (short)(-1 * id);
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

		Log.Logger.Information("Next game event check in {0} seconds.", nextEventDelay + 1);

		return (nextEventDelay + 1) * Time.InMilliseconds; // Add 1 second to be sure event has started/stopped at next call
	}

	public void HandleQuestComplete(uint quest_id)
	{
		// translate the quest to event and condition
		var questToEvent = _QuestToEventConditions.LookupByKey(quest_id);

		// quest is registered
		if (questToEvent != null)
		{
			var event_id = questToEvent.event_id;
			var condition = questToEvent.condition;
			var num = questToEvent.num;

			// the event is not active, so return, don't increase condition finishes
			if (!IsActiveEvent(event_id))
				return;

			// not in correct phase, return
			if (_gameEvent[event_id].state != GameEventState.WorldConditions)
				return;

			var eventFinishCond = _gameEvent[event_id].conditions.LookupByKey(condition);

			// condition is registered
			if (eventFinishCond != null)
				// increase the done count, only if less then the req
				if (eventFinishCond.done < eventFinishCond.reqNum)
				{
					eventFinishCond.done += num;

					// check max limit
					if (eventFinishCond.done > eventFinishCond.reqNum)
						eventFinishCond.done = eventFinishCond.reqNum;

					// save the change to db
					SQLTransaction trans = new();

					var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GAME_EVENT_CONDITION_SAVE);
					stmt.AddValue(0, event_id);
					stmt.AddValue(1, condition);
					trans.Append(stmt);

					stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_GAME_EVENT_CONDITION_SAVE);
					stmt.AddValue(0, event_id);
					stmt.AddValue(1, condition);
					stmt.AddValue(2, eventFinishCond.done);
					trans.Append(stmt);
					_characterDatabase.CommitTransaction(trans);

					// check if all conditions are met, if so, update the event state
					if (CheckOneGameEventConditions(event_id))
					{
						// changed, save to DB the gameevent state
						SaveWorldEventStateToDB(event_id);
						// force update events to set timer
						_worldManager.ForceGameEventUpdate();
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
			if (events[eventId].holiday_id == id)
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
		return _activeEvents;
	}

	public GameEventData[] GetEventMap()
	{
		return _gameEvent;
	}

	public bool IsActiveEvent(ushort event_id)
	{
		return _activeEvents.Contains(event_id);
	}

	bool CheckOneGameEvent(ushort entry)
	{
		switch (_gameEvent[entry].state)
		{
			default:
			case GameEventState.Normal:
			{
				var currenttime = _gameTime.CurrentGameTime;

				// Get the event information
				return _gameEvent[entry].start < currenttime && currenttime < _gameEvent[entry].end && (currenttime - _gameEvent[entry].start) % (_gameEvent[entry].occurence * Time.Minute) < _gameEvent[entry].length * Time.Minute;
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
				var currenttime = _gameTime.CurrentGameTime;

				foreach (var gameEventId in _gameEvent[entry].prerequisite_events)
					if ((_gameEvent[gameEventId].state != GameEventState.WorldNextPhase && _gameEvent[gameEventId].state != GameEventState.WorldFinished) || // if prereq not in nextphase or finished state, then can't start this one
						_gameEvent[gameEventId].nextstart > currenttime)                                                                                     // if not in nextphase state for long enough, can't start this one
						return false;

				// all prerequisite events are met
				// but if there are no prerequisites, this can be only activated through gm command
				return !(_gameEvent[entry].prerequisite_events.Empty());
			}
		}
	}

	void StartInternalEvent(ushort event_id)
	{
		if (event_id < 1 || event_id >= _gameEvent.Length)
			return;

		if (!_gameEvent[event_id].IsValid())
			return;

		if (_activeEvents.Contains(event_id))
			return;

		StartEvent(event_id);
	}

	void UnApplyEvent(ushort event_id)
	{
		Log.Logger.Information("GameEvent {0} \"{1}\" removed.", event_id, _gameEvent[event_id].description);
		//! Run SAI scripts with SMART_EVENT_GAME_EVENT_END
		RunSmartAIScripts(event_id, false);
		// un-spawn positive event tagged objects
		GameEventUnspawn((short)event_id);
		// spawn negative event tagget objects
		var event_nid = (short)(-1 * event_id);
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

	void ApplyNewEvent(ushort event_id)
	{
		var announce = _gameEvent[event_id].announce;

		if (announce == 1) // || (announce == 2 && WorldConfigEventAnnounce))
			_worldManager.SendWorldText(CypherStrings.Eventmessage, _gameEvent[event_id].description);

		Log.Logger.Information("GameEvent {0} \"{1}\" started.", event_id, _gameEvent[event_id].description);

		// spawn positive event tagget objects
		GameEventSpawn((short)event_id);
		// un-spawn negative event tagged objects
		var event_nid = (short)(-1 * event_id);
		GameEventUnspawn(event_nid);
		// Change equipement or model
		ChangeEquipOrModel((short)event_id, true);
		// Add quests that are events only to non event npc
		UpdateEventQuests(event_id, true);
		UpdateWorldStates(event_id, true);
		// add vendor items
		UpdateEventNPCVendor(event_id, true);
		// update bg holiday
		UpdateBattlegroundSettings();

		//! Run SAI scripts with SMART_EVENT_GAME_EVENT_START
		RunSmartAIScripts(event_id, true);

		// check for seasonal quest reset.
		_worldManager.ResetEventSeasonalQuests(event_id, GetLastStartTime(event_id));
	}

	void UpdateEventNPCFlags(ushort event_id)
    {
        // Send to map server
    }

    void UpdateBattlegroundSettings()
	{
        _battlegroundManager.ResetHolidays();

		foreach (var activeEventId in _activeEvents)
            _battlegroundManager.SetHolidayActive(_gameEventBattlegroundHolidays[activeEventId]);
	}

	void UpdateEventNPCVendor(ushort eventId, bool activate)
	{
		foreach (var npcEventVendor in _gameEventVendors[eventId])
			if (activate)
				_gameObjectManager.AddVendorItem(npcEventVendor.Key, npcEventVendor.Value, false);
			else
				_gameObjectManager.RemoveVendorItem(npcEventVendor.Key, npcEventVendor.Value.Item, npcEventVendor.Value.Type, false);
	}

	void GameEventSpawn(short event_id)
    {
        // Send to map server
    }

    void GameEventUnspawn(short event_id)
    {
        // Send to map server
    }

    void ChangeEquipOrModel(short event_id, bool activate)
    {
        // Send to map server
    }

    bool HasCreatureQuestActiveEventExcept(uint questId, ushort eventId)
	{
		foreach (var activeEventId in _activeEvents)
			if (activeEventId != eventId)
				foreach (var pair in _gameEventCreatureQuests[activeEventId])
					if (pair.Item2 == questId)
						return true;

		return false;
	}

	bool HasGameObjectQuestActiveEventExcept(uint questId, ushort eventId)
	{
		foreach (var activeEventId in _activeEvents)
			if (activeEventId != eventId)
				foreach (var pair in _gameEventGameObjectQuests[activeEventId])
					if (pair.Item2 == questId)
						return true;

		return false;
	}

	bool HasCreatureActiveEventExcept(ulong creatureId, ushort eventId)
	{
		foreach (var activeEventId in _activeEvents)
			if (activeEventId != eventId)
			{
				var internal_event_id = _gameEvent.Length + activeEventId - 1;

				foreach (var id in _gameEventCreatureGuids[internal_event_id])
					if (id == creatureId)
						return true;
			}

		return false;
	}

	bool HasGameObjectActiveEventExcept(ulong goId, ushort eventId)
	{
		foreach (var activeEventId in _activeEvents)
			if (activeEventId != eventId)
			{
				var internal_event_id = _gameEvent.Length + activeEventId - 1;

				foreach (var id in _gameEventGameobjectGuids[internal_event_id])
					if (id == goId)
						return true;
			}

		return false;
	}

	void UpdateEventQuests(ushort eventId, bool activate)
	{
		foreach (var pair in _gameEventCreatureQuests[eventId])
		{
			var CreatureQuestMap = _gameObjectManager.GetCreatureQuestRelationMapHACK();

			if (activate) // Add the pair(id, quest) to the multimap
			{
				CreatureQuestMap.Add(pair.Item1, pair.Item2);
			}
			else
			{
				if (!HasCreatureQuestActiveEventExcept(pair.Item2, eventId))
					// Remove the pair(id, quest) from the multimap
					CreatureQuestMap.Remove(pair.Item1, pair.Item2);
			}
		}

		foreach (var pair in _gameEventGameObjectQuests[eventId])
		{
			var GameObjectQuestMap = _gameObjectManager.GetGOQuestRelationMapHACK();

			if (activate) // Add the pair(id, quest) to the multimap
			{
				GameObjectQuestMap.Add(pair.Item1, pair.Item2);
			}
			else
			{
				if (!HasGameObjectQuestActiveEventExcept(pair.Item2, eventId))
					// Remove the pair(id, quest) from the multimap
					GameObjectQuestMap.Remove(pair.Item1, pair.Item2);
			}
		}
	}

	void UpdateWorldStates(ushort event_id, bool Activate)
	{
		var Event = _gameEvent[event_id];

		if (Event.holiday_id != HolidayIds.None)
		{
			var bgTypeId = _battlegroundManager.WeekendHolidayIdToBGType(Event.holiday_id);

			if (bgTypeId != BattlegroundTypeId.None)
			{
				var bl = _cliDb.BattlemasterListStorage.LookupByKey((uint)_battlegroundManager.WeekendHolidayIdToBGType(Event.holiday_id));

				if (bl != null)
					if (bl.HolidayWorldState != 0)
						_worldStateManager.SetValue(bl.HolidayWorldState, Activate ? 1 : 0, false, null);
			}
		}
	}

	bool CheckOneGameEventConditions(ushort event_id)
	{
		foreach (var pair in _gameEvent[event_id].conditions)
			if (pair.Value.done < pair.Value.reqNum)
				// return false if a condition doesn't match
				return false;

		// set the phase
		_gameEvent[event_id].state = GameEventState.WorldNextPhase;

		// set the followup events' start time
		if (_gameEvent[event_id].nextstart == 0)
		{
			var currenttime = _gameTime.CurrentGameTime;
			_gameEvent[event_id].nextstart = currenttime + _gameEvent[event_id].length * 60;
		}

		return true;
	}

	void SaveWorldEventStateToDB(ushort event_id)
	{
		SQLTransaction trans = new();

		var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GAME_EVENT_SAVE);
		stmt.AddValue(0, event_id);
		trans.Append(stmt);

		stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_GAME_EVENT_SAVE);
		stmt.AddValue(0, event_id);
		stmt.AddValue(1, (byte)_gameEvent[event_id].state);
		stmt.AddValue(2, _gameEvent[event_id].nextstart != 0 ? _gameEvent[event_id].nextstart : 0L);
		trans.Append(stmt);
		_characterDatabase.CommitTransaction(trans);
	}

	void SendWorldStateUpdate(Player player, ushort event_id)
	{
		foreach (var pair in _gameEvent[event_id].conditions)
		{
			if (pair.Value.done_world_state != 0)
				player.SendUpdateWorldState(pair.Value.done_world_state, (uint)(pair.Value.done));

			if (pair.Value.max_world_state != 0)
				player.SendUpdateWorldState(pair.Value.max_world_state, (uint)(pair.Value.reqNum));
		}
	}

	void RunSmartAIScripts(ushort event_id, bool activate)
    {
        // Send to map server
    }

    void SetHolidayEventTime(GameEventData gameEvent)
	{
		if (gameEvent.holidayStage == 0) // Ignore holiday
			return;

		var holiday = _cliDb.HolidaysStorage.LookupByKey((uint)gameEvent.holiday_id);

		if (holiday.Date[0] == 0 || holiday.Duration[0] == 0) // Invalid definitions
		{
			Log.Logger.Error($"Missing date or duration for holiday {gameEvent.holiday_id}.");

			return;
		}

		var stageIndex = (byte)(gameEvent.holidayStage - 1);
		gameEvent.length = (uint)(holiday.Duration[stageIndex] * Time.Hour / Time.Minute);

		long stageOffset = 0;

		for (var i = 0; i < stageIndex; ++i)
			stageOffset += holiday.Duration[i] * Time.Hour;

		switch (holiday.CalendarFilterType)
		{
			case -1:                                           // Yearly
				gameEvent.occurence = Time.Year / Time.Minute; // Not all too useful

				break;
			case 0: // Weekly
				gameEvent.occurence = Time.Week / Time.Minute;

				break;
			case 1: // Defined dates only (Darkmoon Faire)
				break;
			case 2: // Only used for looping events (Call to Arms)
				break;
		}

		if (holiday.Looping != 0)
		{
			gameEvent.occurence = 0;

			for (var i = 0; i < SharedConst.MaxHolidayDurations && holiday.Duration[i] != 0; ++i)
				gameEvent.occurence += (uint)(holiday.Duration[i] * Time.Hour / Time.Minute);
		}

		var singleDate = ((holiday.Date[0] >> 24) & 0x1F) == 31; // Events with fixed date within year have - 1

		var curTime = _gameTime.CurrentGameTime;

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

			if (curTime < startTime + gameEvent.length * Time.Minute)
			{
				gameEvent.start = startTime + stageOffset;

				break;
			}
			else if (singleDate)
			{
				var tmCopy = timeInfo.AddYears(Time.UnixTimeToDateTime(curTime).ToLocalTime().Year); // This year
				gameEvent.start = Time.DateTimeToUnixTime(tmCopy) + stageOffset;

				break;
			}
			else
			{
				// date is due and not a singleDate event, try with next DBC date (modified by holiday_dates)
				// if none is found we don't modify start date and use the one in game_event
			}
		}
	}

	long GetLastStartTime(ushort event_id)
	{
		if (event_id >= _gameEvent.Length)
			return 0;

		if (_gameEvent[event_id].state != GameEventState.Normal)
			return 0;

		var now = _gameTime.GetSystemTime;
		var eventInitialStart = Time.UnixTimeToDateTime(_gameEvent[event_id].start);
		var occurence = TimeSpan.FromMinutes(_gameEvent[event_id].occurence);
		var durationSinceLastStart = TimeSpan.FromTicks((now - eventInitialStart).Ticks % occurence.Ticks);

		return Time.DateTimeToUnixTime(now - durationSinceLastStart);
	}

	void AddActiveEvent(ushort event_id)
	{
		_activeEvents.Add(event_id);
	}

	void RemoveActiveEvent(ushort event_id)
	{
		_activeEvents.Remove(event_id);
	}
}

public class GameEventFinishCondition
{
	public float reqNum;          // required number // use float, since some events use percent
	public float done;            // done number
	public uint max_world_state;  // max resource count world state update id
	public uint done_world_state; // done resource count world state update id
}

public class GameEventQuestToEventConditionNum
{
	public ushort event_id;
	public uint condition;
	public float num;
}

public class GameEventData
{
	public long start;     // occurs after this time
	public long end;       // occurs before this time
	public long nextstart; // after this time the follow-up events count this phase completed
	public uint occurence; // time between end and start
	public uint length;    // length of the event (Time.Minutes) after finishing all conditions
	public HolidayIds holiday_id;
	public byte holidayStage;
	public GameEventState state;                                          // state of the game event, these are saved into the game_event table on change!
	public Dictionary<uint, GameEventFinishCondition> conditions = new(); // conditions to finish
	public List<ushort> prerequisite_events = new();                      // events that must be completed before starting this event
	public string description;
	public byte announce; // if 0 dont announce, if 1 announce, if 2 take config value

	public GameEventData()
	{
		start = 1;
	}

	public bool IsValid()
	{
		return length > 0 || state > GameEventState.Normal;
	}
}

public class ModelEquip
{
	public uint modelid;
	public uint modelid_prev;
	public byte equipment_id;
	public byte equipement_id_prev;
}

public enum GameEventState
{
	Normal = 0,          // standard game events
	WorldInactive = 1,   // not yet started
	WorldConditions = 2, // condition matching phase
	WorldNextPhase = 3,  // conditions are met, now 'length' timer to start next event
	WorldFinished = 4,   // next events are started, unapply this one
	Internal = 5         // never handled in update
}