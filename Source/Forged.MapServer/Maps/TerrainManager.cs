// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Phasing;
using Framework.Constants;
using Framework.Database;
using Framework.Threading;
using Framework.Util;
using Microsoft.Extensions.Configuration;

namespace Forged.MapServer.Maps;

public class TerrainManager
{
    private readonly CliDB _cliDB;
    private readonly HashSet<uint> _keepLoaded = new();
    private readonly Dictionary<uint, TerrainInfo> _terrainMaps = new();
    private readonly LimitedThreadTaskManager _threadTaskManager;
    private readonly WorldDatabase _worldDatabase;
    // parent map links
    private MultiMap<uint, uint> _parentMapData = new();

    public TerrainManager(IConfiguration configuration, WorldDatabase worldDatabase, CliDB cliDB)
    {
        _worldDatabase = worldDatabase;
        _cliDB = cliDB;
        _threadTaskManager = new LimitedThreadTaskManager(configuration.GetDefaultValue("Map.ParellelUpdateTasks", 20));
    }

    public static bool ExistMapAndVMap(uint mapid, float x, float y)
    {
        var p = GridDefines.ComputeGridCoord(x, y);

        var gx = (int)((MapConst.MaxGrids - 1) - p.X_Coord);
        var gy = (int)((MapConst.MaxGrids - 1) - p.Y_Coord);

        return TerrainInfo.ExistMap(mapid, gx, gy) && TerrainInfo.ExistVMap(mapid, gx, gy);
    }

    public uint GetAreaId(PhaseShift phaseShift, uint mapid, Position pos)
    {
        return GetAreaId(phaseShift, mapid, pos.X, pos.Y, pos.Z);
    }

    public uint GetAreaId(PhaseShift phaseShift, WorldLocation loc)
    {
        return GetAreaId(phaseShift, loc.MapId, loc);
    }

    public uint GetAreaId(PhaseShift phaseShift, uint mapid, float x, float y, float z)
    {
        var terrain = LoadTerrain(mapid);

        if (terrain != null)
            return terrain.GetAreaId(phaseShift, mapid, x, y, z);

        return 0;
    }

    public void GetZoneAndAreaId(PhaseShift phaseShift, out uint zoneid, out uint areaid, uint mapid, Position pos)
    {
        GetZoneAndAreaId(phaseShift, out zoneid, out areaid, mapid, pos.X, pos.Y, pos.Z);
    }

    public void GetZoneAndAreaId(PhaseShift phaseShift, out uint zoneid, out uint areaid, WorldLocation loc)
    {
        GetZoneAndAreaId(phaseShift, out zoneid, out areaid, loc.MapId, loc);
    }

    public void GetZoneAndAreaId(PhaseShift phaseShift, out uint zoneid, out uint areaid, uint mapid, float x, float y, float z)
    {
        zoneid = areaid = 0;

        var terrain = LoadTerrain(mapid);

        if (terrain != null)
            terrain.GetZoneAndAreaId(phaseShift, mapid, out zoneid, out areaid, x, y, z);
    }

    public uint GetZoneId(PhaseShift phaseShift, uint mapid, Position pos)
    {
        return GetZoneId(phaseShift, mapid, pos.X, pos.Y, pos.Z);
    }

    public uint GetZoneId(PhaseShift phaseShift, WorldLocation loc)
    {
        return GetZoneId(phaseShift, loc.MapId, loc);
    }

    public uint GetZoneId(PhaseShift phaseShift, uint mapId, float x, float y, float z)
    {
        var terrain = LoadTerrain(mapId);

        if (terrain != null)
            return terrain.GetZoneId(phaseShift, mapId, x, y, z);

        return 0;
    }

    public void InitializeParentMapData(MultiMap<uint, uint> mapData)
    {
        _parentMapData = mapData;

        var result = _worldDatabase.Query("SELECT mapid FROM map_keeploaded");

        if (!result.IsEmpty())
            do
            {
                _keepLoaded.Add(result.Read<uint>(0));
            } while (result.NextRow());
    }

    public bool KeepMapLoaded(uint mapid)
    {
        return _keepLoaded.Contains(mapid);
    }

    public TerrainInfo LoadTerrain(uint mapId)
    {
        if (!_cliDB.MapStorage.TryGetValue(mapId, out var entry))
            return null;

        while (entry.ParentMapID != -1 || entry.CosmeticParentMapID != -1)
        {
            var parentMapId = (uint)(entry.ParentMapID != -1 ? entry.ParentMapID : entry.CosmeticParentMapID);
            entry = _cliDB.MapStorage.LookupByKey(parentMapId);

            if (entry == null)
                break;

            mapId = parentMapId;
        }

        if (_terrainMaps.TryGetValue(mapId, out var terrain))
            return terrain;

        var terrainInfo = LoadTerrainImpl(mapId);
        _terrainMaps[mapId] = terrainInfo;

        return terrainInfo;
    }

    public void UnloadAll()
    {
        _terrainMaps.Clear();
    }

    public void Update(uint diff)
    {
        // global garbage collection
        foreach (var (_, terrain) in _terrainMaps)
            _threadTaskManager.Schedule(() => terrain?.CleanUpGrids(diff));

        _threadTaskManager.Wait();
    }
    private TerrainInfo LoadTerrainImpl(uint mapId)
    {
        var rootTerrain = new TerrainInfo(mapId, _keepLoaded.Contains(mapId));

        rootTerrain.DiscoverGridMapFiles();

        foreach (var childMapId in _parentMapData[mapId])
            rootTerrain.AddChildTerrain(LoadTerrainImpl(childMapId));

        return rootTerrain;
    }
}