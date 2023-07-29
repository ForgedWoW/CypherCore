using Forged.MapServer.DataStorage;
using Forged.MapServer.Maps;
using Framework.Constants;

namespace Forged.MapServer.Globals.Caching;

public class SpawnDataCacheRouter
{
    private readonly AreaTriggerDataStorage _areaTriggerDataStorage;
    private readonly GameObjectCache _gameObjectCache;
    private readonly CreatureDataCache _creatureDataCache;

    public SpawnDataCacheRouter(AreaTriggerDataStorage areaTriggerDataStorage, GameObjectCache gameObjectCache, CreatureDataCache creatureDataCache)
    {
        _areaTriggerDataStorage = areaTriggerDataStorage;
        _gameObjectCache = gameObjectCache;
        _creatureDataCache = creatureDataCache;
    }

    public SpawnData GetSpawnData(SpawnObjectType type, ulong spawnId)
    {
        if (!SpawnMetadata.TypeHasData(type))
            return null;

        return type switch
        {
            SpawnObjectType.Creature => _creatureDataCache.GetCreatureData(spawnId),
            SpawnObjectType.GameObject => _gameObjectCache.GetGameObjectData(spawnId),
            SpawnObjectType.AreaTrigger => _areaTriggerDataStorage.GetAreaTriggerSpawn(spawnId),
            _ => null
        };
    }

    public SpawnGroupTemplateData GetSpawnGroupData(SpawnObjectType type, ulong spawnId)
    {
        var data = GetSpawnMetadata(type, spawnId);

        return data?.SpawnGroupData;
    }

    public SpawnMetadata GetSpawnMetadata(SpawnObjectType type, ulong spawnId)
    {
        return SpawnMetadata.TypeHasData(type) ? GetSpawnData(type, spawnId) : null;
    }
}