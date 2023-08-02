// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.M;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class TerrainSwapCache : IObjectCache
{
    private readonly DB2Manager _db2Manager;
    private readonly DB6Storage<MapRecord> _mapRecords;
    private readonly Dictionary<uint, TerrainSwapInfo> _terrainSwapInfoById = new();
    private readonly WorldDatabase _worldDatabase;

    public TerrainSwapCache(WorldDatabase worldDatabase, DB6Storage<MapRecord> mapRecords, DB2Manager db2Manager)
    {
        _worldDatabase = worldDatabase;
        _mapRecords = mapRecords;
        _db2Manager = db2Manager;
    }

    public MultiMap<uint, TerrainSwapInfo> TerrainSwaps { get; } = new();

    public TerrainSwapInfo GetTerrainSwapInfo(uint terrainSwapId)
    {
        return _terrainSwapInfoById.LookupByKey(terrainSwapId);
    }

    public void Load()
    {
        LoadTerrainSwapDefaults();
        LoadTerrainWorldMaps();
    }

    private void LoadTerrainSwapDefaults()
    {
        var oldMSTime = Time.MSTime;

        var result = _worldDatabase.Query("SELECT MapId, TerrainSwapMap FROM `terrain_swap_defaults`");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 terrain swap defaults. DB table `terrain_swap_defaults` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var mapId = result.Read<uint>(0);

            if (!_mapRecords.ContainsKey(mapId))
            {
                Log.Logger.Error("Map {0} defined in `terrain_swap_defaults` does not exist, skipped.", mapId);

                continue;
            }

            var terrainSwap = result.Read<uint>(1);

            if (!_mapRecords.ContainsKey(terrainSwap))
            {
                Log.Logger.Error("TerrainSwapMap {0} defined in `terrain_swap_defaults` does not exist, skipped.", terrainSwap);

                continue;
            }

            var terrainSwapInfo = _terrainSwapInfoById[terrainSwap];
            terrainSwapInfo.Id = terrainSwap;
            TerrainSwaps.Add(mapId, terrainSwapInfo);

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} terrain swap defaults in {1} ms.", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    private void LoadTerrainWorldMaps()
    {
        var oldMSTime = Time.MSTime;

        //                                         0               1
        var result = _worldDatabase.Query("SELECT TerrainSwapMap, UiMapPhaseId  FROM `terrain_worldmap`");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 terrain world maps. DB table `terrain_worldmap` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var mapId = result.Read<uint>(0);
            var uiMapPhaseId = result.Read<uint>(1);

            if (!_mapRecords.ContainsKey(mapId))
            {
                Log.Logger.Error("TerrainSwapMap {0} defined in `terrain_worldmap` does not exist, skipped.", mapId);

                continue;
            }

            if (!_db2Manager.IsUiMapPhase((int)uiMapPhaseId))
            {
                Log.Logger.Error($"Phase {uiMapPhaseId} defined in `terrain_worldmap` is not a valid terrain swap phase, skipped.");

                continue;
            }

            if (!_terrainSwapInfoById.ContainsKey(mapId))
                _terrainSwapInfoById.Add(mapId, new TerrainSwapInfo());

            var terrainSwapInfo = _terrainSwapInfoById[mapId];
            terrainSwapInfo.Id = mapId;
            terrainSwapInfo.UiMapPhaseIDs.Add(uiMapPhaseId);

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} terrain world maps in {1} ms.", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }
}