// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;

namespace Game.Collision;

public class VMapManager : Singleton<VMapManager>
{
	public static string VMapPath = Global.WorldMgr.DataPath + "/vmaps/";

	readonly Dictionary<string, ManagedModel> _loadedModelFiles = new();
	readonly Dictionary<uint, StaticMapTree> _instanceMapTrees = new();
	readonly Dictionary<uint, uint> _parentMapData = new();
	readonly object _loadedModelFilesLock = new();
	bool _enableLineOfSightCalc;
	bool _enableHeightCalc;

	public bool IsLineOfSightCalcEnabled => _enableLineOfSightCalc;

	public bool IsHeightCalcEnabled => _enableHeightCalc;

	public bool IsMapLoadingEnabled => _enableLineOfSightCalc || _enableHeightCalc;
	VMapManager() { }

	public void Initialize(MultiMap<uint, uint> mapData)
	{
		foreach (var pair in mapData.KeyValueList)
			_parentMapData[pair.Value] = pair.Key;
	}

	public LoadResult LoadMap(uint mapId, int x, int y)
	{
		if (!IsMapLoadingEnabled)
			return LoadResult.DisabledInConfig;

		var instanceTree = _instanceMapTrees.LookupByKey(mapId);

		if (instanceTree == null)
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

	public void UnloadMap(uint mapId, int x, int y)
	{
		var instanceTree = _instanceMapTrees.LookupByKey(mapId);

		if (instanceTree != null)
		{
			instanceTree.UnloadMapTile(x, y, this);

			if (instanceTree.NumLoadedTiles() == 0)
				_instanceMapTrees.Remove(mapId);
		}
	}

	public void UnloadMap(uint mapId)
	{
		var instanceTree = _instanceMapTrees.LookupByKey(mapId);

		if (instanceTree != null)
		{
			instanceTree.UnloadMap(this);

			if (instanceTree.NumLoadedTiles() == 0)
				_instanceMapTrees.Remove(mapId);
		}
	}

	public bool IsInLineOfSight(uint mapId, float x1, float y1, float z1, float x2, float y2, float z2, ModelIgnoreFlags ignoreFlags)
	{
		if (!IsLineOfSightCalcEnabled || Global.DisableMgr.IsVMAPDisabledFor(mapId, (byte)DisableFlags.VmapLOS))
			return true;

		var instanceTree = _instanceMapTrees.LookupByKey(mapId);

		if (instanceTree != null)
		{
			var pos1 = ConvertPositionToInternalRep(x1, y1, z1);
			var pos2 = ConvertPositionToInternalRep(x2, y2, z2);

			if (pos1 != pos2)
				return instanceTree.IsInLineOfSight(pos1, pos2, ignoreFlags);
		}

		return true;
	}

	public bool GetObjectHitPos(uint mapId, float x1, float y1, float z1, float x2, float y2, float z2, out float rx, out float ry, out float rz, float modifyDist)
	{
		if (IsLineOfSightCalcEnabled && !Global.DisableMgr.IsVMAPDisabledFor(mapId, (byte)DisableFlags.VmapLOS))
		{
			var instanceTree = _instanceMapTrees.LookupByKey(mapId);

			if (instanceTree != null)
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

	public float GetHeight(uint mapId, float x, float y, float z, float maxSearchDist)
	{
		if (IsHeightCalcEnabled && !Global.DisableMgr.IsVMAPDisabledFor(mapId, (byte)DisableFlags.VmapHeight))
		{
			var instanceTree = _instanceMapTrees.LookupByKey(mapId);

			if (instanceTree != null)
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

	public bool GetAreaInfo(uint mapId, float x, float y, ref float z, out uint flags, out int adtId, out int rootId, out int groupId)
	{
		flags = 0;
		adtId = 0;
		rootId = 0;
		groupId = 0;

		if (!Global.DisableMgr.IsVMAPDisabledFor(mapId, (byte)DisableFlags.VmapAreaFlag))
		{
			var instanceTree = _instanceMapTrees.LookupByKey(mapId);

			if (instanceTree != null)
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

	public bool GetLiquidLevel(uint mapId, float x, float y, float z, uint reqLiquidType, ref float level, ref float floor, ref uint type, ref uint mogpFlags)
	{
		if (!Global.DisableMgr.IsVMAPDisabledFor(mapId, (byte)DisableFlags.VmapLiquidStatus))
		{
			var instanceTree = _instanceMapTrees.LookupByKey(mapId);

			if (instanceTree != null)
			{
				LocationInfo info = new();
				var pos = ConvertPositionToInternalRep(x, y, z);

				if (instanceTree.GetLocationInfo(pos, info))
				{
					floor = info.GroundZ;
					type = info.HitModel.GetLiquidType(); // entry from LiquidType.dbc
					mogpFlags = info.HitModel.GetMogpFlags();

					if (reqLiquidType != 0 && !Convert.ToBoolean(Global.DB2Mgr.GetLiquidFlags(type) & reqLiquidType))
						return false;

					if (info.HitInstance.GetLiquidLevel(pos, info, ref level))
						return true;
				}
			}
		}

		return false;
	}

	public AreaAndLiquidData GetAreaAndLiquidData(uint mapId, float x, float y, float z, uint reqLiquidType)
	{
		var data = new AreaAndLiquidData();

		if (Global.DisableMgr.IsVMAPDisabledFor(mapId, (byte)DisableFlags.VmapLiquidStatus))
		{
			data.FloorZ = z;

			if (GetAreaInfo(mapId, x, y, ref data.FloorZ, out var flags, out var adtId, out var rootId, out var groupId))
				data.AreaInfo = new AreaAndLiquidData.AreaInfoModel(adtId, rootId, groupId, flags);

			return data;
		}

		var instanceTree = _instanceMapTrees.LookupByKey(mapId);

		if (instanceTree != null)
		{
			LocationInfo info = new();
			var pos = ConvertPositionToInternalRep(x, y, z);

			if (instanceTree.GetLocationInfo(pos, info))
			{
				data.FloorZ = info.GroundZ;
				var liquidType = info.HitModel.GetLiquidType();
				float liquidLevel = 0;

				if (reqLiquidType == 0 || Convert.ToBoolean(Global.DB2Mgr.GetLiquidFlags(liquidType) & reqLiquidType))
					if (info.HitInstance.GetLiquidLevel(pos, info, ref liquidLevel))
						data.LiquidInfo = new AreaAndLiquidData.LiquidInfoModel(liquidType, liquidLevel);

				if (!Global.DisableMgr.IsVMAPDisabledFor(mapId, (byte)DisableFlags.VmapLiquidStatus))
					data.AreaInfo = new AreaAndLiquidData.AreaInfoModel(info.HitInstance.AdtId, info.RootId, (int)info.HitModel.GetWmoID(), info.HitModel.GetMogpFlags());
			}
		}

		return data;
	}

	public WorldModel AcquireModelInstance(string filename, uint flags = 0)
	{
		lock (_loadedModelFilesLock)
		{
			filename = filename.TrimEnd('\0');
			var model = _loadedModelFiles.LookupByKey(filename);

			if (model == null)
			{
				model = new ManagedModel();

				if (!model.GetModel().ReadFile(VMapPath + filename))
				{
					Log.outError(LogFilter.Server, "VMapManager: could not load '{0}'", filename);

					return null;
				}

				Log.outDebug(LogFilter.Maps, "VMapManager: loading file '{0}'", filename);
				model.GetModel().Flags = flags;

				_loadedModelFiles.Add(filename, model);
			}

			model.IncRefCount();

			return model.GetModel();
		}
	}

	public void ReleaseModelInstance(string filename)
	{
		lock (_loadedModelFilesLock)
		{
			filename = filename.TrimEnd('\0');
			var model = _loadedModelFiles.LookupByKey(filename);

			if (model == null)
			{
				Log.outError(LogFilter.Server, "VMapManager: trying to unload non-loaded file '{0}'", filename);

				return;
			}

			if (model.DecRefCount() == 0)
			{
				Log.outDebug(LogFilter.Maps, "VMapManager: unloading file '{0}'", filename);
				_loadedModelFiles.Remove(filename);
			}
		}
	}

	public LoadResult ExistsMap(uint mapId, int x, int y)
	{
		return StaticMapTree.CanLoadMap(VMapPath, mapId, x, y, this);
	}

	public int GetParentMapId(uint mapId)
	{
		if (_parentMapData.ContainsKey(mapId))
			return (int)_parentMapData[mapId];

		return -1;
	}

	public static string GetMapFileName(uint mapId)
	{
		return $"{mapId:D4}.vmtree";
	}

	public void SetEnableLineOfSightCalc(bool pVal)
	{
		_enableLineOfSightCalc = pVal;
	}

	public void SetEnableHeightCalc(bool pVal)
	{
		_enableHeightCalc = pVal;
	}

	Vector3 ConvertPositionToInternalRep(float x, float y, float z)
	{
		Vector3 pos = new();
		var mid = 0.5f * 64.0f * 533.33333333f;
		pos.X = mid - x;
		pos.Y = mid - y;
		pos.Z = z;

		return pos;
	}
}