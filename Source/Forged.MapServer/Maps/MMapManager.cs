// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Maps;

public class MMapManager
{
    private const string MapFileNameFormat = "{0}/mmaps/{1:D4}.mmap";
    private const string TileFileNameFormat = "{0}/mmaps/{1:D4}{2:D2}{3:D2}.mmtile";

    private readonly Dictionary<uint, MMapData> _loadedMMaps = new();
    private readonly Dictionary<uint, uint> _parentMapData = new();
    private uint _loadedTiles;

    public int GetLoadedMapsCount()
    {
        return _loadedMMaps.Count;
    }

    public uint GetLoadedTilesCount()
    {
        return _loadedTiles;
    }

    public Detour.dtNavMesh GetNavMesh(uint mapId)
    {
        if (!_loadedMMaps.TryGetValue(mapId, out var mmap))
            return null;

        return mmap.navMesh;
    }

    public Detour.dtNavMeshQuery GetNavMeshQuery(uint mapId, uint instanceId)
    {
        if (!_loadedMMaps.TryGetValue(mapId, out var mmap))
            return null;

        return mmap.navMeshQueries.LookupByKey(instanceId);
    }

    public void Initialize(MultiMap<uint, uint> mapData)
    {
        foreach (var pair in mapData.KeyValueList)
            _parentMapData[pair.Value] = pair.Key;
    }

    public bool LoadMap(string basePath, uint mapId, int x, int y)
    {
        // make sure the mmap is loaded and ready to load tiles
        if (!LoadMapData(basePath, mapId))
            return false;

        // get this mmap data
        var mmap = _loadedMMaps[mapId];

        // check if we already have this tile loaded
        var packedGridPos = PackTileID(x, y);

        if (mmap.loadedTileRefs.ContainsKey(packedGridPos))
            return false;

        // load this tile . mmaps/MMMXXYY.mmtile
        var fileName = string.Format(TileFileNameFormat, basePath, mapId, x, y);

        if (!File.Exists(fileName))
            if (_parentMapData.ContainsKey(mapId))
                fileName = string.Format(TileFileNameFormat, basePath, _parentMapData[mapId], x, y);

        if (!File.Exists(fileName))
        {
            Log.Logger.Debug("MMAP:loadMap: Could not open mmtile file '{0}'", fileName);

            return false;
        }

        using BinaryReader reader = new(new FileStream(fileName, FileMode.Open, FileAccess.Read));
        var fileHeader = reader.Read<MmapTileHeader>();

        if (fileHeader.mmapMagic != MapConst.mmapMagic)
        {
            Log.Logger.Error("MMAP:loadMap: Bad header in mmap {0:D4}{1:D2}{2:D2}.mmtile", mapId, x, y);

            return false;
        }

        if (fileHeader.mmapVersion != MapConst.mmapVersion)
        {
            Log.Logger.Error("MMAP:loadMap: {0:D4}{1:D2}{2:D2}.mmtile was built with generator v{3}, expected v{4}",
                             mapId,
                             x,
                             y,
                             fileHeader.mmapVersion,
                             MapConst.mmapVersion);

            return false;
        }

        var bytes = reader.ReadBytes((int)fileHeader.size);
        Detour.dtRawTileData data = new();
        data.FromBytes(bytes, 0);

        ulong tileRef = 0;

        // memory allocated for data is now managed by detour, and will be deallocated when the tile is removed
        if (Detour.dtStatusSucceed(mmap.navMesh.addTile(data, 1, 0, ref tileRef)))
        {
            mmap.loadedTileRefs.Add(packedGridPos, tileRef);
            ++_loadedTiles;
            Log.Logger.Information("MMAP:loadMap: Loaded mmtile {0:D4}[{1:D2}, {2:D2}]", mapId, x, y);

            return true;
        }

        Log.Logger.Error("MMAP:loadMap: Could not load {0:D4}{1:D2}{2:D2}.mmtile into navmesh", mapId, x, y);

        return false;
    }

    public bool LoadMapInstance(string basePath, uint mapId, uint instanceId)
    {
        if (!LoadMapData(basePath, mapId))
            return false;

        var mmap = _loadedMMaps[mapId];

        if (mmap.navMeshQueries.ContainsKey(instanceId))
            return true;

        // allocate mesh query
        Detour.dtNavMeshQuery query = new();

        if (Detour.dtStatusFailed(query.init(mmap.navMesh, 1024)))
        {
            Log.Logger.Error("MMAP.GetNavMeshQuery: Failed to initialize dtNavMeshQuery for mapId {0:D4} instanceId {1}", mapId, instanceId);

            return false;
        }

        Log.Logger.Debug("MMAP.GetNavMeshQuery: created dtNavMeshQuery for mapId {0:D4} instanceId {1}", mapId, instanceId);
        mmap.navMeshQueries.Add(instanceId, query);

        return true;
    }

    public bool UnloadMap(uint mapId, int x, int y)
    {
        // check if we have this map loaded
        if (!_loadedMMaps.TryGetValue(mapId, out var mmap))
            return false;

        // check if we have this tile loaded
        var packedGridPos = PackTileID(x, y);

        if (!mmap.loadedTileRefs.TryGetValue(packedGridPos, out var tileRef))
            return false;

        // unload, and mark as non loaded
        if (!Detour.dtStatusFailed(mmap.navMesh.removeTile(tileRef, out _)))
        {
            mmap.loadedTileRefs.Remove(packedGridPos);
            --_loadedTiles;
            Log.Logger.Information("MMAP:unloadMap: Unloaded mmtile {0:D4}[{1:D2}, {2:D2}] from {3:D4}", mapId, x, y, mapId);

            return true;
        }

        return false;
    }

    public bool UnloadMap(uint mapId)
    {
        if (!_loadedMMaps.ContainsKey(mapId))
        {
            // file may not exist, therefore not loaded
            Log.Logger.Debug("MMAP:unloadMap: Asked to unload not loaded navmesh map {0:D4}", mapId);

            return false;
        }

        // unload all tiles from given map
        var mmap = _loadedMMaps.LookupByKey(mapId);

        foreach (var i in mmap.loadedTileRefs)
        {
            var x = i.Key >> 16;
            var y = i.Key & 0x0000FFFF;

            if (Detour.dtStatusFailed(mmap.navMesh.removeTile(i.Value, out _)))
            {
                Log.Logger.Error("MMAP:unloadMap: Could not unload {0:D4}{1:D2}{2:D2}.mmtile from navmesh", mapId, x, y);
            }
            else
            {
                --_loadedTiles;
                Log.Logger.Information("MMAP:unloadMap: Unloaded mmtile {0:D4} [{1:D2}, {2:D2}] from {3:D4}", mapId, x, y, mapId);
            }
        }

        _loadedMMaps.Remove(mapId);
        Log.Logger.Information("MMAP:unloadMap: Unloaded {0:D4}.mmap", mapId);

        return true;
    }

    public bool UnloadMapInstance(uint mapId, uint instanceId)
    {
        // check if we have this map loaded
        if (!_loadedMMaps.TryGetValue(mapId, out var mmap))
        {
            // file may not exist, therefore not loaded
            Log.Logger.Debug("MMAP:unloadMapInstance: Asked to unload not loaded navmesh map {0}", mapId);

            return false;
        }

        if (!mmap.navMeshQueries.ContainsKey(instanceId))
        {
            Log.Logger.Debug("MMAP:unloadMapInstance: Asked to unload not loaded dtNavMeshQuery mapId {0} instanceId {1}", mapId, instanceId);

            return false;
        }

        mmap.navMeshQueries.Remove(instanceId);
        Log.Logger.Information("MMAP:unloadMapInstance: Unloaded mapId {0} instanceId {1}", mapId, instanceId);

        return true;
    }
    private MMapData GetMMapData(uint mapId)
    {
        return _loadedMMaps.LookupByKey(mapId);
    }

    private bool LoadMapData(string basePath, uint mapId)
    {
        // we already have this map loaded?
        if (_loadedMMaps.TryGetValue(mapId, out var mmap) && mmap != null)
            return true;

        // load and init dtNavMesh - read parameters from file
        var filename = string.Format(MapFileNameFormat, basePath, mapId);

        if (!File.Exists(filename))
        {
            Log.Logger.Error("Could not open mmap file {0}", filename);

            return false;
        }

        using BinaryReader reader = new(new FileStream(filename, FileMode.Open, FileAccess.Read), Encoding.UTF8);

        Detour.dtNavMeshParams Params = new()
        {
            orig =
            {
                [0] = reader.ReadSingle(),
                [1] = reader.ReadSingle(),
                [2] = reader.ReadSingle()
            },
            tileWidth = reader.ReadSingle(),
            tileHeight = reader.ReadSingle(),
            maxTiles = reader.ReadInt32(),
            maxPolys = reader.ReadInt32()
        };

        Detour.dtNavMesh mesh = new();

        if (Detour.dtStatusFailed(mesh.init(Params)))
        {
            Log.Logger.Error("MMAP:loadMapData: Failed to initialize dtNavMesh for mmap {0:D4} from file {1}", mapId, filename);

            return false;
        }

        Log.Logger.Information("MMAP:loadMapData: Loaded {0:D4}.mmap", mapId);

        // store inside our map list
        _loadedMMaps[mapId] = new MMapData(mesh);

        return true;
    }

    private uint PackTileID(int x, int y)
    {
        return (uint)(x << 16 | y);
    }
}