// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Numerics;
using Forged.MapServer.Collision.Management;
using Forged.MapServer.Collision.Models;
using Framework.Constants;
using Framework.GameMath;
using Serilog;

namespace Forged.MapServer.Collision.Maps;

public class StaticMapTree
{
	readonly uint _mapId;
    readonly BIH _tree = new();
	readonly ConcurrentDictionary<uint, uint> _spawnIndices = new();
	readonly ConcurrentDictionary<uint, bool> _loadedTiles = new();
	readonly ConcurrentDictionary<uint, uint> _loadedSpawns = new();
	ModelInstance[] _treeValues;
	uint _nTreeValues;

	public StaticMapTree(uint mapId)
    {
        _mapId = mapId;
    }

	public LoadResult InitMap(string fname)
	{
		Log.Logger.Debug("StaticMapTree.InitMap() : initializing StaticMapTree '{0}'", fname);

		if (!File.Exists(fname))
			return LoadResult.FileNotFound;

        using BinaryReader reader = new(new FileStream(fname, FileMode.Open, FileAccess.Read));

        var magic = reader.ReadStringFromChars(8);

        if (magic != MapConst.VMapMagic)
            return LoadResult.VersionMismatch;

        var node = reader.ReadStringFromChars(4);

        if (node != "NODE")
            return LoadResult.ReadFromFileFailed;

        if (!_tree.ReadFromFile(reader))
            return LoadResult.ReadFromFileFailed;

        _nTreeValues = _tree.PrimCount();
        _treeValues = new ModelInstance[_nTreeValues];

        if (reader.ReadStringFromChars(4) != "SIDX")
            return LoadResult.ReadFromFileFailed;

        var spawnIndicesSize = reader.ReadUInt32();

        for (uint i = 0; i < spawnIndicesSize; ++i)
        {
            var spawnId = reader.ReadUInt32();
            _spawnIndices[spawnId] = i;
        }

        return LoadResult.Success;
	}

	public void UnloadMap(VMapManager vm)
	{
		lock (_loadedSpawns)
		{
			foreach (var id in _loadedSpawns)
			{
				for (uint refCount = 0; refCount < id.Key; ++refCount)
					vm.ReleaseModelInstance(_treeValues[id.Key].Name);

				_treeValues[id.Key].SetUnloaded();
			}

			_loadedSpawns.Clear();
			_loadedTiles.Clear();
		}
	}

	public LoadResult LoadMapTile(int tileX, int tileY, VMapManager vm)
	{
		lock (_loadedSpawns)
		{
			if (_treeValues == null)
			{
				Log.Logger.Error("StaticMapTree.LoadMapTile() : tree has not been initialized [{0}, {1}]", tileX, tileY);

				return LoadResult.ReadFromFileFailed;
			}

			var result = LoadResult.FileNotFound;

			var fileResult = OpenMapTileFile(vm.VMapPath, _mapId, tileX, tileY, vm);

			if (fileResult.File != null)
			{
				result = LoadResult.Success;
				using BinaryReader reader = new(fileResult.File);

				if (reader.ReadStringFromChars(8) != MapConst.VMapMagic)
					result = LoadResult.VersionMismatch;

				if (result == LoadResult.Success)
				{
					var numSpawns = reader.ReadUInt32();

					for (uint i = 0; i < numSpawns && result == LoadResult.Success; ++i)
						// read model spawns
						if (ModelSpawn.ReadFromFile(reader, out var spawn))
						{
							// acquire model instance
							var model = vm.AcquireModelInstance(spawn.Name, spawn.Flags);

							if (model == null)
								Log.Logger.Error("StaticMapTree.LoadMapTile() : could not acquire WorldModel [{0}, {1}]", tileX, tileY);

							// update tree
							if (_spawnIndices.TryGetValue(spawn.Id, out var referencedVal))
							{
								if (!_loadedSpawns.ContainsKey(referencedVal))
								{
									if (referencedVal >= _nTreeValues)
									{
										Log.Logger.Error("StaticMapTree.LoadMapTile() : invalid tree element ({0}/{1}) referenced in tile {2}", referencedVal, _nTreeValues, fileResult.Name);

										continue;
									}

									_treeValues[referencedVal] = new ModelInstance(spawn, model);
									_loadedSpawns.TryAdd(referencedVal, 1);
								}
								else
								{
									++_loadedSpawns[referencedVal];
								}
							}
							else if (_mapId == fileResult.UsedMapId)
							{
								// unknown parent spawn might appear in because it overlaps multiple tiles
								// in case the original tile is swapped but its neighbour is now (adding this spawn)
								// we want to not mark it as loading error and just skip that model
								Log.Logger.Error($"StaticMapTree.LoadMapTile() : invalid tree element (spawn {spawn.Id}) referenced in tile fileResult.Name{fileResult.Name} by map {_mapId}");
								result = LoadResult.ReadFromFileFailed;
							}
						}
						else
						{
							Log.Logger.Error($"StaticMapTree.LoadMapTile() : cannot read model from file (spawn index {i}) referenced in tile {fileResult.Name} by map {_mapId}");
							result = LoadResult.ReadFromFileFailed;
						}
				}

				_loadedTiles[PackTileID(tileX, tileY)] = true;
			}
			else
			{
				_loadedTiles[PackTileID(tileX, tileY)] = false;
			}

			return result;
		}
	}

	public void UnloadMapTile(int tileX, int tileY, VMapManager vm)
	{
		lock (_loadedTiles)
		{
			var tileID = PackTileID(tileX, tileY);

			if (!_loadedTiles.ContainsKey(tileID))
				return;

			if (_loadedTiles[tileID]) // file associated with tile
			{
				var fileResult = OpenMapTileFile(vm.VMapPath, _mapId, tileX, tileY, vm);

				if (fileResult.File != null)
                {
                    using BinaryReader reader = new(fileResult.File);
                    var result = reader.ReadStringFromChars(8) == MapConst.VMapMagic;

                    var numSpawns = reader.ReadUInt32();

                    for (uint i = 0; i < numSpawns && result; ++i)
                    {
                        // read model spawns
                        result = ModelSpawn.ReadFromFile(reader, out var spawn);

                        if (result)
                        {
                            // release model instance
                            vm.ReleaseModelInstance(spawn.Name);

                            // update tree
                            if (_spawnIndices.TryGetValue(spawn.Id, out var referencedNode))
                            {
                                if (_loadedSpawns.ContainsKey(referencedNode) && --_loadedSpawns[referencedNode] == 0)
                                {
                                    _treeValues[referencedNode].SetUnloaded();
                                    _loadedSpawns.TryRemove(referencedNode, out _);
                                }
                            }
                            else if (_mapId == fileResult.UsedMapId) // logic documented in StaticMapTree::LoadMapTile
                            {
                                result = false;
                            }
                        }
                    }
                }
            }

			_loadedTiles.TryRemove(tileID, out _);
		}
	}

	public static LoadResult CanLoadMap(string vmapPath, uint mapID, int tileX, int tileY, VMapManager vm)
	{
		var fullname = vmapPath + VMapManager.GetMapFileName(mapID);

		if (!File.Exists(fullname))
			return LoadResult.FileNotFound;

		using (BinaryReader reader = new(new FileStream(fullname, FileMode.Open, FileAccess.Read)))
		{
			if (reader.ReadStringFromChars(8) != MapConst.VMapMagic)
				return LoadResult.VersionMismatch;
		}

		var stream = OpenMapTileFile(vmapPath, mapID, tileX, tileY, vm).File;

		if (stream == null)
			return LoadResult.FileNotFound;

		using (BinaryReader reader = new(stream))
		{
			if (reader.ReadStringFromChars(8) != MapConst.VMapMagic)
				return LoadResult.VersionMismatch;
		}

		return LoadResult.Success;
	}

	public static string GetTileFileName(uint mapID, int tileX, int tileY)
	{
		return $"{mapID:D4}_{tileY:D2}_{tileX:D2}.vmtile";
	}

	public bool GetAreaInfo(ref Vector3 pos, out uint flags, out int adtId, out int rootId, out int groupId)
	{
		flags = 0;
		adtId = 0;
		rootId = 0;
		groupId = 0;

		AreaInfoCallback intersectionCallBack = new(_treeValues);
		_tree.IntersectPoint(pos, intersectionCallBack);

		if (intersectionCallBack.AInfo.Result)
		{
			flags = intersectionCallBack.AInfo.Flags;
			adtId = intersectionCallBack.AInfo.AdtId;
			rootId = intersectionCallBack.AInfo.RootId;
			groupId = intersectionCallBack.AInfo.GroupId;
			pos.Z = intersectionCallBack.AInfo.GroundZ;

			return true;
		}

		return false;
	}

	public bool GetLocationInfo(Vector3 pos, LocationInfo info)
	{
		LocationInfoCallback intersectionCallBack = new(_treeValues, info);
		_tree.IntersectPoint(pos, intersectionCallBack);

		return intersectionCallBack.Result;
	}

	public float GetHeight(Vector3 pPos, float maxSearchDist)
	{
		var height = float.PositiveInfinity;
		Vector3 dir = new(0, 0, -1);
		Ray ray = new(pPos, dir); // direction with length of 1
		var maxDist = maxSearchDist;

		if (GetIntersectionTime(ray, ref maxDist, false, ModelIgnoreFlags.Nothing))
			height = pPos.Z - maxDist;

		return height;
	}

	public bool GetObjectHitPos(Vector3 pPos1, Vector3 pPos2, out Vector3 pResultHitPos, float pModifyDist)
	{
		bool result;
		var maxDist = (pPos2 - pPos1).Length();

		// prevent NaN values which can cause BIH intersection to enter infinite loop
		if (maxDist < 1e-10f)
		{
			pResultHitPos = pPos2;

			return false;
		}

		var dir = (pPos2 - pPos1) / maxDist; // direction with length of 1
		Ray ray = new(pPos1, dir);
		var dist = maxDist;

		if (GetIntersectionTime(ray, ref dist, false, ModelIgnoreFlags.Nothing))
		{
			pResultHitPos = pPos1 + dir * dist;

			if (pModifyDist < 0)
			{
				if ((pResultHitPos - pPos1).Length() > -pModifyDist)
					pResultHitPos += dir * pModifyDist;
				else
					pResultHitPos = pPos1;
			}
			else
			{
				pResultHitPos += dir * pModifyDist;
			}

			result = true;
		}
		else
		{
			pResultHitPos = pPos2;
			result = false;
		}

		return result;
	}

	public bool IsInLineOfSight(Vector3 pos1, Vector3 pos2, ModelIgnoreFlags ignoreFlags)
	{
		var maxDist = (pos2 - pos1).Length();

		// return false if distance is over max float, in case of cheater teleporting to the end of the universe
		if (maxDist is float.MaxValue or float.PositiveInfinity)
			return false;

		// prevent NaN values which can cause BIH intersection to enter infinite loop
		if (maxDist < 1e-10f)
			return true;

		// direction with length of 1
		Ray ray = new(pos1, (pos2 - pos1) / maxDist);

		if (GetIntersectionTime(ray, ref maxDist, true, ignoreFlags))
			return false;

		return true;
	}

	public int NumLoadedTiles()
    {
        lock (_loadedTiles)
            return _loadedTiles.Count;
    }

	static uint PackTileID(int tileX, int tileY)
	{
		return (uint)(tileX << 16 | tileY);
	}


	static TileFileOpenResult OpenMapTileFile(string vmapPath, uint mapID, int tileX, int tileY, VMapManager vm)
	{
		TileFileOpenResult result = new()
		{
			Name = vmapPath + GetTileFileName(mapID, tileX, tileY)
		};

		if (File.Exists(result.Name))
		{
			result.UsedMapId = mapID;
			result.File = new FileStream(result.Name, FileMode.Open, FileAccess.Read);

			return result;
		}

		var parentMapId = vm.GetParentMapId(mapID);

		while (parentMapId != -1)
		{
			result.Name = vmapPath + GetTileFileName((uint)parentMapId, tileX, tileY);

			if (File.Exists(result.Name))
			{
				result.File = new FileStream(result.Name, FileMode.Open, FileAccess.Read);
				result.UsedMapId = (uint)parentMapId;

				return result;
			}

			parentMapId = vm.GetParentMapId((uint)parentMapId);
		}

		return result;
	}

	bool GetIntersectionTime(Ray pRay, ref float pMaxDist, bool pStopAtFirstHit, ModelIgnoreFlags ignoreFlags)
	{
		var distance = pMaxDist;
		MapRayCallback intersectionCallBack = new(_treeValues, ignoreFlags);
		_tree.IntersectRay(pRay, intersectionCallBack, ref distance, pStopAtFirstHit);

		if (intersectionCallBack.DidHit())
			pMaxDist = distance;

		return intersectionCallBack.DidHit();
	}
}