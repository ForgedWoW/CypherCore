// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Forged.MapServer.Collision.Maps;
using Forged.MapServer.Collision.Models;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Framework.Constants;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Collision.Management;

public class VMapManager
{
    private readonly DB2Manager _db2Manager;
    private readonly DisableManager _disableManager;
    private readonly Dictionary<uint, StaticMapTree> _instanceMapTrees = new();
    private readonly Dictionary<string, ManagedModel> _loadedModelFiles = new();
    private readonly object _loadedModelFilesLock = new();
    private readonly Dictionary<uint, uint> _parentMapData = new();

    public bool IsHeightCalcEnabled { get; private set; }
    public bool IsLineOfSightCalcEnabled { get; private set; }
    public bool IsMapLoadingEnabled => IsLineOfSightCalcEnabled || IsHeightCalcEnabled;
    public string VMapPath { get; }

    public VMapManager(IConfiguration configuration, DisableManager disableManager, DB2Manager db2Manager)
    {
        _disableManager = disableManager;
        _db2Manager = db2Manager;
        VMapPath = configuration.GetDefaultValue("DataDir", "./") + "/vmaps/";
    }

    public static string GetMapFileName(uint mapId)
    {
        return $"{mapId:D4}.vmtree";
    }

    public WorldModel AcquireModelInstance(string filename, uint flags = 0)
    {
        lock (_loadedModelFilesLock)
        {
            filename = filename.TrimEnd('\0');
            if (!_loadedModelFiles.TryGetValue(filename, out var model))
            {
                model = new ManagedModel();

                if (!model.GetModel().ReadFile(VMapPath + filename))
                {
                    Log.Logger.Error("VMapManager: could not load '{0}'", filename);

                    return null;
                }

                Log.Logger.Debug("VMapManager: loading file '{0}'", filename);
                model.GetModel().Flags = flags;

                _loadedModelFiles.Add(filename, model);
            }

            model.IncRefCount();

            return model.GetModel();
        }
    }

    public LoadResult ExistsMap(uint mapId, int x, int y)
    {
        return StaticMapTree.CanLoadMap(VMapPath, mapId, x, y, this);
    }

    public AreaAndLiquidData GetAreaAndLiquidData(uint mapId, float x, float y, float z, uint reqLiquidType)
    {
        var data = new AreaAndLiquidData();

        if (_disableManager.IsVMAPDisabledFor(mapId, (byte)DisableFlags.VmapLiquidStatus))
        {
            data.FloorZ = z;

            if (GetAreaInfo(mapId, x, y, ref data.FloorZ, out var flags, out var adtId, out var rootId, out var groupId))
                data.AreaInfo = new AreaAndLiquidData.AreaInfoModel(adtId, rootId, groupId, flags);

            return data;
        }

        if (_instanceMapTrees.TryGetValue(mapId, out var instanceTree))
        {
            LocationInfo info = new();
            var pos = ConvertPositionToInternalRep(x, y, z);

            if (instanceTree.GetLocationInfo(pos, info))
            {
                data.FloorZ = info.GroundZ;
                var liquidType = info.HitModel.LiquidType;
                float liquidLevel = 0;

                if (reqLiquidType == 0 || Convert.ToBoolean(_db2Manager.GetLiquidFlags(liquidType) & reqLiquidType))
                    if (info.HitInstance.GetLiquidLevel(pos, info, ref liquidLevel))
                        data.LiquidInfo = new AreaAndLiquidData.LiquidInfoModel(liquidType, liquidLevel);

                if (!_disableManager.IsVMAPDisabledFor(mapId, (byte)DisableFlags.VmapLiquidStatus))
                    data.AreaInfo = new AreaAndLiquidData.AreaInfoModel(info.HitInstance.AdtId, info.RootId, (int)info.HitModel.WmoID, info.HitModel.MogpFlags);
            }
        }

        return data;
    }

    public bool GetAreaInfo(uint mapId, float x, float y, ref float z, out uint flags, out int adtId, out int rootId, out int groupId)
    {
        flags = 0;
        adtId = 0;
        rootId = 0;
        groupId = 0;

        if (!_disableManager.IsVMAPDisabledFor(mapId, (byte)DisableFlags.VmapAreaFlag))
        {
            if (_instanceMapTrees.TryGetValue(mapId, out var instanceTree))
            {
                var pos = ConvertPositionToInternalRep(x, y, z);
                var result = instanceTree.GetAreaInfo(ref pos, out flags, out adtId, out rootId, out groupId);
                // z is not touched by convertPositionToInternalRep(), so just copy
                z = pos.Z;

                return result;
            }
        }

        return false;
    }

    public float GetHeight(uint mapId, float x, float y, float z, float maxSearchDist)
    {
        if (IsHeightCalcEnabled && !_disableManager.IsVMAPDisabledFor(mapId, (byte)DisableFlags.VmapHeight))
        {
            if (_instanceMapTrees.TryGetValue(mapId, out var instanceTree))
            {
                var pos = ConvertPositionToInternalRep(x, y, z);
                var height = instanceTree.GetHeight(pos, maxSearchDist);

                if (float.IsInfinity(height))
                    height = MapConst.VMAPInvalidHeightValue; // No height

                return height;
            }
        }

        return MapConst.VMAPInvalidHeightValue;
    }

    public bool GetLiquidLevel(uint mapId, float x, float y, float z, uint reqLiquidType, ref float level, ref float floor, ref uint type, ref uint mogpFlags)
    {
        if (!_disableManager.IsVMAPDisabledFor(mapId, (byte)DisableFlags.VmapLiquidStatus))
        {
            if (_instanceMapTrees.TryGetValue(mapId, out var instanceTree))
            {
                LocationInfo info = new();
                var pos = ConvertPositionToInternalRep(x, y, z);

                if (instanceTree.GetLocationInfo(pos, info))
                {
                    floor = info.GroundZ;
                    type = info.HitModel.LiquidType; // entry from LiquidType.dbc
                    mogpFlags = info.HitModel.MogpFlags;

                    if (reqLiquidType != 0 && !Convert.ToBoolean(_db2Manager.GetLiquidFlags(type) & reqLiquidType))
                        return false;

                    if (info.HitInstance.GetLiquidLevel(pos, info, ref level))
                        return true;
                }
            }
        }

        return false;
    }

    public bool GetObjectHitPos(uint mapId, float x1, float y1, float z1, float x2, float y2, float z2, out float rx, out float ry, out float rz, float modifyDist)
    {
        if (IsLineOfSightCalcEnabled && !_disableManager.IsVMAPDisabledFor(mapId, (byte)DisableFlags.VmapLOS))
        {
            if (_instanceMapTrees.TryGetValue(mapId, out var instanceTree))
            {
                var pos1 = ConvertPositionToInternalRep(x1, y1, z1);
                var pos2 = ConvertPositionToInternalRep(x2, y2, z2);
                var result = instanceTree.GetObjectHitPos(pos1, pos2, out var resultPos, modifyDist);
                resultPos = ConvertPositionToInternalRep(resultPos.X, resultPos.Y, resultPos.Z);
                rx = resultPos.X;
                ry = resultPos.Y;
                rz = resultPos.Z;

                return result;
            }
        }

        rx = x2;
        ry = y2;
        rz = z2;

        return false;
    }

    public int GetParentMapId(uint mapId)
    {
        if (_parentMapData.ContainsKey(mapId))
            return (int)_parentMapData[mapId];

        return -1;
    }

    public void Initialize(MultiMap<uint, uint> mapData)
    {
        foreach (var pair in mapData.KeyValueList)
            _parentMapData[pair.Value] = pair.Key;
    }

    public bool IsInLineOfSight(uint mapId, float x1, float y1, float z1, float x2, float y2, float z2, ModelIgnoreFlags ignoreFlags)
    {
        if (!IsLineOfSightCalcEnabled || _disableManager.IsVMAPDisabledFor(mapId, (byte)DisableFlags.VmapLOS))
            return true;

        if (_instanceMapTrees.TryGetValue(mapId, out var instanceTree))
        {
            var pos1 = ConvertPositionToInternalRep(x1, y1, z1);
            var pos2 = ConvertPositionToInternalRep(x2, y2, z2);

            if (pos1 != pos2)
                return instanceTree.IsInLineOfSight(pos1, pos2, ignoreFlags);
        }

        return true;
    }

    public LoadResult LoadMap(uint mapId, int x, int y)
    {
        if (!IsMapLoadingEnabled)
            return LoadResult.DisabledInConfig;

        if (!_instanceMapTrees.TryGetValue(mapId, out var instanceTree))
        {
            var filename = VMapPath + GetMapFileName(mapId);
            StaticMapTree newTree = new(mapId);
            var treeInitResult = newTree.InitMap(filename);

            if (treeInitResult != LoadResult.Success)
                return treeInitResult;

            _instanceMapTrees.Add(mapId, newTree);

            instanceTree = newTree;
        }

        return instanceTree.LoadMapTile(x, y, this);
    }

    public void ReleaseModelInstance(string filename)
    {
        lock (_loadedModelFilesLock)
        {
            filename = filename.TrimEnd('\0');
            if (!_loadedModelFiles.TryGetValue(filename, out var model))
            {
                Log.Logger.Error("VMapManager: trying to unload non-loaded file '{0}'", filename);

                return;
            }

            if (model.DecRefCount() == 0)
            {
                Log.Logger.Debug("VMapManager: unloading file '{0}'", filename);
                _loadedModelFiles.Remove(filename);
            }
        }
    }

    public void SetEnableHeightCalc(bool pVal)
    {
        IsHeightCalcEnabled = pVal;
    }

    public void SetEnableLineOfSightCalc(bool pVal)
    {
        IsLineOfSightCalcEnabled = pVal;
    }

    public void UnloadMap(uint mapId, int x, int y)
    {
        if (_instanceMapTrees.TryGetValue(mapId, out var instanceTree))
        {
            instanceTree.UnloadMapTile(x, y, this);

            if (instanceTree.NumLoadedTiles() == 0)
                _instanceMapTrees.Remove(mapId);
        }
    }

    public void UnloadMap(uint mapId)
    {
        if (_instanceMapTrees.TryGetValue(mapId, out var instanceTree))
        {
            instanceTree.UnloadMap(this);

            if (instanceTree.NumLoadedTiles() == 0)
                _instanceMapTrees.Remove(mapId);
        }
    }

    private Vector3 ConvertPositionToInternalRep(float x, float y, float z)
    {
        Vector3 pos = new();
        var mid = 0.5f * 64.0f * 533.33333333f;
        pos.X = mid - x;
        pos.Y = mid - y;
        pos.Z = z;

        return pos;
    }
}