// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Framework.Threading;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Garrisons;
using Game.Maps;
using Game.Common.Groups;
using Game.Common.Server;

namespace Game.Entities;

public class MapManager : Singleton<MapManager>
{
	readonly LoopSafeDoubleDictionary<uint, uint, Map> _maps = new();
	readonly IntervalTimer _timer = new();
	readonly object _mapsLock = new();
	readonly BitSet _freeInstanceIds = new(1);
	uint _gridCleanUpDelay;
	uint _nextInstanceId;
	LimitedThreadTaskManager _updater;
	uint _scheduledScripts;

	MapManager()
	{
		_gridCleanUpDelay = WorldConfig.GetUIntValue(WorldCfg.IntervalGridclean);
		_timer.Interval = WorldConfig.GetIntValue(WorldCfg.IntervalMapupdate);
	}

	public void Initialize()
	{
		var num_threads = WorldConfig.GetIntValue(WorldCfg.Numthreads);

		_updater = new LimitedThreadTaskManager(num_threads > 0 ? num_threads : 1);
	}

	public void InitializeVisibilityDistanceInfo()
	{
		foreach (var pair in _maps.Values)
			foreach (var map in pair.Values)
				map.InitVisibilityDistance();
	}

	/// <summary>
	///  create the instance if it's not created already
	///  the player is not actually added to the instance(only in InstanceMap::Add)
	/// </summary>
	/// <param name="mapId"> </param>
	/// <param name="player"> </param>
	/// <param name="loginInstanceId"> </param>
	/// <returns> the right instance for the object, based on its InstanceId </returns>
	public Map CreateMap(uint mapId, Player player)
	{
		if (!player)
			return null;

		var entry = CliDB.MapStorage.LookupByKey(mapId);

		if (entry == null)
			return null;

		lock (_mapsLock)
		{
			Map map = null;
			uint newInstanceId = 0; // instanceId of the resulting map

			if (entry.IsBattlegroundOrArena())
			{
				// instantiate or find existing bg map for player
				// the instance id is set in battlegroundid
				newInstanceId = player.BattlegroundId;

				if (newInstanceId == 0)
					return null;

				map = FindMap_i(mapId, newInstanceId);

				if (!map)
				{
					var bg = player.Battleground;

					if (bg != null)
					{
						map = CreateBattleground(mapId, newInstanceId, bg);
					}
					else
					{
						player.TeleportToBGEntryPoint();

						return null;
					}
				}
			}
			else if (entry.IsDungeon())
			{
				var group = player.Group;
				var difficulty = group != null ? group.GetDifficultyID(entry) : player.GetDifficultyId(entry);
				MapDb2Entries entries = new(entry, Global.DB2Mgr.GetDownscaledMapDifficultyData(mapId, ref difficulty));
				var instanceOwnerGuid = group != null ? group.GetRecentInstanceOwner(mapId) : player.GUID;
				var instanceLock = Global.InstanceLockMgr.FindActiveInstanceLock(instanceOwnerGuid, entries);

				if (instanceLock != null)
				{
					newInstanceId = instanceLock.GetInstanceId();

					// Reset difficulty to the one used in instance lock
					if (!entries.Map.IsFlexLocking())
						difficulty = instanceLock.GetDifficultyId();
				}
				else
				{
					// Try finding instance id for normal dungeon
					if (!entries.MapDifficulty.HasResetSchedule())
						newInstanceId = group ? group.GetRecentInstanceId(mapId) : player.GetRecentInstanceId(mapId);

					// If not found or instance is not a normal dungeon, generate new one
					if (newInstanceId == 0)
						newInstanceId = GenerateInstanceId();

					instanceLock = Global.InstanceLockMgr.CreateInstanceLockForNewInstance(instanceOwnerGuid, entries, newInstanceId);
				}

				// it is possible that the save exists but the map doesn't
				map = FindMap_i(mapId, newInstanceId);

				// is is also possible that instance id is already in use by another group for boss-based locks
				if (!entries.IsInstanceIdBound() && instanceLock != null && map != null && map.ToInstanceMap.InstanceLock != instanceLock)
				{
					newInstanceId = GenerateInstanceId();
					instanceLock.SetInstanceId(newInstanceId);
					map = null;
				}

				if (!map)
				{
					map = CreateInstance(mapId, newInstanceId, instanceLock, difficulty, player.TeamId, group);

					if (group)
						group.SetRecentInstance(mapId, instanceOwnerGuid, newInstanceId);
					else
						player.SetRecentInstance(mapId, newInstanceId);
				}
			}
			else if (entry.IsGarrison())
			{
				newInstanceId = (uint)player.GUID.Counter;
				map = FindMap_i(mapId, newInstanceId);

				if (!map)
					map = CreateGarrison(mapId, newInstanceId, player);
			}
			else
			{
				newInstanceId = 0;

				if (entry.IsSplitByFaction())
					newInstanceId = (uint)player.TeamId;

				map = FindMap_i(mapId, newInstanceId);

				if (!map)
					map = CreateWorldMap(mapId, newInstanceId);
			}

			if (map)
				_maps.Add(map.Id, map.InstanceId, map);

			return map;
		}
	}

	public Map FindMap(uint mapId, uint instanceId)
	{
		lock (_mapsLock)
		{
			return FindMap_i(mapId, instanceId);
		}
	}

	public uint FindInstanceIdForPlayer(uint mapId, Player player)
	{
		var entry = CliDB.MapStorage.LookupByKey(mapId);

		if (entry == null)
			return 0;

		if (entry.IsBattlegroundOrArena())
		{
			return player.BattlegroundId;
		}
		else if (entry.IsDungeon())
		{
			var group = player.Group;
			var difficulty = group != null ? group.GetDifficultyID(entry) : player.GetDifficultyId(entry);
			MapDb2Entries entries = new(entry, Global.DB2Mgr.GetDownscaledMapDifficultyData(mapId, ref difficulty));

			var instanceOwnerGuid = group ? group.GetRecentInstanceOwner(mapId) : player.GUID;
			var instanceLock = Global.InstanceLockMgr.FindActiveInstanceLock(instanceOwnerGuid, entries);
			uint newInstanceId = 0;

			if (instanceLock != null)
				newInstanceId = instanceLock.GetInstanceId();
			else if (!entries.MapDifficulty.HasResetSchedule()) // Try finding instance id for normal dungeon
				newInstanceId = group ? group.GetRecentInstanceId(mapId) : player.GetRecentInstanceId(mapId);

			if (newInstanceId == 0)
				return 0;

			var map = FindMap(mapId, newInstanceId);

			// is is possible that instance id is already in use by another group for boss-based locks
			if (!entries.IsInstanceIdBound() && instanceLock != null && map != null && map.ToInstanceMap.InstanceLock != instanceLock)
				return 0;

			return newInstanceId;
		}
		else if (entry.IsGarrison())
		{
			return (uint)player.GUID.Counter;
		}
		else
		{
			if (entry.IsSplitByFaction())
				return (uint)player.TeamId;

			return 0;
		}
	}

	public void Update(uint diff)
	{
		_timer.Update(diff);

		if (!_timer.Passed)
			return;

		var time = (uint)_timer.Current;

		foreach (var mapkvp in _maps)
			foreach (var instanceKvp in mapkvp.Value)
			{
				if (instanceKvp.Value.CanUnload(diff))
				{
					_updater.Schedule(() =>
					{
						if (DestroyMap(instanceKvp.Value))
							_maps.QueueRemove(mapkvp.Key, instanceKvp.Key);
					});

					continue;
				}

				_updater.Schedule(() => instanceKvp.Value.Update(time));
			}

		_updater.Wait();
		_maps.ExecuteRemove();

		foreach (var kvp in _maps.Values)
			foreach (var map in kvp.Values)
				_updater.Stage(() => map.DelayedUpdate(time));

		_updater.Wait();
		_timer.Current = 0;
	}

	public bool IsValidMAP(uint mapId)
	{
		return CliDB.MapStorage.ContainsKey(mapId);
	}

	public void UnloadAll()
	{
		// first unload maps
		foreach (var pair in _maps.Values)
			foreach (var map in pair.Values)
				map.UnloadAll();

		foreach (var pair in _maps.Values)
			foreach (var map in pair.Values)
				map.Dispose();

		_maps.Clear();

		if (_updater != null)
			_updater.Deactivate();
	}

	public uint GetNumInstances()
	{
		lock (_mapsLock)
		{
			return (uint)_maps.Sum(pair => pair.Value.Count(kvp => kvp.Value.IsDungeon));
		}
	}

	public uint GetNumPlayersInInstances()
	{
		lock (_mapsLock)
		{
			return (uint)_maps.Sum(pair => pair.Value.Sum(kvp => kvp.Value.IsDungeon ? kvp.Value.Players.Count : 0));
		}
	}

	public void InitInstanceIds()
	{
		_nextInstanceId = 1;

		ulong maxExistingInstanceId = 0;
		var result = DB.Characters.Query("SELECT IFNULL(MAX(instanceId), 0) FROM instance");

		if (!result.IsEmpty())
			maxExistingInstanceId = Math.Max(maxExistingInstanceId, result.Read<ulong>(0));

		result = DB.Characters.Query("SELECT IFNULL(MAX(instanceId), 0) FROM character_instance_lock");

		if (!result.IsEmpty())
			maxExistingInstanceId = Math.Max(maxExistingInstanceId, result.Read<ulong>(0));

		_freeInstanceIds.Length = (int)(maxExistingInstanceId + 2); // make space for one extra to be able to access [_nextInstanceId] index in case all slots are taken

		// never allow 0 id
		_freeInstanceIds[0] = false;
	}

	public void RegisterInstanceId(uint instanceId)
	{
		_freeInstanceIds[(int)instanceId] = false;

		// Instances are pulled in ascending order from db and nextInstanceId is initialized with 1,
		// so if the instance id is used, increment until we find the first unused one for a potential new instance
		if (_nextInstanceId == instanceId)
			++_nextInstanceId;
	}

	public uint GenerateInstanceId()
	{
		if (_nextInstanceId == 0xFFFFFFFF)
		{
			Log.outError(LogFilter.Maps, "Instance ID overflow!! Can't continue, shutting down server. ");
			Global.WorldMgr.StopNow();

			return _nextInstanceId;
		}

		var newInstanceId = _nextInstanceId;
		_freeInstanceIds[(int)newInstanceId] = false;

		// Find the lowest available id starting from the current NextInstanceId (which should be the lowest according to the logic in FreeInstanceId()
		var nextFreeId = -1;

		for (var i = (int)_nextInstanceId++; i < _freeInstanceIds.Length; i++)
			if (_freeInstanceIds[i])
			{
				nextFreeId = i;

				break;
			}

		if (nextFreeId == -1)
		{
			_nextInstanceId = (uint)_freeInstanceIds.Length;
			_freeInstanceIds.Length += 1;
			_freeInstanceIds[(int)_nextInstanceId] = true;
		}
		else
		{
			_nextInstanceId = (uint)nextFreeId;
		}

		return newInstanceId;
	}

	public void FreeInstanceId(uint instanceId)
	{
		// If freed instance id is lower than the next id available for new instances, use the freed one instead
		_nextInstanceId = Math.Min(instanceId, _nextInstanceId);
		_freeInstanceIds[(int)instanceId] = true;
	}

	public void SetGridCleanUpDelay(uint t)
	{
		if (t < MapConst.MinGridDelay)
			_gridCleanUpDelay = MapConst.MinGridDelay;
		else
			_gridCleanUpDelay = t;
	}

	public void SetMapUpdateInterval(int t)
	{
		if (t < MapConst.MinMapUpdateDelay)
			t = MapConst.MinMapUpdateDelay;

		_timer.Interval = t;
		_timer.Reset();
	}

	public uint GetNextInstanceId()
	{
		return _nextInstanceId;
	}

	public void SetNextInstanceId(uint nextInstanceId)
	{
		_nextInstanceId = nextInstanceId;
	}

	public void DoForAllMaps(Action<Map> worker)
	{
		lock (_mapsLock)
		{
			foreach (var kvp in _maps.Values)
				foreach (var map in kvp.Values)
					worker(map);
		}
	}

	public void DoForAllMapsWithMapId(uint mapId, Action<Map> worker)
	{
		lock (_mapsLock)
		{
			if (_maps.TryGetValue(mapId, out var instanceDict))
				foreach (var kvp in instanceDict)
					if (kvp.Key >= 0)
						worker(kvp.Value);
		}
	}

	public void AddSC_BuiltInScripts()
	{
		foreach (var (_, mapEntry) in CliDB.MapStorage)
			if (mapEntry.IsWorldMap() && mapEntry.IsSplitByFaction())
				new SplitByFactionMapScript($"world_map_set_faction_worldstates_{mapEntry.Id}", mapEntry.Id);
	}

	public void IncreaseScheduledScriptsCount()
	{
		++_scheduledScripts;
	}

	public void DecreaseScheduledScriptCount()
	{
		--_scheduledScripts;
	}

	public void DecreaseScheduledScriptCount(uint count)
	{
		_scheduledScripts -= count;
	}

	public bool IsScriptScheduled()
	{
		return _scheduledScripts > 0;
	}

	Map FindMap_i(uint mapId, uint instanceId)
	{
		return _maps.TryGetValue(mapId, instanceId, out var map) ? map : null;
	}

	Map CreateWorldMap(uint mapId, uint instanceId)
	{
		var map = new Map(mapId, _gridCleanUpDelay, instanceId, Difficulty.None);
		map.LoadRespawnTimes();
		map.LoadCorpseData();

		if (WorldConfig.GetBoolValue(WorldCfg.BasemapLoadGrids))
			map.LoadAllCells();

		return map;
	}

	InstanceMap CreateInstance(uint mapId, uint instanceId, InstanceLock instanceLock, Difficulty difficulty, int team, PlayerGroup group)
	{
		// make sure we have a valid map id
		var entry = CliDB.MapStorage.LookupByKey(mapId);

		if (entry == null)
		{
			Log.outError(LogFilter.Maps, $"CreateInstance: no entry for map {mapId}");

			//ABORT();
			return null;
		}

		// some instances only have one difficulty
		Global.DB2Mgr.GetDownscaledMapDifficultyData(mapId, ref difficulty);

		Log.outDebug(LogFilter.Maps, $"MapInstanced::CreateInstance: {(instanceLock?.GetInstanceId() != 0 ? "" : "new ")}map instance {instanceId} for {mapId} created with difficulty {difficulty}");

		var map = new InstanceMap(mapId, _gridCleanUpDelay, instanceId, difficulty, team, instanceLock);

		map.LoadRespawnTimes();
		map.LoadCorpseData();

		if (group != null)
			map.TrySetOwningGroup(group);

		map.CreateInstanceData();
		map.SetInstanceScenario(Global.ScenarioMgr.CreateInstanceScenario(map, team));

		if (WorldConfig.GetBoolValue(WorldCfg.InstancemapLoadGrids))
			map.LoadAllCells();

		return map;
	}

	BattlegroundMap CreateBattleground(uint mapId, uint instanceId, Battleground bg)
	{
		Log.outDebug(LogFilter.Maps, $"MapInstanced::CreateBattleground: map bg {instanceId} for {mapId} created.");

		var map = new BattlegroundMap(mapId, _gridCleanUpDelay, instanceId, Difficulty.None);
		map.SetBG(bg);
		bg.SetBgMap(map);

		return map;
	}

	GarrisonMap CreateGarrison(uint mapId, uint instanceId, Player owner)
	{
		var map = new GarrisonMap(mapId, _gridCleanUpDelay, instanceId, owner.GUID);

		return map;
	}

	bool DestroyMap(Map map)
	{
		map.RemoveAllPlayers();

		if (map.HavePlayers)
			return false;

		map.UnloadAll();

		// Free up the instance id and allow it to be reused for normal dungeons, bgs and arenas
		if (map.IsBattlegroundOrArena || (map.IsDungeon && !map.MapDifficulty.HasResetSchedule()))
			FreeInstanceId(map.InstanceId);

		// erase map
		map.Dispose();

		return true;
	}
}

// hack to allow conditions to access what faction owns the map (these worldstates should not be set on these maps)