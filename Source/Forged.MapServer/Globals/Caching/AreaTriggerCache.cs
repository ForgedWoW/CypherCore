// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.DataStorage.Structs.M;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class AreaTriggerCache : IObjectCache
{
    private readonly WorldDatabase _worldDatabase;
    private readonly WorldSafeLocationsCache _worldSafeLocationsCache;
    private readonly InstanceTemplateCache _instanceTemplateCache;
    private readonly DB6Storage<MapRecord> _mapRecords;
    private readonly DB6Storage<AreaTriggerRecord> _areaTriggerRecords;
    private readonly Dictionary<uint, AreaTriggerStruct> _areaTriggerStorage = new();

    public AreaTriggerCache(WorldDatabase worldDatabase, WorldSafeLocationsCache worldSafeLocationsCache, InstanceTemplateCache instanceTemplateCache,
                              DB6Storage<MapRecord> mapRecords, DB6Storage<AreaTriggerRecord> areaTriggerRecords)
    {
        _worldDatabase = worldDatabase;
        _worldSafeLocationsCache = worldSafeLocationsCache;
        _instanceTemplateCache = instanceTemplateCache;
        _mapRecords = mapRecords;
        _areaTriggerRecords = areaTriggerRecords;
    }

    public AreaTriggerStruct GetAreaTrigger(uint trigger)
    {
        return _areaTriggerStorage.LookupByKey(trigger);
    }

    public AreaTriggerStruct GetGoBackTrigger(uint map)
    {
        uint? parentId = null;
        var mapEntry = _mapRecords.LookupByKey(map);

        if (mapEntry == null || mapEntry.CorpseMapID < 0)
            return null;

        if (mapEntry.IsDungeon())
        {
            var iTemplate = _instanceTemplateCache.GetInstanceTemplate(map);

            if (iTemplate != null)
                parentId = iTemplate.Parent;
        }

        var entranceMap = parentId.GetValueOrDefault((uint)mapEntry.CorpseMapID);

        foreach (var pair in _areaTriggerStorage)
            if (pair.Value.TargetMapId == entranceMap)
            {
                var atEntry = _areaTriggerRecords.LookupByKey(pair.Key);

                if (atEntry != null && atEntry.ContinentID == map)
                    return pair.Value;
            }

        return null;
    }

    public AreaTriggerStruct GetMapEntranceTrigger(uint map)
    {
        foreach (var pair in _areaTriggerStorage)
            if (pair.Value.TargetMapId == map)
                if (_areaTriggerRecords.TryGetValue(pair.Key, out _))
                    return pair.Value;

        return null;
    }

    public void Load()
    {
        var oldMSTime = Time.MSTime;

        _areaTriggerStorage.Clear(); // need for reload case

        //                                         0   1
        var result = _worldDatabase.Query("SELECT ID, PortLocID FROM areatrigger_teleport");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 area trigger teleport definitions. DB table `areatrigger_teleport` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            ++count;

            var triggerID = result.Read<uint>(0);
            var portLocID = result.Read<uint>(1);

            var portLoc = _worldSafeLocationsCache.GetWorldSafeLoc(portLocID);

            if (portLoc == null)
            {
                Log.Logger.Error("Area Trigger (ID: {0}) has a non-existing Port Loc (ID: {1}) in WorldSafeLocs.dbc, skipped", triggerID, portLocID);

                continue;
            }

            AreaTriggerStruct at = new()
            {
                TargetMapId = portLoc.Location.MapId,
                TargetX = portLoc.Location.X,
                TargetY = portLoc.Location.Y,
                TargetZ = portLoc.Location.Z,
                TargetOrientation = portLoc.Location.Orientation,
                PortLocId = portLoc.Id
            };

            if (!_areaTriggerRecords.TryGetValue(triggerID, out _))
            {
                Log.Logger.Error("Area trigger (ID: {0}) does not exist in `AreaTrigger.dbc`.", triggerID);

                continue;
            }

            _areaTriggerStorage[triggerID] = at;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} area trigger teleport definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }
}