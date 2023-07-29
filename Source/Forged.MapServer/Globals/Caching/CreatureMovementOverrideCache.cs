// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class CreatureMovementOverrideCache : IObjectCache
{
    private readonly WorldDatabase _worldDatabase;
    private readonly IConfiguration _configuration;
    private readonly CreatureDataCache _creatureDataCache;
    private readonly Dictionary<ulong, CreatureMovementData> _creatureMovementOverrides = new();

    public CreatureMovementOverrideCache(WorldDatabase worldDatabase, IConfiguration configuration, CreatureDataCache creatureDataCache)
    {
        _worldDatabase = worldDatabase;
        _configuration = configuration;
        _creatureDataCache = creatureDataCache;
    }

    public void Load()
    {
        var oldMSTime = Time.MSTime;

        _creatureMovementOverrides.Clear();

        // Load the data from creature_movement_override and if NULL fallback to creature_template_movement
        var result = _worldDatabase.Query("SELECT cmo.SpawnId,COALESCE(cmo.Ground, ctm.Ground),COALESCE(cmo.Swim, ctm.Swim),COALESCE(cmo.Flight, ctm.Flight),COALESCE(cmo.Rooted, ctm.Rooted),COALESCE(cmo.Chase, ctm.Chase),COALESCE(cmo.Random, ctm.Random)," +
                                          "COALESCE(cmo.InteractionPauseTimer, ctm.InteractionPauseTimer) FROM creature_movement_override AS cmo LEFT JOIN creature AS c ON c.guid = cmo.SpawnId LEFT JOIN creature_template_movement AS ctm ON ctm.CreatureId = c.id");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 creature movement overrides. DB table `creature_movement_override` is empty!");

            return;
        }

        do
        {
            var spawnId = result.Read<ulong>(0);

            if (_creatureDataCache.GetCreatureData(spawnId) == null)
            {
                if (_configuration.GetDefaultValue("load:autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM creature_movement_override WHERE SpawnId = {spawnId}");
                else
                    Log.Logger.Error($"Creature (GUID: {spawnId}) does not exist but has a record in `creature_movement_override`");

                continue;
            }

            CreatureMovementData movement = new(_configuration);

            if (!result.IsNull(1))
                movement.Ground = (CreatureGroundMovementType)result.Read<byte>(1);

            if (!result.IsNull(2))
                movement.Swim = result.Read<bool>(2);

            if (!result.IsNull(3))
                movement.Flight = (CreatureFlightMovementType)result.Read<byte>(3);

            if (!result.IsNull(4))
                movement.Rooted = result.Read<bool>(4);

            if (!result.IsNull(5))
                movement.Chase = (CreatureChaseMovementType)result.Read<byte>(5);

            if (!result.IsNull(6))
                movement.Random = (CreatureRandomMovementType)result.Read<byte>(6);

            if (!result.IsNull(7))
                movement.InteractionPauseTimer = result.Read<uint>(7);

            CheckCreatureMovement("creature_movement_override", spawnId, movement);

            _creatureMovementOverrides[spawnId] = movement;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {_creatureMovementOverrides.Count} movement overrides in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    public CreatureMovementData GetCreatureMovementOverride(ulong spawnId)
    {
        return _creatureMovementOverrides.LookupByKey(spawnId);
    }

    public bool TryGetGetCreatureMovementOverride(ulong spawnId, out CreatureMovementData movementData) => _creatureMovementOverrides.TryGetValue(spawnId, out movementData);

    private void CheckCreatureMovement(string table, ulong id, CreatureMovementData creatureMovement)
    {
        if (creatureMovement.Ground >= CreatureGroundMovementType.Max)
        {
            Log.Logger.Error($"`{table}`.`Ground` wrong value ({creatureMovement.Ground}) for Id {id}, setting to Run.");
            creatureMovement.Ground = CreatureGroundMovementType.Run;
        }

        if (creatureMovement.Flight >= CreatureFlightMovementType.Max)
        {
            Log.Logger.Error($"`{table}`.`Flight` wrong value ({creatureMovement.Flight}) for Id {id}, setting to None.");
            creatureMovement.Flight = CreatureFlightMovementType.None;
        }

        if (creatureMovement.Chase >= CreatureChaseMovementType.Max)
        {
            Log.Logger.Error($"`{table}`.`Chase` wrong value ({creatureMovement.Chase}) for Id {id}, setting to Run.");
            creatureMovement.Chase = CreatureChaseMovementType.Run;
        }

        if (creatureMovement.Random >= CreatureRandomMovementType.Max)
        {
            Log.Logger.Error($"`{table}`.`Random` wrong value ({creatureMovement.Random}) for Id {id}, setting to Walk.");
            creatureMovement.Random = CreatureRandomMovementType.Walk;
        }
    }
}