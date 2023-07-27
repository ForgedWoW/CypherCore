// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Globals;
using Framework.Constants;

namespace Forged.MapServer.Pools;

public class PoolGroup<T>
{
    private readonly CreatureFactory _creatureFactory;
    private readonly List<PoolObject> _equalChanced = new();
    private readonly List<PoolObject> _explicitlyChanced = new();
    private readonly GameObjectFactory _gameObjectFactory;
    private readonly GameObjectManager _objectManager;
    private readonly PoolManager _poolManager;
    private uint _poolId;

    public PoolGroup(PoolManager poolManager, GameObjectManager objectManager, CreatureFactory creatureFactory, GameObjectFactory gameObjectFactory)
    {
        _poolManager = poolManager;
        _objectManager = objectManager;
        _creatureFactory = creatureFactory;
        _gameObjectFactory = gameObjectFactory;
        _poolId = 0;
    }

    public void AddEntry(PoolObject poolitem, uint maxentries)
    {
        if (poolitem.Chance != 0 && maxentries == 1)
            _explicitlyChanced.Add(poolitem);
        else
            _equalChanced.Add(poolitem);
    }

    public bool CheckPool()
    {
        if (!_equalChanced.Empty())
            return true;

        return _explicitlyChanced.Sum(t => t.Chance) is 100 or 0;
    }

    public void DespawnObject(SpawnedPoolData spawns, ulong guid = 0, bool alwaysDeleteRespawnTime = false)
    {
        foreach (var obj in _equalChanced)
            if (spawns.IsSpawnedObject<T>(obj.Guid))
            {
                if (guid != 0 && obj.Guid != guid)
                    continue;

                Despawn1Object(spawns, obj.Guid, alwaysDeleteRespawnTime);
                spawns.RemoveSpawn<T>(obj.Guid, _poolId);
            }
            else if (alwaysDeleteRespawnTime)
                RemoveRespawnTimeFromDB(spawns, obj.Guid);

        foreach (var obj in _explicitlyChanced)
            if (spawns.IsSpawnedObject<T>(obj.Guid))
            {
                if (guid != 0 && obj.Guid != guid)
                    continue;

                Despawn1Object(spawns, obj.Guid, alwaysDeleteRespawnTime);
                spawns.RemoveSpawn<T>(obj.Guid, _poolId);
            }
            else if (alwaysDeleteRespawnTime)
                RemoveRespawnTimeFromDB(spawns, obj.Guid);
    }

    public uint GetPoolId()
    {
        return _poolId;
    }

    public bool IsEmpty()
    {
        return _explicitlyChanced.Empty() && _equalChanced.Empty();
    }

    public bool IsEmptyDeepCheck()
    {
        if (typeof(T).Name != "Pool")
            return IsEmpty();

        return _explicitlyChanced.All(explicitlyChanced => _poolManager.IsEmpty((uint)explicitlyChanced.Guid)) &&
               _equalChanced.All(equalChanced => _poolManager.IsEmpty((uint)equalChanced.Guid));
    }

    public void RemoveOneRelation(uint childPoolID)
    {
        if (typeof(T).Name != "Pool")
            return;

        foreach (var poolObject in _explicitlyChanced)
            if (poolObject.Guid == childPoolID)
            {
                _explicitlyChanced.Remove(poolObject);

                break;
            }

        foreach (var poolObject in _equalChanced)
            if (poolObject.Guid == childPoolID)
            {
                _equalChanced.Remove(poolObject);

                break;
            }
    }

    public void SetPoolId(uint poolID)
    {
        _poolId = poolID;
    }

    public void SpawnObject(SpawnedPoolData spawns, uint limit, ulong triggerFrom)
    {
        var count = (int)(limit - spawns.GetSpawnedObjects(_poolId));

        // If triggered from some object respawn this object is still marked as spawned
        // and also counted into m_SpawnedPoolAmount so we need increase count to be
        // spawned by 1
        if (triggerFrom != 0)
            ++count;

        // This will try to spawn the rest of pool, not guaranteed
        if (count > 0)
        {
            List<PoolObject> rolledObjects = new();

            // roll objects to be spawned
            if (!_explicitlyChanced.Empty())
            {
                var roll = (float)RandomHelper.randChance();

                foreach (var obj in _explicitlyChanced)
                {
                    roll -= obj.Chance;

                    // Triggering object is marked as spawned at this time and can be also rolled (respawn case)
                    // so this need explicit check for this case
                    if (roll < 0 && (obj.Guid == triggerFrom || !spawns.IsSpawnedObject<T>(obj.Guid)))
                    {
                        rolledObjects.Add(obj);

                        break;
                    }
                }
            }

            if (!_equalChanced.Empty() && rolledObjects.Empty())
            {
                rolledObjects.AddRange(_equalChanced.Where(obj => obj.Guid == triggerFrom || !spawns.IsSpawnedObject<T>(obj.Guid)));
                rolledObjects.RandomResize((uint)count);
            }

            // try to spawn rolled objects
            foreach (var obj in rolledObjects)
                if (obj.Guid == triggerFrom)
                {
                    ReSpawn1Object(spawns, obj);
                    triggerFrom = 0;
                }
                else
                {
                    spawns.AddSpawn<T>(obj.Guid, _poolId);
                    Spawn1Object(spawns, obj);
                }
        }

        // One spawn one despawn no count increase
        if (triggerFrom != 0)
            DespawnObject(spawns, triggerFrom);
    }

    private void Despawn1Object(SpawnedPoolData spawns, ulong guid, bool alwaysDeleteRespawnTime = false, bool saveRespawnTime = true)
    {
        switch (typeof(T).Name)
        {
            case "Creature":
            {
                var creatureBounds = spawns.Map.CreatureBySpawnIdStore.LookupByKey(guid);

                for (var i = creatureBounds.Count - 1; i > 0; i--) // this gets modified.
                    if (creatureBounds.Count > i)
                    {
                        var creature = creatureBounds[i];

                        // For dynamic spawns, save respawn time here
                        if (saveRespawnTime && !creature.RespawnCompatibilityMode)
                            creature.SaveRespawnTime();

                        creature.Location.AddObjectToRemoveList();
                    }

                if (alwaysDeleteRespawnTime)
                    spawns.Map.RemoveRespawnTime(SpawnObjectType.Creature, guid, null, true);

                break;
            }
            case "GameObject":
            {
                var gameobjectBounds = spawns.Map.GameObjectBySpawnIdStore.LookupByKey(guid);

                foreach (var go in gameobjectBounds)
                {
                    // For dynamic spawns, save respawn time here
                    if (saveRespawnTime && !go.RespawnCompatibilityMode)
                        go.SaveRespawnTime();

                    go.Location.AddObjectToRemoveList();
                }

                if (alwaysDeleteRespawnTime)
                    spawns.Map.RemoveRespawnTime(SpawnObjectType.GameObject, guid, null, true);

                break;
            }
            case "Pool":
                _poolManager.DespawnPool(spawns, (uint)guid, alwaysDeleteRespawnTime);

                break;
        }
    }

    private void RemoveRespawnTimeFromDB(SpawnedPoolData spawns, ulong guid)
    {
        switch (typeof(T).Name)
        {
            case "Creature":
                spawns.Map.RemoveRespawnTime(SpawnObjectType.Creature, guid, null, true);

                break;
            case "GameObject":
                spawns.Map.RemoveRespawnTime(SpawnObjectType.GameObject, guid, null, true);

                break;
        }
    }

    private void ReSpawn1Object(SpawnedPoolData spawns, PoolObject obj)
    {
        switch (typeof(T).Name)
        {
            case "Creature":
            case "GameObject":
                Despawn1Object(spawns, obj.Guid, false, false);
                Spawn1Object(spawns, obj);

                break;
        }
    }

    private void Spawn1Object(SpawnedPoolData spawns, PoolObject obj)
    {
        switch (typeof(T).Name)
        {
            case "Creature":
            {
                var data = _objectManager.GetCreatureData(obj.Guid);

                if (data != null)
                    // Spawn if necessary (loaded grids only)
                    // We use spawn coords to spawn
                    if (spawns.Map.IsGridLoaded(data.SpawnPoint))
                        _creatureFactory.CreateCreatureFromDB(obj.Guid, spawns.Map);
            }

                break;
            case "GameObject":
            {
                var data = _objectManager.GameObjectCache.GetGameObjectData(obj.Guid);

                if (data != null)
                    // Spawn if necessary (loaded grids only)
                    // We use current coords to unspawn, not spawn coords since creature can have changed grid
                    if (spawns.Map.IsGridLoaded(data.SpawnPoint))
                    {
                        var go = _gameObjectFactory.CreateGameObjectFromDb(obj.Guid, spawns.Map, false);

                        if (go is { IsSpawnedByDefault: true })
                            if (!spawns.Map.AddToMap(go))
                                go.Dispose();
                    }
            }

                break;
            case "Pool":
                _poolManager.SpawnPool(spawns, (uint)obj.Guid);

                break;
        }
    }
}