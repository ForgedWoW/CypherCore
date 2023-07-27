// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps;
using Framework.Constants;
using Framework.Database;
using Game.Common;

namespace Forged.MapServer.Entities.GameObjects;

public class GameObjectFactory
{
    private readonly ClassFactory _classFactory;
    private readonly MapManager _mapManager;
    private readonly GameObjectManager _objectManager;
    private readonly WorldDatabase _worldDatabase;

    public GameObjectFactory(GameObjectManager objectManager, MapManager mapManager, WorldDatabase worldDatabase, ClassFactory classFactory)
    {
        _objectManager = objectManager;
        _mapManager = mapManager;
        _worldDatabase = worldDatabase;
        _classFactory = classFactory;
    }

    public GameObject CreateGameObject(uint entry, Map map, Position pos, Quaternion rotation, uint animProgress, GameObjectState goState, uint artKit = 0)
    {
        var goInfo = _objectManager.GameObjectTemplateCache.GetGameObjectTemplate(entry);

        if (goInfo == null)
            return null;

        var go = _classFactory.Resolve<GameObject>();

        return !go.Create(entry, map, pos, rotation, animProgress, goState, artKit, false, 0) ? null : go;
    }

    public GameObject CreateGameObjectFromDb(ulong spawnId, Map map, bool addToMap = true)
    {
        var go = _classFactory.Resolve<GameObject>();

        return !go.LoadFromDB(spawnId, map, addToMap) ? null : go;
    }

    public bool DeleteFromDB(ulong spawnId)
    {
        var data = _objectManager.GameObjectCache.GetGameObjectData(spawnId);

        if (data == null)
            return false;

        SQLTransaction trans = new();

        _mapManager.DoForAllMapsWithMapId(data.MapId,
                                          map =>
                                          {
                                              // despawn all active objects, and remove their respawns
                                              foreach (var obj in map.GameObjectBySpawnIdStore.LookupByKey(spawnId))
                                                  map.AddObjectToRemoveList(obj);

                                              map.RemoveRespawnTime(SpawnObjectType.GameObject, spawnId, trans);
                                          });

        // delete data from memory
        _objectManager.GameObjectCache.DeleteGameObjectData(spawnId);

        trans = new SQLTransaction();

        // ... and the database
        var stmt = _worldDatabase.GetPreparedStatement(WorldStatements.DEL_GAMEOBJECT);
        stmt.AddValue(0, spawnId);
        trans.Append(stmt);

        stmt = _worldDatabase.GetPreparedStatement(WorldStatements.DEL_EVENT_GAMEOBJECT);
        stmt.AddValue(0, spawnId);
        trans.Append(stmt);

        stmt = _worldDatabase.GetPreparedStatement(WorldStatements.DEL_LINKED_RESPAWN);
        stmt.AddValue(0, spawnId);
        stmt.AddValue(1, (uint)CreatureLinkedRespawnType.GOToGO);
        trans.Append(stmt);

        stmt = _worldDatabase.GetPreparedStatement(WorldStatements.DEL_LINKED_RESPAWN);
        stmt.AddValue(0, spawnId);
        stmt.AddValue(1, (uint)CreatureLinkedRespawnType.GOToCreature);
        trans.Append(stmt);

        stmt = _worldDatabase.GetPreparedStatement(WorldStatements.DEL_LINKED_RESPAWN_MASTER);
        stmt.AddValue(0, spawnId);
        stmt.AddValue(1, (uint)CreatureLinkedRespawnType.GOToGO);
        trans.Append(stmt);

        stmt = _worldDatabase.GetPreparedStatement(WorldStatements.DEL_LINKED_RESPAWN_MASTER);
        stmt.AddValue(0, spawnId);
        stmt.AddValue(1, (uint)CreatureLinkedRespawnType.CreatureToGO);
        trans.Append(stmt);

        stmt = _worldDatabase.GetPreparedStatement(WorldStatements.DEL_GAMEOBJECT_ADDON);
        stmt.AddValue(0, spawnId);
        trans.Append(stmt);

        _worldDatabase.CommitTransaction(trans);

        return true;
    }
}