// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Forged.MapServer.Collision;
using Forged.MapServer.Collision.Management;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Phasing;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Maps;

public class TerrainInfo
{
    private static readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(1);
    private readonly List<TerrainInfo> _childTerrain = new();
    // global garbage collection timer
    private readonly TimeTracker _cleanupTimer;

    private readonly BitSet _gridFileExists = new(MapConst.MaxGrids * MapConst.MaxGrids);
    private readonly GridMap[][] _gridMap = new GridMap[MapConst.MaxGrids][];
    private readonly bool _keepLoaded = false;
    private readonly BitSet _loadedGrids = new(MapConst.MaxGrids * MapConst.MaxGrids);
    private readonly object _loadLock = new();
    private readonly uint _mapId;
    private readonly ushort[][] _referenceCountFromMap = new ushort[MapConst.MaxGrids][];
     // cache what grids are available for this map (not including parent/child maps)
    private TerrainInfo _parentTerrain;

    public TerrainInfo(uint mapId, bool keeLoaded)
    {
        _mapId = mapId;
        _keepLoaded = keeLoaded;
        _cleanupTimer = new TimeTracker(RandomHelper.RandTime(_cleanupInterval / 2, _cleanupInterval));

        for (var i = 0; i < MapConst.MaxGrids; ++i)
        {
            _gridMap[i] = new GridMap[MapConst.MaxGrids];
            _referenceCountFromMap[i] = new ushort[MapConst.MaxGrids];
        }
    }

    public static bool ExistMap(uint mapid, int gx, int gy, bool log = true)
    {
        var fileName = $"{Global.WorldMgr.DataPath}/maps/{mapid:D4}_{gx:D2}_{gy:D2}.map";

        var ret = false;

        if (!File.Exists(fileName))
        {
            if (log)
            {
                Log.Logger.Error($"Map file '{fileName}' does not exist!");
                Log.Logger.Error($"Please place MAP-files (*.map) in the appropriate directory ({Global.WorldMgr.DataPath + "/maps/"}), or correct the DataDir setting in your worldserver.conf file.");
            }
        }
        else
        {
            using var reader = new BinaryReader(new FileStream(fileName, FileMode.Open, FileAccess.Read));
            var header = reader.Read<MapFileHeader>();

            if (header.mapMagic != MapConst.MapMagic || (header.versionMagic != MapConst.MapVersionMagic && header.versionMagic != MapConst.MapVersionMagic2)) // Hack for some different extractors using v2.0 header
            {
                if (log)
                    Log.Logger.Error($"Map file '{fileName}' is from an incompatible map version ({header.versionMagic}), {MapConst.MapVersionMagic} is expected. Please pull your source, recompile tools and recreate maps using the updated mapextractor, then replace your old map files with new files. If you still have problems search on forum for error TCE00018.");
            }
            else
            {
                ret = true;
            }
        }

        return ret;
    }

    public static bool ExistVMap(uint mapid, int gx, int gy)
    {
        if (Global.VMapMgr.IsMapLoadingEnabled)
        {
            var result = Global.VMapMgr.ExistsMap(mapid, gx, gy);
            var name = VMapManager.GetMapFileName(mapid); //, gx, gy);

            switch (result)
            {
                case LoadResult.Success:
                    break;
                case LoadResult.FileNotFound:
                    Log.Logger.Error($"VMap file '{Global.WorldMgr.DataPath + "/vmaps/" + name}' does not exist");
                    Log.Logger.Error($"Please place VMAP files (*.vmtree and *.vmtile) in the vmap directory ({Global.WorldMgr.DataPath + "/vmaps/"}), or correct the DataDir setting in your worldserver.conf file.");

                    return false;
                case LoadResult.VersionMismatch:
                    Log.Logger.Error($"VMap file '{Global.WorldMgr.DataPath + "/vmaps/" + name}' couldn't be loaded");
                    Log.Logger.Error("This is because the version of the VMap file and the version of this module are different, please re-extract the maps with the tools compiled with this module.");

                    return false;
                case LoadResult.ReadFromFileFailed:
                    Log.Logger.Error($"VMap file '{Global.WorldMgr.DataPath + "/vmaps/" + name}' couldn't be loaded");
                    Log.Logger.Error("This is because VMAP files are corrupted, please re-extract the maps with the tools compiled with this module.");

                    return false;
                case LoadResult.DisabledInConfig:
                    Log.Logger.Error($"VMap file '{Global.WorldMgr.DataPath + "/vmaps/" + name}' couldn't be loaded");
                    Log.Logger.Error("This is because VMAP is disabled in config file.");

                    return false;
            }
        }

        return true;
    }

    public static bool IsInWMOInterior(uint mogpFlags)
    {
        return (mogpFlags & 0x2000) != 0;
    }

    public void AddChildTerrain(TerrainInfo childTerrain)
    {
        childTerrain._parentTerrain = this;
        _childTerrain.Add(childTerrain);
    }

    public void CleanUpGrids(uint diff)
    {
        if (_keepLoaded)
            return;

        _cleanupTimer.Update(diff);

        if (!_cleanupTimer.Passed)
            return;

        // delete those GridMap objects which have refcount = 0
        for (var x = 0; x < MapConst.MaxGrids; ++x)
            for (var y = 0; y < MapConst.MaxGrids; ++y)
                if (_loadedGrids[GetBitsetIndex(x, y)] && _referenceCountFromMap[x][y] == 0)
                    UnloadMapImpl(x, y);

        _cleanupTimer.Reset(_cleanupInterval);
    }

    public void DiscoverGridMapFiles()
    {
        var tileListName = $"{Global.WorldMgr.DataPath}/maps/{GetId():D4}.tilelist";

        // tile list is optional
        if (File.Exists(tileListName))
        {
            using var reader = new BinaryReader(new FileStream(tileListName, FileMode.Open, FileAccess.Read));
            var mapMagic = reader.ReadUInt32();
            var versionMagic = reader.ReadUInt32();

            if (mapMagic == MapConst.MapMagic && versionMagic == MapConst.MapVersionMagic)
            {
                var build = reader.ReadUInt32();
                var tilesData = reader.ReadArray<byte>(MapConst.MaxGrids * MapConst.MaxGrids);
                Array.Reverse(tilesData);

                for (var gx = 0; gx < MapConst.MaxGrids; ++gx)
                    for (var gy = 0; gy < MapConst.MaxGrids; ++gy)
                        _gridFileExists[GetBitsetIndex(gx, gy)] = tilesData[GetBitsetIndex(gx, gy)] == 49; // char of 1

                return;
            }
        }

        for (var gx = 0; gx < MapConst.MaxGrids; ++gx)
            for (var gy = 0; gy < MapConst.MaxGrids; ++gy)
                _gridFileExists[GetBitsetIndex(gx, gy)] = ExistMap(GetId(), gx, gy, false);
    }

    public uint GetAreaId(PhaseShift phaseShift, uint mapId, Position pos, DynamicMapTree dynamicMapTree = null)
    {
        return GetAreaId(phaseShift, mapId, pos.X, pos.Y, pos.Z, dynamicMapTree);
    }

    public uint GetAreaId(PhaseShift phaseShift, uint mapId, float x, float y, float z, DynamicMapTree dynamicMapTree = null)
    {
        var vmapZ = z;
        var hasVmapArea = GetAreaInfo(phaseShift, mapId, x, y, vmapZ, out var mogpFlags, out var adtId, out var rootId, out var groupId, dynamicMapTree);

        uint gridAreaId = 0;
        var gridMapHeight = MapConst.InvalidHeight;
        var gmap = GetGrid(PhasingHandler.GetTerrainMapId(phaseShift, mapId, this, x, y), x, y);

        if (gmap != null)
        {
            gridAreaId = gmap.GetArea(x, y);
            gridMapHeight = gmap.GetHeight(x, y);
        }

        uint areaId = 0;

        // floor is the height we are closer to (but only if above)
        if (hasVmapArea && MathFunctions.fuzzyGe(z, vmapZ - MapConst.GroundHeightTolerance) && (MathFunctions.fuzzyLt(z, gridMapHeight - MapConst.GroundHeightTolerance) || vmapZ > gridMapHeight))
        {
            // wmo found
            var wmoEntry = Global.DB2Mgr.GetWMOAreaTable(rootId, adtId, groupId);

            if (wmoEntry != null)
                areaId = wmoEntry.AreaTableID;

            if (areaId == 0)
                areaId = gridAreaId;
        }
        else
        {
            areaId = gridAreaId;
        }

        if (areaId == 0)
            areaId = CliDB.MapStorage.LookupByKey(GetId()).AreaTableID;

        return areaId;
    }

    public bool GetAreaInfo(PhaseShift phaseShift, uint mapId, float x, float y, float z, out uint mogpflags, out int adtId, out int rootId, out int groupId, DynamicMapTree dynamicMapTree = null)
    {
        mogpflags = 0;
        adtId = 0;
        rootId = 0;
        groupId = 0;

        var vmap_z = z;
        var dynamic_z = z;
        var check_z = z;
        var terrainMapId = PhasingHandler.GetTerrainMapId(phaseShift, mapId, this, x, y);

        uint dflags = 0;
        var dadtId = 0;
        var drootId = 0;
        var dgroupId = 0;

        var hasVmapAreaInfo = Global.VMapMgr.GetAreaInfo(terrainMapId, x, y, ref vmap_z, out var vflags, out var vadtId, out var vrootId, out var vgroupId);
        var hasDynamicAreaInfo = dynamicMapTree?.GetAreaInfo(x, y, ref dynamic_z, phaseShift, out dflags, out dadtId, out drootId, out dgroupId) ?? false;

        if (hasVmapAreaInfo)
        {
            if (hasDynamicAreaInfo && dynamic_z > vmap_z)
            {
                check_z = dynamic_z;
                mogpflags = dflags;
                adtId = dadtId;
                rootId = drootId;
                groupId = dgroupId;
            }
            else
            {
                check_z = vmap_z;
                mogpflags = vflags;
                adtId = vadtId;
                rootId = vrootId;
                groupId = vgroupId;
            }
        }
        else if (hasDynamicAreaInfo)
        {
            check_z = dynamic_z;
            mogpflags = dflags;
            adtId = dadtId;
            rootId = drootId;
            groupId = dgroupId;
        }

        if (hasVmapAreaInfo || hasDynamicAreaInfo)
        {
            // check if there's terrain between player height and object height
            var gmap = GetGrid(terrainMapId, x, y);

            if (gmap != null)
            {
                var mapHeight = gmap.GetHeight(x, y);

                // z + 2.0f condition taken from GetHeight(), not sure if it's such a great choice...
                if (z + 2.0f > mapHeight && mapHeight > check_z)
                    return false;
            }

            return true;
        }

        return false;
    }

    public void GetFullTerrainStatusForPosition(PhaseShift phaseShift, uint mapId, float x, float y, float z, PositionFullTerrainStatus data, LiquidHeaderTypeFlags reqLiquidType = LiquidHeaderTypeFlags.AllLiquids, float collisionHeight = MapConst.DefaultCollesionHeight, DynamicMapTree dynamicMapTree = null)
    {
        AreaAndLiquidData dynData = null;
        AreaAndLiquidData wmoData = null;

        var terrainMapId = PhasingHandler.GetTerrainMapId(phaseShift, mapId, this, x, y);
        var gmap = GetGrid(terrainMapId, x, y);
        var vmapData = Global.VMapMgr.GetAreaAndLiquidData(terrainMapId, x, y, z, (byte)reqLiquidType);

        if (dynamicMapTree != null)
            dynData = dynamicMapTree.GetAreaAndLiquidData(x, y, z, phaseShift, (byte)reqLiquidType);

        uint gridAreaId = 0;
        var gridMapHeight = MapConst.InvalidHeight;

        if (gmap != null)
        {
            gridAreaId = gmap.GetArea(x, y);
            gridMapHeight = gmap.GetHeight(x, y);
        }

        var useGridLiquid = true;

        // floor is the height we are closer to (but only if above)
        data.FloorZ = MapConst.InvalidHeight;

        if (gridMapHeight > MapConst.InvalidHeight && MathFunctions.fuzzyGe(z, gridMapHeight - MapConst.GroundHeightTolerance))
            data.FloorZ = gridMapHeight;

        if (vmapData.FloorZ > MapConst.InvalidHeight &&
            MathFunctions.fuzzyGe(z, vmapData.FloorZ - MapConst.GroundHeightTolerance) &&
            (MathFunctions.fuzzyLt(z, gridMapHeight - MapConst.GroundHeightTolerance) || vmapData.FloorZ > gridMapHeight))
        {
            data.FloorZ = vmapData.FloorZ;
            wmoData = vmapData;
        }

        // NOTE: Objects will not detect a case when a wmo providing area/liquid despawns from under them
        // but this is fine as these kind of objects are not meant to be spawned and despawned a lot
        // example: Lich King platform
        if (dynData.FloorZ > MapConst.InvalidHeight &&
            MathFunctions.fuzzyGe(z, dynData.FloorZ - MapConst.GroundHeightTolerance) &&
            (MathFunctions.fuzzyLt(z, gridMapHeight - MapConst.GroundHeightTolerance) || dynData.FloorZ > gridMapHeight) &&
            (MathFunctions.fuzzyLt(z, vmapData.FloorZ - MapConst.GroundHeightTolerance) || dynData.FloorZ > vmapData.FloorZ))
        {
            data.FloorZ = dynData.FloorZ;
            wmoData = dynData;
        }

        if (wmoData != null)
        {
            if (wmoData.AreaInfo.HasValue)
            {
                data.AreaInfo = new PositionFullTerrainStatus.AreaInfoModel(wmoData.AreaInfo.Value.AdtId, wmoData.AreaInfo.Value.RootId, wmoData.AreaInfo.Value.GroupId, wmoData.AreaInfo.Value.MogpFlags);
                // wmo found
                var wmoEntry = Global.DB2Mgr.GetWMOAreaTable(wmoData.AreaInfo.Value.RootId, wmoData.AreaInfo.Value.AdtId, wmoData.AreaInfo.Value.GroupId);

                if (wmoEntry == null)
                    wmoEntry = Global.DB2Mgr.GetWMOAreaTable(wmoData.AreaInfo.Value.RootId, wmoData.AreaInfo.Value.AdtId, -1);

                data.Outdoors = (wmoData.AreaInfo.Value.MogpFlags & 0x8) != 0;

                if (wmoEntry != null)
                {
                    data.AreaId = wmoEntry.AreaTableID;

                    if ((wmoEntry.Flags & 4) != 0)
                        data.Outdoors = true;
                    else if ((wmoEntry.Flags & 2) != 0)
                        data.Outdoors = false;
                }

                if (data.AreaId == 0)
                    data.AreaId = gridAreaId;

                useGridLiquid = !IsInWMOInterior(wmoData.AreaInfo.Value.MogpFlags);
            }
        }
        else
        {
            data.Outdoors = true;
            data.AreaId = gridAreaId;
            var areaEntry1 = CliDB.AreaTableStorage.LookupByKey(data.AreaId);

            if (areaEntry1 != null)
                data.Outdoors = ((AreaFlags)areaEntry1.Flags[0] & (AreaFlags.Inside | AreaFlags.Outside)) != AreaFlags.Inside;
        }

        if (data.AreaId == 0)
            data.AreaId = CliDB.MapStorage.LookupByKey(GetId()).AreaTableID;

        var areaEntry = CliDB.AreaTableStorage.LookupByKey(data.AreaId);

        // liquid processing
        data.LiquidStatus = ZLiquidStatus.NoWater;

        if (wmoData is { LiquidInfo: { } } && wmoData.LiquidInfo.Value.Level > wmoData.FloorZ)
        {
            var liquidType = wmoData.LiquidInfo.Value.LiquidType;

            if (GetId() == 530 && liquidType == 2) // gotta love hacks
                liquidType = 15;

            uint liquidFlagType = 0;
            var liquidData = CliDB.LiquidTypeStorage.LookupByKey(liquidType);

            if (liquidData != null)
                liquidFlagType = liquidData.SoundBank;

            if (liquidType != 0 && liquidType < 21 && areaEntry != null)
            {
                uint overrideLiquid = areaEntry.LiquidTypeID[liquidFlagType];

                if (overrideLiquid == 0 && areaEntry.ParentAreaID != 0)
                {
                    var zoneEntry = CliDB.AreaTableStorage.LookupByKey(areaEntry.ParentAreaID);

                    if (zoneEntry != null)
                        overrideLiquid = zoneEntry.LiquidTypeID[liquidFlagType];
                }

                var overrideData = CliDB.LiquidTypeStorage.LookupByKey(overrideLiquid);

                if (overrideData != null)
                {
                    liquidType = overrideLiquid;
                    liquidFlagType = overrideData.SoundBank;
                }
            }

            data.LiquidInfo = new LiquidData
            {
                level = wmoData.LiquidInfo.Value.Level,
                depth_level = wmoData.FloorZ,
                entry = liquidType,
                type_flags = (LiquidHeaderTypeFlags)(1 << (int)liquidFlagType)
            };

            var delta = wmoData.LiquidInfo.Value.Level - z;

            if (delta > collisionHeight)
                data.LiquidStatus = ZLiquidStatus.UnderWater;
            else if (delta > 0.0f)
                data.LiquidStatus = ZLiquidStatus.InWater;
            else if (delta > -0.1f)
                data.LiquidStatus = ZLiquidStatus.WaterWalk;
            else
                data.LiquidStatus = ZLiquidStatus.AboveWater;
        }

        // look up liquid data from grid map
        if (gmap != null && useGridLiquid)
        {
            LiquidData gridMapLiquid = new();
            var gridMapStatus = gmap.GetLiquidStatus(x, y, z, reqLiquidType, gridMapLiquid, collisionHeight);

            if (gridMapStatus != ZLiquidStatus.NoWater && (wmoData == null || gridMapLiquid.level > wmoData.FloorZ))
            {
                if (GetId() == 530 && gridMapLiquid.entry == 2)
                    gridMapLiquid.entry = 15;

                data.LiquidInfo = gridMapLiquid;
                data.LiquidStatus = gridMapStatus;
            }
        }
    }

    public GridMap GetGrid(uint mapId, float x, float y, bool loadIfMissing = true)
    {
        // half opt method
        var gx = (int)(MapConst.CenterGridId - x / MapConst.SizeofGrids); //grid x
        var gy = (int)(MapConst.CenterGridId - y / MapConst.SizeofGrids); //grid y

        // ensure GridMap is loaded
        if (!_loadedGrids[GetBitsetIndex(gx, gy)] && loadIfMissing)
            lock (_loadLock)
            {
                LoadMapAndVMapImpl(gx, gy);
            }

        var grid = _gridMap[gx][gy];

        if (mapId != GetId())
        {
            var childMap = _childTerrain.Find(childTerrain => childTerrain.GetId() == mapId);

            if (childMap != null && childMap._gridMap[gx][gy] != null)
                grid = childMap.GetGrid(mapId, x, y, false);
        }

        return grid;
    }

    public float GetGridHeight(PhaseShift phaseShift, uint mapId, float x, float y)
    {
        var gmap = GetGrid(PhasingHandler.GetTerrainMapId(phaseShift, mapId, this, x, y), x, y);

        if (gmap != null)
            return gmap.GetHeight(x, y);

        return MapConst.VMAPInvalidHeightValue;
    }

    public uint GetId()
    {
        return _mapId;
    }

    public ZLiquidStatus GetLiquidStatus(PhaseShift phaseShift, uint mapId, float x, float y, float z, LiquidHeaderTypeFlags ReqLiquidType, out LiquidData data, float collisionHeight = MapConst.DefaultCollesionHeight)
    {
        data = null;

        var result = ZLiquidStatus.NoWater;
        var liquid_level = MapConst.InvalidHeight;
        var ground_level = MapConst.InvalidHeight;
        uint liquid_type = 0;
        uint mogpFlags = 0;
        var useGridLiquid = true;
        var terrainMapId = PhasingHandler.GetTerrainMapId(phaseShift, mapId, this, x, y);

        if (Global.VMapMgr.GetLiquidLevel(terrainMapId, x, y, z, (byte)ReqLiquidType, ref liquid_level, ref ground_level, ref liquid_type, ref mogpFlags))
        {
            useGridLiquid = !IsInWMOInterior(mogpFlags);
            Log.Logger.Debug($"GetLiquidStatus(): vmap liquid level: {liquid_level} ground: {ground_level} type: {liquid_type}");

            // Check water level and ground level
            if (liquid_level > ground_level && MathFunctions.fuzzyGe(z, ground_level - MapConst.GroundHeightTolerance))
            {
                // All ok in water . store data
                data = new LiquidData();

                // hardcoded in client like this
                if (GetId() == 530 && liquid_type == 2)
                    liquid_type = 15;

                uint liquidFlagType = 0;
                var liq = CliDB.LiquidTypeStorage.LookupByKey(liquid_type);

                if (liq != null)
                    liquidFlagType = liq.SoundBank;

                if (liquid_type != 0 && liquid_type < 21)
                {
                    var area = CliDB.AreaTableStorage.LookupByKey(GetAreaId(phaseShift, mapId, x, y, z));

                    if (area != null)
                    {
                        uint overrideLiquid = area.LiquidTypeID[liquidFlagType];

                        if (overrideLiquid == 0 && area.ParentAreaID != 0)
                        {
                            area = CliDB.AreaTableStorage.LookupByKey(area.ParentAreaID);

                            if (area != null)
                                overrideLiquid = area.LiquidTypeID[liquidFlagType];
                        }

                        var liq1 = CliDB.LiquidTypeStorage.LookupByKey(overrideLiquid);

                        if (liq1 != null)
                        {
                            liquid_type = overrideLiquid;
                            liquidFlagType = liq1.SoundBank;
                        }
                    }
                }

                data.level = liquid_level;
                data.depth_level = ground_level;

                data.entry = liquid_type;
                data.type_flags = (LiquidHeaderTypeFlags)(1 << (int)liquidFlagType);

                var delta = liquid_level - z;

                // Get position delta
                if (delta > collisionHeight) // Under water
                    return ZLiquidStatus.UnderWater;

                if (delta > 0.0f) // In water
                    return ZLiquidStatus.InWater;

                if (delta > -0.1f) // Walk on water
                    return ZLiquidStatus.WaterWalk;

                result = ZLiquidStatus.AboveWater;
            }
        }

        if (useGridLiquid)
        {
            var gmap = GetGrid(terrainMapId, x, y);

            if (gmap != null)
            {
                LiquidData map_data = new();
                var map_result = gmap.GetLiquidStatus(x, y, z, ReqLiquidType, map_data, collisionHeight);

                // Not override LIQUID_MAP_ABOVE_WATER with LIQUID_MAP_NO_WATER:
                if (map_result != ZLiquidStatus.NoWater && (map_data.level > ground_level))
                {
                    // hardcoded in client like this
                    if (GetId() == 530 && map_data.entry == 2)
                        map_data.entry = 15;

                    data = map_data;

                    return map_result;
                }
            }
        }

        return result;
    }

    public string GetMapName()
    {
        return CliDB.MapStorage.LookupByKey(GetId()).MapName[Global.WorldMgr.DefaultDbcLocale];
    }
    public float GetMinHeight(PhaseShift phaseShift, uint mapId, float x, float y)
    {
        var grid = GetGrid(PhasingHandler.GetTerrainMapId(phaseShift, mapId, this, x, y), x, y);

        if (grid != null)
            return grid.GetMinHeight(x, y);

        return -500.0f;
    }

    public float GetStaticHeight(PhaseShift phaseShift, uint mapId, Position pos, bool checkVMap = true, float maxSearchDist = MapConst.DefaultHeightSearch)
    {
        return GetStaticHeight(phaseShift, mapId, pos.X, pos.Y, pos.Z, checkVMap, maxSearchDist);
    }

    public float GetStaticHeight(PhaseShift phaseShift, uint mapId, float x, float y, float z, bool checkVMap = true, float maxSearchDist = MapConst.DefaultHeightSearch)
    {
        // find raw .map surface under Z coordinates
        var mapHeight = MapConst.VMAPInvalidHeightValue;
        var terrainMapId = PhasingHandler.GetTerrainMapId(phaseShift, mapId, this, x, y);

        var gridHeight = GetGridHeight(phaseShift, mapId, x, y);

        if (MathFunctions.fuzzyGe(z, gridHeight - MapConst.GroundHeightTolerance))
            mapHeight = gridHeight;

        var vmapHeight = MapConst.VMAPInvalidHeightValue;

        if (checkVMap)
            if (Global.VMapMgr.IsHeightCalcEnabled)
                vmapHeight = Global.VMapMgr.GetHeight(terrainMapId, x, y, z, maxSearchDist);

        // mapHeight set for any above raw ground Z or <= INVALID_HEIGHT
        // vmapheight set for any under Z value or <= INVALID_HEIGHT
        if (vmapHeight > MapConst.InvalidHeight)
        {
            if (mapHeight > MapConst.InvalidHeight)
            {
                // we have mapheight and vmapheight and must select more appropriate

                // vmap height above map height
                // or if the distance of the vmap height is less the land height distance
                if (vmapHeight > mapHeight || Math.Abs(mapHeight - z) > Math.Abs(vmapHeight - z))
                    return vmapHeight;

                return mapHeight; // better use .map surface height
            }

            return vmapHeight; // we have only vmapHeight (if have)
        }

        return mapHeight; // explicitly use map data
    }

    public float GetWaterLevel(PhaseShift phaseShift, uint mapId, float x, float y)
    {
        var gmap = GetGrid(PhasingHandler.GetTerrainMapId(phaseShift, mapId, this, x, y), x, y);

        if (gmap != null)
            return gmap.GetLiquidLevel(x, y);

        return 0;
    }

    public float GetWaterOrGroundLevel(PhaseShift phaseShift, uint mapId, float x, float y, float z, ref float ground, bool swim = false, float collisionHeight = MapConst.DefaultCollesionHeight, DynamicMapTree dynamicMapTree = null)
    {
        if (GetGrid(PhasingHandler.GetTerrainMapId(phaseShift, mapId, this, x, y), x, y) != null)
        {
            // we need ground level (including grid height version) for proper return water level in point
            var ground_z = GetStaticHeight(phaseShift, mapId, x, y, z + MapConst.ZOffsetFindHeight);

            if (dynamicMapTree != null)
                ground_z = Math.Max(ground_z, dynamicMapTree.GetHeight(x, y, z + MapConst.ZOffsetFindHeight, 50.0f, phaseShift));

            ground = ground_z;

            var res = GetLiquidStatus(phaseShift, mapId, x, y, ground_z, LiquidHeaderTypeFlags.AllLiquids, out var liquid_status, collisionHeight);

            switch (res)
            {
                case ZLiquidStatus.AboveWater:
                    return Math.Max(liquid_status.level, ground_z);
                case ZLiquidStatus.NoWater:
                    return ground_z;
                default:
                    return liquid_status.level;
            }
        }

        return MapConst.VMAPInvalidHeightValue;
    }

    public void GetZoneAndAreaId(PhaseShift phaseShift, uint mapId, out uint zoneid, out uint areaid, Position pos, DynamicMapTree dynamicMapTree = null)
    {
        GetZoneAndAreaId(phaseShift, mapId, out zoneid, out areaid, pos.X, pos.Y, pos.Z, dynamicMapTree);
    }

    public void GetZoneAndAreaId(PhaseShift phaseShift, uint mapId, out uint zoneid, out uint areaid, float x, float y, float z, DynamicMapTree dynamicMapTree = null)
    {
        areaid = zoneid = GetAreaId(phaseShift, mapId, x, y, z, dynamicMapTree);
        var area = CliDB.AreaTableStorage.LookupByKey(areaid);

        if (area != null)
            if (area.ParentAreaID != 0)
                zoneid = area.ParentAreaID;
    }

    public uint GetZoneId(PhaseShift phaseShift, uint mapId, Position pos, DynamicMapTree dynamicMapTree = null)
    {
        return GetZoneId(phaseShift, mapId, pos.X, pos.Y, pos.Z, dynamicMapTree);
    }

    public uint GetZoneId(PhaseShift phaseShift, uint mapId, float x, float y, float z, DynamicMapTree dynamicMapTree = null)
    {
        var areaId = GetAreaId(phaseShift, mapId, x, y, z, dynamicMapTree);
        var area = CliDB.AreaTableStorage.LookupByKey(areaId);

        if (area != null)
            if (area.ParentAreaID != 0)
                return area.ParentAreaID;

        return areaId;
    }

    public bool HasChildTerrainGridFile(uint mapId, int gx, int gy)
    {
        var childMap = _childTerrain.Find(childTerrain => childTerrain.GetId() == mapId);

        return childMap != null && childMap._gridFileExists[GetBitsetIndex(gx, gy)];
    }
    public bool IsInWater(PhaseShift phaseShift, uint mapId, float x, float y, float pZ, out LiquidData data)
    {
        return (GetLiquidStatus(phaseShift, mapId, x, y, pZ, LiquidHeaderTypeFlags.AllLiquids, out data) & (ZLiquidStatus.InWater | ZLiquidStatus.UnderWater)) != 0;
    }

    public bool IsUnderWater(PhaseShift phaseShift, uint mapId, float x, float y, float z)
    {
        return (GetLiquidStatus(phaseShift, mapId, x, y, z, LiquidHeaderTypeFlags.Water | LiquidHeaderTypeFlags.Ocean, out _) & ZLiquidStatus.UnderWater) != 0;
    }

    public void LoadMap(int gx, int gy)
    {
        if (_gridMap[gx][gy] != null)
            return;

        if (!_gridFileExists[GetBitsetIndex(gx, gy)])
            return;

        // map file name
        var fileName = $"{Global.WorldMgr.DataPath}/maps/{GetId():D4}_{gx:D2}_{gy:D2}.map";
        Log.Logger.Information($"Loading map {fileName}");

        // loading data
        GridMap gridMap = new();
        var gridMapLoadResult = gridMap.LoadData(fileName);

        if (gridMapLoadResult == LoadResult.Success)
            _gridMap[gx][gy] = gridMap;
        else
            _gridFileExists[GetBitsetIndex(gx, gy)] = false;

        if (gridMapLoadResult == LoadResult.ReadFromFileFailed)
            Log.Logger.Error($"Error loading map file: {fileName}");
    }

    public void LoadMapAndVMap(int gx, int gy)
    {
        if (++_referenceCountFromMap[gx][gy] != 1) // check if already loaded
            return;

        lock (_loadLock)
        {
            LoadMapAndVMapImpl(gx, gy);
        }
    }

    public void LoadMapAndVMapImpl(int gx, int gy)
    {
        LoadMap(gx, gy);
        LoadVMap(gx, gy);
        LoadMMap(gx, gy);

        foreach (var childTerrain in _childTerrain)
            childTerrain.LoadMapAndVMapImpl(gx, gy);

        _loadedGrids[GetBitsetIndex(gx, gy)] = true;
    }
    public void LoadMMap(int gx, int gy)
    {
        if (!Global.DisableMgr.IsPathfindingEnabled(GetId()))
            return;

        var mmapLoadResult = Global.MMapMgr.LoadMap(Global.WorldMgr.DataPath, GetId(), gx, gy);

        if (mmapLoadResult)
            Log.Logger.Debug($"MMAP loaded name:{GetMapName()}, id:{GetId()}, x:{gx}, y:{gy} (mmap rep.: x:{gx}, y:{gy})");
        else
            Log.Logger.Warning($"Could not load MMAP name:{GetMapName()}, id:{GetId()}, x:{gx}, y:{gy} (mmap rep.: x:{gx}, y:{gy})");
    }

    public void LoadVMap(int gx, int gy)
    {
        if (!Global.VMapMgr.IsMapLoadingEnabled)
            return;

        // x and y are swapped !!
        var vmapLoadResult = Global.VMapMgr.LoadMap(GetId(), gx, gy);

        switch (vmapLoadResult)
        {
            case LoadResult.Success:
                Log.Logger.Debug($"VMAP loaded name:{GetMapName()}, id:{GetId()}, x:{gx}, y:{gy} (vmap rep.: x:{gx}, y:{gy})");

                break;
            case LoadResult.VersionMismatch:
            case LoadResult.ReadFromFileFailed:
                Log.Logger.Error($"Could not load VMAP name:{GetMapName()}, id:{GetId()}, x:{gx}, y:{gy} (vmap rep.: x:{gx}, y:{gy})");

                break;
            case LoadResult.DisabledInConfig:
                Log.Logger.Debug($"Ignored VMAP name:{GetMapName()}, id:{GetId()}, x:{gx}, y:{gy} (vmap rep.: x:{gx}, y:{gy})");

                break;
        }
    }
    public void UnloadMap(int gx, int gy)
    {
        if (_keepLoaded)
            return;

        --_referenceCountFromMap[gx][gy];
        // unload later
    }

    public void UnloadMapImpl(int gx, int gy)
    {
        if (_keepLoaded)
            return;

        _gridMap[gx][gy] = null;
        Global.VMapMgr.UnloadMap(GetId(), gx, gy);
        Global.MMapMgr.UnloadMap(GetId(), gx, gy);

        foreach (var childTerrain in _childTerrain)
            childTerrain.UnloadMapImpl(gx, gy);

        _loadedGrids[GetBitsetIndex(gx, gy)] = false;
    }
    private static int GetBitsetIndex(int gx, int gy)
    {
        return gx * MapConst.MaxGrids + gy;
    }
}