// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps;
using Framework.Constants;
using Framework.Database;
using Game.Common;

namespace Forged.MapServer.Entities.Creatures;

public class CreatureFactory
{
    private readonly CharacterDatabase _characterDatabase;
    private readonly ClassFactory _classFactory;
    private readonly MapManager _mapManager;
    private readonly GameObjectManager _objectManager;
    private readonly WorldDatabase _worldDatabase;

    public CreatureFactory(ClassFactory classFactory, GameObjectManager objectManager, MapManager mapManager, CharacterDatabase characterDatabase, WorldDatabase worldDatabase)
    {
        _classFactory = classFactory;
        _objectManager = objectManager;
        _mapManager = mapManager;
        _characterDatabase = characterDatabase;
        _worldDatabase = worldDatabase;
    }

    public Creature CreateCreature(uint entry, Map map, Position pos, uint vehId = 0)
    {
        var cInfo = _objectManager.GetCreatureTemplate(entry);

        if (cInfo == null)
            return null;

        ulong lowGuid;

        if (vehId != 0 || cInfo.VehicleId != 0)
            lowGuid = map.GenerateLowGuid(HighGuid.Vehicle);
        else
            lowGuid = map.GenerateLowGuid(HighGuid.Creature);

        Creature creature = new(_classFactory);

        return !creature.Create(lowGuid, map, entry, pos, null, vehId) ? null : creature;
    }

    public Creature CreateCreatureFromDB(ulong spawnId, Map map, bool addToMap = true, bool allowDuplicate = false)
    {
        Creature creature = new(_classFactory);

        return !creature.LoadFromDB(spawnId, map, addToMap, allowDuplicate) ? null : creature;
    }

    public bool DeleteFromDB(ulong spawnId)
    {
        var data = _objectManager.GetCreatureData(spawnId);

        if (data == null)
            return false;

        SQLTransaction trans = new();

        _mapManager.DoForAllMapsWithMapId(data.MapId,
                                          map =>
                                          {
                                              // despawn all active creatures, and remove their respawns
                                              List<Creature> toUnload = new();

                                              foreach (var creature in map.CreatureBySpawnIdStore.LookupByKey(spawnId))
                                                  toUnload.Add(creature);

                                              foreach (var creature in toUnload)
                                                  map.AddObjectToRemoveList(creature);

                                              map.RemoveRespawnTime(SpawnObjectType.Creature, spawnId, trans);
                                          });

        // delete data from memory ...
        _objectManager.DeleteCreatureData(spawnId);

        _characterDatabase.CommitTransaction(trans);

        // ... and the database
        trans = new SQLTransaction();

        var stmt = _worldDatabase.GetPreparedStatement(WorldStatements.DEL_CREATURE);
        stmt.AddValue(0, spawnId);
        trans.Append(stmt);

        stmt = _worldDatabase.GetPreparedStatement(WorldStatements.DEL_SPAWNGROUP_MEMBER);
        stmt.AddValue(0, (byte)SpawnObjectType.Creature);
        stmt.AddValue(1, spawnId);
        trans.Append(stmt);

        stmt = _worldDatabase.GetPreparedStatement(WorldStatements.DEL_CREATURE_ADDON);
        stmt.AddValue(0, spawnId);
        trans.Append(stmt);

        stmt = _worldDatabase.GetPreparedStatement(WorldStatements.DEL_GAME_EVENT_CREATURE);
        stmt.AddValue(0, spawnId);
        trans.Append(stmt);

        stmt = _worldDatabase.GetPreparedStatement(WorldStatements.DEL_GAME_EVENT_MODEL_EQUIP);
        stmt.AddValue(0, spawnId);
        trans.Append(stmt);

        stmt = _worldDatabase.GetPreparedStatement(WorldStatements.DEL_LINKED_RESPAWN);
        stmt.AddValue(0, spawnId);
        stmt.AddValue(1, (uint)CreatureLinkedRespawnType.CreatureToCreature);
        trans.Append(stmt);

        stmt = _worldDatabase.GetPreparedStatement(WorldStatements.DEL_LINKED_RESPAWN);
        stmt.AddValue(0, spawnId);
        stmt.AddValue(1, (uint)CreatureLinkedRespawnType.CreatureToGO);
        trans.Append(stmt);

        stmt = _worldDatabase.GetPreparedStatement(WorldStatements.DEL_LINKED_RESPAWN_MASTER);
        stmt.AddValue(0, spawnId);
        stmt.AddValue(1, (uint)CreatureLinkedRespawnType.CreatureToCreature);
        trans.Append(stmt);

        stmt = _worldDatabase.GetPreparedStatement(WorldStatements.DEL_LINKED_RESPAWN_MASTER);
        stmt.AddValue(0, spawnId);
        stmt.AddValue(1, (uint)CreatureLinkedRespawnType.GOToCreature);
        trans.Append(stmt);

        _worldDatabase.CommitTransaction(trans);

        return true;
    }
}