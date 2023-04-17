// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Maps;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Pools;

public class SpawnedPoolData
{
    private readonly List<ulong> _spawnedCreatures = new();
    private readonly List<ulong> _spawnedGameobjects = new();
    private readonly Dictionary<ulong, uint> _spawnedPools = new();

    public SpawnedPoolData(Map owner)
    {
        Map = owner;
    }

    public Map Map { get; }

    public void AddSpawn<T>(ulong dbGuid, uint poolId)
    {
        switch (typeof(T).Name)
        {
            case "Creature":
                _spawnedCreatures.Add(dbGuid);

                break;
            case "GameObject":
                _spawnedGameobjects.Add(dbGuid);

                break;
            case "Pool":
                _spawnedPools[dbGuid] = 0;

                break;
            default:
                return;
        }

        if (!_spawnedPools.ContainsKey(poolId))
            _spawnedPools[poolId] = 0;

        ++_spawnedPools[poolId];
    }

    public uint GetSpawnedObjects(uint poolId)
    {
        return _spawnedPools.LookupByKey(poolId);
    }

    public bool IsSpawnedObject<T>(ulong dbGuid)
    {
        return typeof(T).Name switch
        {
            "Creature"   => _spawnedCreatures.Contains(dbGuid),
            "GameObject" => _spawnedGameobjects.Contains(dbGuid),
            "Pool"       => _spawnedPools.ContainsKey(dbGuid),
            _            => false
        };
    }

    public bool IsSpawnedObject(SpawnObjectType type, ulong dbGuidOrPoolId)
    {
        switch (type)
        {
            case SpawnObjectType.Creature:
                return _spawnedCreatures.Contains(dbGuidOrPoolId);
            case SpawnObjectType.GameObject:
                return _spawnedGameobjects.Contains(dbGuidOrPoolId);
            default:
                Log.Logger.Fatal($"Invalid spawn type {type} passed to SpawnedPoolData::IsSpawnedObject (with spawnId {dbGuidOrPoolId})");

                return false;
        }
    }

    public void RemoveSpawn<T>(ulong dbGuid, uint poolId)
    {
        switch (typeof(T).Name)
        {
            case "Creature":
                _spawnedCreatures.Remove(dbGuid);

                break;
            case "GameObject":
                _spawnedGameobjects.Remove(dbGuid);

                break;
            case "Pool":
                _spawnedPools.Remove(dbGuid);

                break;
            default:
                return;
        }

        if (_spawnedPools[poolId] > 0)
            --_spawnedPools[poolId];
    }
}