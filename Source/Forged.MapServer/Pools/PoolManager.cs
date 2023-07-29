// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Pools;

public class PoolManager
{
    public enum QuestTypes
    {
        None = 0,
        Daily = 1,
        Weekly = 2
    }

    public MultiMap<uint, uint> QuestCreatureRelation = new();

    public MultiMap<uint, uint> QuestGORelation = new();

    private readonly MultiMap<uint, uint> _autoSpawnPoolsPerMap = new();

    private readonly CreatureFactory _creatureFactory;
    private readonly Dictionary<ulong, uint> _creatureSearchMap = new();

    private readonly GameObjectFactory _gameObjectFactory;
    private readonly Dictionary<ulong, uint> _gameobjectSearchMap = new();

    private readonly GameObjectManager _objectManager;
    private readonly Dictionary<uint, PoolGroup<Creature>> _poolCreatureGroups = new();

    private readonly Dictionary<uint, PoolGroup<GameObject>> _poolGameobjectGroups = new();

    private readonly Dictionary<uint, PoolGroup<Pool>> _poolPoolGroups = new();

    private readonly Dictionary<ulong, uint> _poolSearchMap = new();

    private readonly Dictionary<uint, PoolTemplateData> _poolTemplate = new();

    private readonly WorldDatabase _worldDatabase;

    public PoolManager(WorldDatabase worldDatabase, GameObjectManager objectManager, CreatureFactory creatureFactory, GameObjectFactory gameObjectFactory)
    {
        _worldDatabase = worldDatabase;
        _objectManager = objectManager;
        _creatureFactory = creatureFactory;
        _gameObjectFactory = gameObjectFactory;
    }

    public bool CheckPool(uint poolID)
    {
        if (_poolGameobjectGroups.ContainsKey(poolID) && !_poolGameobjectGroups[poolID].CheckPool())
            return false;

        if (_poolCreatureGroups.ContainsKey(poolID) && !_poolCreatureGroups[poolID].CheckPool())
            return false;

        if (_poolPoolGroups.ContainsKey(poolID) && !_poolPoolGroups[poolID].CheckPool())
            return false;

        return true;
    }

    public void DespawnPool(SpawnedPoolData spawnedPoolData, uint poolID, bool alwaysDeleteRespawnTime = false)
    {
        if (_poolCreatureGroups.ContainsKey(poolID) && !_poolCreatureGroups[poolID].IsEmpty())
            _poolCreatureGroups[poolID].DespawnObject(spawnedPoolData, 0, alwaysDeleteRespawnTime);

        if (_poolGameobjectGroups.ContainsKey(poolID) && !_poolGameobjectGroups[poolID].IsEmpty())
            _poolGameobjectGroups[poolID].DespawnObject(spawnedPoolData, 0, alwaysDeleteRespawnTime);

        if (_poolPoolGroups.ContainsKey(poolID) && !_poolPoolGroups[poolID].IsEmpty())
            _poolPoolGroups[poolID].DespawnObject(spawnedPoolData, 0, alwaysDeleteRespawnTime);
    }

    public PoolTemplateData GetPoolTemplate(uint poolID)
    {
        return _poolTemplate.LookupByKey(poolID);
    }

    public void Initialize()
    {
        _gameobjectSearchMap.Clear();
        _creatureSearchMap.Clear();
    }

    public SpawnedPoolData InitPoolsForMap(Map map)
    {
        SpawnedPoolData spawnedPoolData = new(map);

        if (_autoSpawnPoolsPerMap.TryGetValue(spawnedPoolData.Map.Id, out var poolIds))
            foreach (var poolId in poolIds)
                SpawnPool(spawnedPoolData, poolId);

        return spawnedPoolData;
    }

    public bool IsEmpty(uint poolID)
    {
        if (_poolGameobjectGroups.TryGetValue(poolID, out var gameobjectPool) && !gameobjectPool.IsEmptyDeepCheck())
            return false;

        if (_poolCreatureGroups.TryGetValue(poolID, out var creaturePool) && !creaturePool.IsEmptyDeepCheck())
            return false;

        if (_poolPoolGroups.TryGetValue(poolID, out var pool) && !pool.IsEmptyDeepCheck())
            return false;

        return true;
    }

    public uint IsPartOfAPool<T>(ulong dbGuid)
    {
        return typeof(T).Name switch
        {
            "Creature"   => _creatureSearchMap.LookupByKey(dbGuid),
            "GameObject" => _gameobjectSearchMap.LookupByKey(dbGuid),
            "Pool"       => _poolSearchMap.LookupByKey(dbGuid),
            _            => 0
        };
    }

    // Selects proper template overload to call based on passed type
    public uint IsPartOfAPool(SpawnObjectType type, ulong spawnId)
    {
        return type switch
        {
            SpawnObjectType.Creature    => IsPartOfAPool<Creature>(spawnId),
            SpawnObjectType.GameObject  => IsPartOfAPool<GameObject>(spawnId),
            SpawnObjectType.AreaTrigger => 0,
            _                           => 0
        };
    }

    public bool IsSpawnedObject<T>(ulong dbGuidOrPoolID)
    {
        return typeof(T).Name switch
        {
            "Creature"   => _creatureSearchMap.ContainsKey(dbGuidOrPoolID),
            "GameObject" => _gameobjectSearchMap.ContainsKey(dbGuidOrPoolID),
            "Pool"       => _poolSearchMap.ContainsKey(dbGuidOrPoolID),
            _            => false
        };
    }

    public void LoadFromDB()
    {
        // Pool templates
        {
            var oldMSTime = Time.MSTime;

            var result = _worldDatabase.Query("SELECT entry, max_limit FROM pool_template");

            if (result.IsEmpty())
            {
                _poolTemplate.Clear();
                Log.Logger.Information("Loaded 0 object pools. DB table `pool_template` is empty.");

                return;
            }

            uint count = 0;

            do
            {
                var poolID = result.Read<uint>(0);

                PoolTemplateData pPoolTemplate = new()
                {
                    MaxLimit = result.Read<uint>(1),
                    MapId = -1
                };

                _poolTemplate[poolID] = pPoolTemplate;
                ++count;
            } while (result.NextRow());

            Log.Logger.Information("Loaded {0} objects pools in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        // Creatures

        Log.Logger.Information("Loading Creatures Pooling Data...");

        {
            var oldMSTime = Time.MSTime;

            //                                         1        2            3
            var result = _worldDatabase.Query("SELECT spawnId, poolSpawnId, chance FROM pool_members WHERE type = 0");

            if (result.IsEmpty())
                Log.Logger.Information("Loaded 0 creatures in  pools. DB table `pool_creature` is empty.");
            else
            {
                uint count = 0;

                do
                {
                    var guid = result.Read<ulong>(0);
                    var poolID = result.Read<uint>(1);
                    var chance = result.Read<float>(2);

                    var data = _objectManager.SpawnDataCacheRouter.CreatureDataCache.GetCreatureData(guid);

                    if (data == null)
                    {
                        Log.Logger.Error("`pool_creature` has a non existing creature spawn (GUID: {0}) defined for pool id ({1}), skipped.", guid, poolID);

                        continue;
                    }

                    if (!_poolTemplate.ContainsKey(poolID))
                    {
                        Log.Logger.Error("`pool_creature` pool id ({0}) is not in `pool_template`, skipped.", poolID);

                        continue;
                    }

                    if (chance is < 0 or > 100)
                    {
                        Log.Logger.Error("`pool_creature` has an invalid chance ({0}) for creature guid ({1}) in pool id ({2}), skipped.", chance, guid, poolID);

                        continue;
                    }

                    var pPoolTemplate = _poolTemplate[poolID];

                    if (pPoolTemplate.MapId == -1)
                        pPoolTemplate.MapId = (int)data.MapId;

                    if (pPoolTemplate.MapId != data.MapId)
                    {
                        Log.Logger.Error($"`pool_creature` has creature spawns on multiple different maps for creature guid ({guid}) in pool id ({poolID}), skipped.");

                        continue;
                    }

                    PoolObject plObject = new(guid, chance);

                    if (!_poolCreatureGroups.ContainsKey(poolID))
                        _poolCreatureGroups[poolID] = new PoolGroup<Creature>(this, _objectManager, _creatureFactory, _gameObjectFactory);

                    var cregroup = _poolCreatureGroups[poolID];
                    cregroup.SetPoolId(poolID);
                    cregroup.AddEntry(plObject, pPoolTemplate.MaxLimit);

                    _creatureSearchMap.Add(guid, poolID);
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
            var result = _worldDatabase.Query("SELECT spawnId, poolSpawnId, chance FROM pool_members WHERE type = 1");

            if (result.IsEmpty())
                Log.Logger.Information("Loaded 0 gameobjects in  pools. DB table `pool_gameobject` is empty.");
            else
            {
                uint count = 0;

                do
                {
                    var guid = result.Read<ulong>(0);
                    var poolID = result.Read<uint>(1);
                    var chance = result.Read<float>(2);

                    var data = _objectManager.SpawnDataCacheRouter.GameObjectCache.GetGameObjectData(guid);

                    if (data == null)
                    {
                        Log.Logger.Error("`pool_gameobject` has a non existing gameobject spawn (GUID: {0}) defined for pool id ({1}), skipped.", guid, poolID);

                        continue;
                    }

                    if (!_poolTemplate.ContainsKey(poolID))
                    {
                        Log.Logger.Error("`pool_gameobject` pool id ({0}) is not in `pool_template`, skipped.", poolID);

                        continue;
                    }

                    if (chance is < 0 or > 100)
                    {
                        Log.Logger.Error("`pool_gameobject` has an invalid chance ({0}) for gameobject guid ({1}) in pool id ({2}), skipped.", chance, guid, poolID);

                        continue;
                    }

                    var pPoolTemplate = _poolTemplate[poolID];

                    if (pPoolTemplate.MapId == -1)
                        pPoolTemplate.MapId = (int)data.MapId;

                    if (pPoolTemplate.MapId != data.MapId)
                    {
                        Log.Logger.Error($"`pool_gameobject` has gameobject spawns on multiple different maps for gameobject guid ({guid}) in pool id ({poolID}), skipped.");

                        continue;
                    }

                    PoolObject plObject = new(guid, chance);

                    if (!_poolGameobjectGroups.ContainsKey(poolID))
                        _poolGameobjectGroups[poolID] = new PoolGroup<GameObject>(this, _objectManager, _creatureFactory, _gameObjectFactory);

                    var gogroup = _poolGameobjectGroups[poolID];
                    gogroup.SetPoolId(poolID);
                    gogroup.AddEntry(plObject, pPoolTemplate.MaxLimit);

                    _gameobjectSearchMap.Add(guid, poolID);
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
            var result = _worldDatabase.Query("SELECT spawnId, poolSpawnId, chance FROM pool_members WHERE type = 2");

            if (result.IsEmpty())
                Log.Logger.Information("Loaded 0 pools in pools");
            else
            {
                uint count = 0;

                do
                {
                    var childPoolID = result.Read<uint>(0);
                    var motherPoolID = result.Read<uint>(1);
                    var chance = result.Read<float>(2);

                    if (!_poolTemplate.ContainsKey(motherPoolID))
                    {
                        Log.Logger.Error("`pool_pool` mother_pool id ({0}) is not in `pool_template`, skipped.", motherPoolID);

                        continue;
                    }

                    if (!_poolTemplate.ContainsKey(childPoolID))
                    {
                        Log.Logger.Error("`pool_pool` included pool_id ({0}) is not in `pool_template`, skipped.", childPoolID);

                        continue;
                    }

                    if (motherPoolID == childPoolID)
                    {
                        Log.Logger.Error("`pool_pool` pool_id ({0}) includes itself, dead-lock detected, skipped.", childPoolID);

                        continue;
                    }

                    if (chance is < 0 or > 100)
                    {
                        Log.Logger.Error("`pool_pool` has an invalid chance ({0}) for pool id ({1}) in mother pool id ({2}), skipped.", chance, childPoolID, motherPoolID);

                        continue;
                    }

                    var pPoolTemplateMother = _poolTemplate[motherPoolID];
                    PoolObject plObject = new(childPoolID, chance);

                    if (!_poolPoolGroups.ContainsKey(motherPoolID))
                        _poolPoolGroups[motherPoolID] = new PoolGroup<Pool>(this, _objectManager, _creatureFactory, _gameObjectFactory);

                    var plgroup = _poolPoolGroups[motherPoolID];
                    plgroup.SetPoolId(motherPoolID);
                    plgroup.AddEntry(plObject, pPoolTemplateMother.MaxLimit);

                    _poolSearchMap.Add(childPoolID, motherPoolID);
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

        foreach (var (poolId, _) in _poolTemplate)
            if (IsEmpty(poolId))
            {
                Log.Logger.Error($"Pool Id {poolId} is empty (has no creatures and no gameobects and either no child pools or child pools are all empty. The pool will not be spawned");
            }

        // The initialize method will spawn all pools not in an event and not in another pool, this is why there is 2 left joins with 2 null checks
        Log.Logger.Information("Starting objects pooling system...");

        {
            var oldMSTime = Time.MSTime;

            var result = _worldDatabase.Query("SELECT DISTINCT pool_template.entry, pool_members.spawnId, pool_members.poolSpawnId FROM pool_template" +
                                              " LEFT JOIN game_event_pool ON pool_template.entry=game_event_pool.pool_entry" +
                                              " LEFT JOIN pool_members ON pool_members.type = 2 AND pool_template.entry = pool_members.spawnId WHERE game_event_pool.pool_entry IS NULL");

            if (result.IsEmpty())
                Log.Logger.Information("Pool handling system initialized, 0 pools spawned.");
            else
            {
                uint count = 0;

                do
                {
                    var poolEntry = result.Read<uint>(0);
                    var poolPoolID = result.Read<uint>(1);

                    if (IsEmpty(poolEntry))
                        continue;

                    if (!CheckPool(poolEntry))
                    {
                        if (poolPoolID != 0)
                            // The pool is a child pool in pool_pool table. Ideally we should remove it from the pool handler to ensure it never gets spawned,
                            // however that could recursively invalidate entire chain of mother pools. It can be done in the future but for now we'll do nothing.
                            Log.Logger.Error("Pool Id {0} has no equal chance pooled entites defined and explicit chance sum is not 100. This broken pool is a child pool of Id {1} and cannot be safely removed.", poolEntry, result.Read<uint>(2));
                        else
                            Log.Logger.Error("Pool Id {0} has no equal chance pooled entites defined and explicit chance sum is not 100. The pool will not be spawned.", poolEntry);

                        continue;
                    }

                    // Don't spawn child pools, they are spawned recursively by their parent pools
                    if (poolPoolID == 0)
                    {
                        _autoSpawnPoolsPerMap.Add((uint)_poolTemplate[poolEntry].MapId, poolEntry);
                        count++;
                    }
                } while (result.NextRow());

                Log.Logger.Information("Pool handling system initialized, {0} pools will be spawned by default in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
        }
    }

    public void SpawnPool(SpawnedPoolData spawnedPoolData, uint poolID)
    {
        SpawnPool<Pool>(spawnedPoolData, poolID, 0);
        SpawnPool<GameObject>(spawnedPoolData, poolID, 0);
        SpawnPool<Creature>(spawnedPoolData, poolID, 0);
    }

    public void UpdatePool<T>(SpawnedPoolData spawnedPoolData, uint poolID, ulong dbGuidOrPoolID)
    {
        var motherpoolid = IsPartOfAPool<Pool>(poolID);

        if (motherpoolid != 0)
            SpawnPool<Pool>(spawnedPoolData, motherpoolid, poolID);
        else
            SpawnPool<T>(spawnedPoolData, poolID, dbGuidOrPoolID);
    }

    public void UpdatePool(SpawnedPoolData spawnedPoolData, uint poolID, SpawnObjectType type, ulong spawnId)
    {
        switch (type)
        {
            case SpawnObjectType.Creature:
                UpdatePool<Creature>(spawnedPoolData, poolID, spawnId);

                break;
            case SpawnObjectType.GameObject:
                UpdatePool<GameObject>(spawnedPoolData, poolID, spawnId);

                break;
        }
    }

    private void SpawnPool<T>(SpawnedPoolData spawnedPoolData, uint poolID, ulong dbGuid)
    {
        switch (typeof(T).Name)
        {
            case "Creature":
                if (_poolCreatureGroups.ContainsKey(poolID) && !_poolCreatureGroups[poolID].IsEmpty())
                    _poolCreatureGroups[poolID].SpawnObject(spawnedPoolData, _poolTemplate[poolID].MaxLimit, dbGuid);

                break;
            case "GameObject":
                if (_poolGameobjectGroups.ContainsKey(poolID) && !_poolGameobjectGroups[poolID].IsEmpty())
                    _poolGameobjectGroups[poolID].SpawnObject(spawnedPoolData, _poolTemplate[poolID].MaxLimit, dbGuid);

                break;
            case "Pool":
                if (_poolPoolGroups.ContainsKey(poolID) && !_poolPoolGroups[poolID].IsEmpty())
                    _poolPoolGroups[poolID].SpawnObject(spawnedPoolData, _poolTemplate[poolID].MaxLimit, dbGuid);

                break;
        }
    }
}

// for Pool of Pool case