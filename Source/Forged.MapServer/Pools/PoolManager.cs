// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Maps;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Pools;

public class PoolManager : Singleton<PoolManager>
{
	public enum QuestTypes
	{
		None = 0,
		Daily = 1,
		Weekly = 2
	}

	public MultiMap<uint, uint> QuestCreatureRelation = new();
	public MultiMap<uint, uint> QuestGORelation = new();
	readonly Dictionary<uint, PoolTemplateData> _poolTemplate = new();
	readonly Dictionary<uint, PoolGroup<Creature>> _poolCreatureGroups = new();
	readonly Dictionary<uint, PoolGroup<GameObject>> _poolGameobjectGroups = new();
	readonly Dictionary<uint, PoolGroup<Pool>> _poolPoolGroups = new();
	readonly Dictionary<ulong, uint> _creatureSearchMap = new();
	readonly Dictionary<ulong, uint> _gameobjectSearchMap = new();
	readonly Dictionary<ulong, uint> _poolSearchMap = new();
	readonly MultiMap<uint, uint> _autoSpawnPoolsPerMap = new();
	PoolManager() { }

	public void Initialize()
	{
		_gameobjectSearchMap.Clear();
		_creatureSearchMap.Clear();
	}

	public void LoadFromDB()
	{
		// Pool templates
		{
			var oldMSTime = Time.MSTime;

			var result = DB.World.Query("SELECT entry, max_limit FROM pool_template");

			if (result.IsEmpty())
			{
				_poolTemplate.Clear();
				Log.Logger.Information("Loaded 0 object pools. DB table `pool_template` is empty.");

				return;
			}

			uint count = 0;

			do
			{
				var pool_id = result.Read<uint>(0);

				PoolTemplateData pPoolTemplate = new()
				{
					MaxLimit = result.Read<uint>(1),
					MapId = -1
				};

				_poolTemplate[pool_id] = pPoolTemplate;
				++count;
			} while (result.NextRow());

			Log.Logger.Information("Loaded {0} objects pools in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
		}

		// Creatures

		Log.Logger.Information("Loading Creatures Pooling Data...");

		{
			var oldMSTime = Time.MSTime;

			//                                         1        2            3
			var result = DB.World.Query("SELECT spawnId, poolSpawnId, chance FROM pool_members WHERE type = 0");

			if (result.IsEmpty())
			{
				Log.Logger.Information("Loaded 0 creatures in  pools. DB table `pool_creature` is empty.");
			}
			else
			{
				uint count = 0;

				do
				{
					var guid = result.Read<ulong>(0);
					var pool_id = result.Read<uint>(1);
					var chance = result.Read<float>(2);

					var data = Global.ObjectMgr.GetCreatureData(guid);

					if (data == null)
					{
						Log.Logger.Error("`pool_creature` has a non existing creature spawn (GUID: {0}) defined for pool id ({1}), skipped.", guid, pool_id);

						continue;
					}

					if (!_poolTemplate.ContainsKey(pool_id))
					{
						Log.Logger.Error("`pool_creature` pool id ({0}) is not in `pool_template`, skipped.", pool_id);

						continue;
					}

					if (chance < 0 || chance > 100)
					{
						Log.Logger.Error("`pool_creature` has an invalid chance ({0}) for creature guid ({1}) in pool id ({2}), skipped.", chance, guid, pool_id);

						continue;
					}

					var pPoolTemplate = _poolTemplate[pool_id];

					if (pPoolTemplate.MapId == -1)
						pPoolTemplate.MapId = (int)data.MapId;

					if (pPoolTemplate.MapId != data.MapId)
					{
						Log.Logger.Error($"`pool_creature` has creature spawns on multiple different maps for creature guid ({guid}) in pool id ({pool_id}), skipped.");

						continue;
					}

					PoolObject plObject = new(guid, chance);

					if (!_poolCreatureGroups.ContainsKey(pool_id))
						_poolCreatureGroups[pool_id] = new PoolGroup<Creature>();

					var cregroup = _poolCreatureGroups[pool_id];
					cregroup.SetPoolId(pool_id);
					cregroup.AddEntry(plObject, pPoolTemplate.MaxLimit);

					_creatureSearchMap.Add(guid, pool_id);
					++count;
				} while (result.NextRow());

				Log.Logger.Information("Loaded {0} creatures in pools in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
			}
		}

		// Gameobjects

		Log.Logger.Information("Loading Gameobject Pooling Data...");

		{
			var oldMSTime = Time.MSTime;

			//                                         1        2            3
			var result = DB.World.Query("SELECT spawnId, poolSpawnId, chance FROM pool_members WHERE type = 1");

			if (result.IsEmpty())
			{
				Log.Logger.Information("Loaded 0 gameobjects in  pools. DB table `pool_gameobject` is empty.");
			}
			else
			{
				uint count = 0;

				do
				{
					var guid = result.Read<ulong>(0);
					var pool_id = result.Read<uint>(1);
					var chance = result.Read<float>(2);

					var data = Global.ObjectMgr.GetGameObjectData(guid);

					if (data == null)
					{
						Log.Logger.Error("`pool_gameobject` has a non existing gameobject spawn (GUID: {0}) defined for pool id ({1}), skipped.", guid, pool_id);

						continue;
					}

					if (!_poolTemplate.ContainsKey(pool_id))
					{
						Log.Logger.Error("`pool_gameobject` pool id ({0}) is not in `pool_template`, skipped.", pool_id);

						continue;
					}

					if (chance < 0 || chance > 100)
					{
						Log.Logger.Error("`pool_gameobject` has an invalid chance ({0}) for gameobject guid ({1}) in pool id ({2}), skipped.", chance, guid, pool_id);

						continue;
					}

					var pPoolTemplate = _poolTemplate[pool_id];

					if (pPoolTemplate.MapId == -1)
						pPoolTemplate.MapId = (int)data.MapId;

					if (pPoolTemplate.MapId != data.MapId)
					{
						Log.Logger.Error($"`pool_gameobject` has gameobject spawns on multiple different maps for gameobject guid ({guid}) in pool id ({pool_id}), skipped.");

						continue;
					}

					PoolObject plObject = new(guid, chance);

					if (!_poolGameobjectGroups.ContainsKey(pool_id))
						_poolGameobjectGroups[pool_id] = new PoolGroup<GameObject>();

					var gogroup = _poolGameobjectGroups[pool_id];
					gogroup.SetPoolId(pool_id);
					gogroup.AddEntry(plObject, pPoolTemplate.MaxLimit);

					_gameobjectSearchMap.Add(guid, pool_id);
					++count;
				} while (result.NextRow());

				Log.Logger.Information("Loaded {0} gameobject in pools in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
			}
		}

		// Pool of pools

		Log.Logger.Information("Loading Mother Pooling Data...");

		{
			var oldMSTime = Time.MSTime;

			//                                         1        2            3
			var result = DB.World.Query("SELECT spawnId, poolSpawnId, chance FROM pool_members WHERE type = 2");

			if (result.IsEmpty())
			{
				Log.Logger.Information("Loaded 0 pools in pools");
			}
			else
			{
				uint count = 0;

				do
				{
					var child_pool_id = result.Read<uint>(0);
					var mother_pool_id = result.Read<uint>(1);
					var chance = result.Read<float>(2);

					if (!_poolTemplate.ContainsKey(mother_pool_id))
					{
						Log.Logger.Error("`pool_pool` mother_pool id ({0}) is not in `pool_template`, skipped.", mother_pool_id);

						continue;
					}

					if (!_poolTemplate.ContainsKey(child_pool_id))
					{
						Log.Logger.Error("`pool_pool` included pool_id ({0}) is not in `pool_template`, skipped.", child_pool_id);

						continue;
					}

					if (mother_pool_id == child_pool_id)
					{
						Log.Logger.Error("`pool_pool` pool_id ({0}) includes itself, dead-lock detected, skipped.", child_pool_id);

						continue;
					}

					if (chance < 0 || chance > 100)
					{
						Log.Logger.Error("`pool_pool` has an invalid chance ({0}) for pool id ({1}) in mother pool id ({2}), skipped.", chance, child_pool_id, mother_pool_id);

						continue;
					}

					var pPoolTemplateMother = _poolTemplate[mother_pool_id];
					PoolObject plObject = new(child_pool_id, chance);

					if (!_poolPoolGroups.ContainsKey(mother_pool_id))
						_poolPoolGroups[mother_pool_id] = new PoolGroup<Pool>();

					var plgroup = _poolPoolGroups[mother_pool_id];
					plgroup.SetPoolId(mother_pool_id);
					plgroup.AddEntry(plObject, pPoolTemplateMother.MaxLimit);

					_poolSearchMap.Add(child_pool_id, mother_pool_id);
					++count;
				} while (result.NextRow());

				// Now check for circular reference
				// All pool_ids are in pool_template
				foreach (var (id, poolData) in _poolTemplate)
				{
					List<uint> checkedPools = new();
					var poolItr = _poolSearchMap.LookupByKey(id);

					while (poolItr != 0)
					{
						if (poolData.MapId != -1)
						{
							if (_poolTemplate[poolItr].MapId == -1)
								_poolTemplate[poolItr].MapId = poolData.MapId;

							if (_poolTemplate[poolItr].MapId != poolData.MapId)
							{
								Log.Logger.Error($"`pool_pool` has child pools on multiple maps in pool id ({poolItr}), skipped.");
								_poolPoolGroups[poolItr].RemoveOneRelation(id);
								_poolSearchMap.Remove(poolItr);
								--count;

								break;
							}
						}

						checkedPools.Add(id);

						if (checkedPools.Contains(poolItr))
						{
							var ss = "The pool(s) ";

							foreach (var itr in checkedPools)
								ss += $"{itr} ";

							ss += $"create(s) a circular reference, which can cause the server to freeze.\nRemoving the last link between mother pool {id} and child pool {poolItr}";
							Log.Logger.Error(ss);
							_poolPoolGroups[poolItr].RemoveOneRelation(id);
							_poolSearchMap.Remove(poolItr);
							--count;

							break;
						}

						poolItr = _poolSearchMap.LookupByKey(poolItr);
					}
				}

				Log.Logger.Information("Loaded {0} pools in mother pools in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
			}
		}

		foreach (var (poolId, templateData) in _poolTemplate)
			if (IsEmpty(poolId))
			{
				Log.Logger.Error($"Pool Id {poolId} is empty (has no creatures and no gameobects and either no child pools or child pools are all empty. The pool will not be spawned");

				continue;
			}

		// The initialize method will spawn all pools not in an event and not in another pool, this is why there is 2 left joins with 2 null checks
		Log.Logger.Information("Starting objects pooling system...");

		{
			var oldMSTime = Time.MSTime;

			var result = DB.World.Query("SELECT DISTINCT pool_template.entry, pool_members.spawnId, pool_members.poolSpawnId FROM pool_template" +
										" LEFT JOIN game_event_pool ON pool_template.entry=game_event_pool.pool_entry" +
										" LEFT JOIN pool_members ON pool_members.type = 2 AND pool_template.entry = pool_members.spawnId WHERE game_event_pool.pool_entry IS NULL");

			if (result.IsEmpty())
			{
				Log.Logger.Information("Pool handling system initialized, 0 pools spawned.");
			}
			else
			{
				uint count = 0;

				do
				{
					var pool_entry = result.Read<uint>(0);
					var pool_pool_id = result.Read<uint>(1);

					if (IsEmpty(pool_entry))
						continue;

					if (!CheckPool(pool_entry))
					{
						if (pool_pool_id != 0)
							// The pool is a child pool in pool_pool table. Ideally we should remove it from the pool handler to ensure it never gets spawned,
							// however that could recursively invalidate entire chain of mother pools. It can be done in the future but for now we'll do nothing.
							Log.Logger.Error("Pool Id {0} has no equal chance pooled entites defined and explicit chance sum is not 100. This broken pool is a child pool of Id {1} and cannot be safely removed.", pool_entry, result.Read<uint>(2));
						else
							Log.Logger.Error("Pool Id {0} has no equal chance pooled entites defined and explicit chance sum is not 100. The pool will not be spawned.", pool_entry);

						continue;
					}

					// Don't spawn child pools, they are spawned recursively by their parent pools
					if (pool_pool_id == 0)
					{
						_autoSpawnPoolsPerMap.Add((uint)_poolTemplate[pool_entry].MapId, pool_entry);
						count++;
					}
				} while (result.NextRow());

				Log.Logger.Information("Pool handling system initialized, {0} pools will be spawned by default in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
			}
		}
	}

	public void SpawnPool(SpawnedPoolData spawnedPoolData, uint pool_id)
	{
		SpawnPool<Pool>(spawnedPoolData, pool_id, 0);
		SpawnPool<GameObject>(spawnedPoolData, pool_id, 0);
		SpawnPool<Creature>(spawnedPoolData, pool_id, 0);
	}

	public void DespawnPool(SpawnedPoolData spawnedPoolData, uint pool_id, bool alwaysDeleteRespawnTime = false)
	{
		if (_poolCreatureGroups.ContainsKey(pool_id) && !_poolCreatureGroups[pool_id].IsEmpty())
			_poolCreatureGroups[pool_id].DespawnObject(spawnedPoolData, 0, alwaysDeleteRespawnTime);

		if (_poolGameobjectGroups.ContainsKey(pool_id) && !_poolGameobjectGroups[pool_id].IsEmpty())
			_poolGameobjectGroups[pool_id].DespawnObject(spawnedPoolData, 0, alwaysDeleteRespawnTime);

		if (_poolPoolGroups.ContainsKey(pool_id) && !_poolPoolGroups[pool_id].IsEmpty())
			_poolPoolGroups[pool_id].DespawnObject(spawnedPoolData, 0, alwaysDeleteRespawnTime);
	}

	public bool IsEmpty(uint pool_id)
	{
		if (_poolGameobjectGroups.TryGetValue(pool_id, out var gameobjectPool) && !gameobjectPool.IsEmptyDeepCheck())
			return false;

		if (_poolCreatureGroups.TryGetValue(pool_id, out var creaturePool) && !creaturePool.IsEmptyDeepCheck())
			return false;

		if (_poolPoolGroups.TryGetValue(pool_id, out var pool) && !pool.IsEmptyDeepCheck())
			return false;

		return true;
	}

	public bool CheckPool(uint pool_id)
	{
		if (_poolGameobjectGroups.ContainsKey(pool_id) && !_poolGameobjectGroups[pool_id].CheckPool())
			return false;

		if (_poolCreatureGroups.ContainsKey(pool_id) && !_poolCreatureGroups[pool_id].CheckPool())
			return false;

		if (_poolPoolGroups.ContainsKey(pool_id) && !_poolPoolGroups[pool_id].CheckPool())
			return false;

		return true;
	}

	public void UpdatePool<T>(SpawnedPoolData spawnedPoolData, uint pool_id, ulong db_guid_or_pool_id)
	{
		var motherpoolid = IsPartOfAPool<Pool>(pool_id);

		if (motherpoolid != 0)
			SpawnPool<Pool>(spawnedPoolData, motherpoolid, pool_id);
		else
			SpawnPool<T>(spawnedPoolData, pool_id, db_guid_or_pool_id);
	}

	public void UpdatePool(SpawnedPoolData spawnedPoolData, uint pool_id, SpawnObjectType type, ulong spawnId)
	{
		switch (type)
		{
			case SpawnObjectType.Creature:
				UpdatePool<Creature>(spawnedPoolData, pool_id, spawnId);

				break;
			case SpawnObjectType.GameObject:
				UpdatePool<GameObject>(spawnedPoolData, pool_id, spawnId);

				break;
		}
	}

	public SpawnedPoolData InitPoolsForMap(Map map)
	{
		SpawnedPoolData spawnedPoolData = new(map);
		var poolIds = _autoSpawnPoolsPerMap.LookupByKey(spawnedPoolData.Map.Id);

		if (poolIds != null)
			foreach (var poolId in poolIds)
				SpawnPool(spawnedPoolData, poolId);

		return spawnedPoolData;
	}

	public PoolTemplateData GetPoolTemplate(uint pool_id)
	{
		return _poolTemplate.LookupByKey(pool_id);
	}

	public uint IsPartOfAPool<T>(ulong db_guid)
	{
		switch (typeof(T).Name)
		{
			case "Creature":
				return _creatureSearchMap.LookupByKey(db_guid);
			case "GameObject":
				return _gameobjectSearchMap.LookupByKey(db_guid);
			case "Pool":
				return _poolSearchMap.LookupByKey(db_guid);
		}

		return 0;
	}

	// Selects proper template overload to call based on passed type
	public uint IsPartOfAPool(SpawnObjectType type, ulong spawnId)
	{
		switch (type)
		{
			case SpawnObjectType.Creature:
				return IsPartOfAPool<Creature>(spawnId);
			case SpawnObjectType.GameObject:
				return IsPartOfAPool<GameObject>(spawnId);
			case SpawnObjectType.AreaTrigger:
				return 0;
			default:
				return 0;
		}
	}

	public bool IsSpawnedObject<T>(ulong db_guid_or_pool_id)
	{
		switch (typeof(T).Name)
		{
			case "Creature":
				return _creatureSearchMap.ContainsKey(db_guid_or_pool_id);
			case "GameObject":
				return _gameobjectSearchMap.ContainsKey(db_guid_or_pool_id);
			case "Pool":
				return _poolSearchMap.ContainsKey(db_guid_or_pool_id);
		}

		return false;
	}

	void SpawnPool<T>(SpawnedPoolData spawnedPoolData, uint pool_id, ulong db_guid)
	{
		switch (typeof(T).Name)
		{
			case "Creature":
				if (_poolCreatureGroups.ContainsKey(pool_id) && !_poolCreatureGroups[pool_id].IsEmpty())
					_poolCreatureGroups[pool_id].SpawnObject(spawnedPoolData, _poolTemplate[pool_id].MaxLimit, db_guid);

				break;
			case "GameObject":
				if (_poolGameobjectGroups.ContainsKey(pool_id) && !_poolGameobjectGroups[pool_id].IsEmpty())
					_poolGameobjectGroups[pool_id].SpawnObject(spawnedPoolData, _poolTemplate[pool_id].MaxLimit, db_guid);

				break;
			case "Pool":
				if (_poolPoolGroups.ContainsKey(pool_id) && !_poolPoolGroups[pool_id].IsEmpty())
					_poolPoolGroups[pool_id].SpawnObject(spawnedPoolData, _poolTemplate[pool_id].MaxLimit, db_guid);

				break;
		}
	}
}

// for Pool of Pool case