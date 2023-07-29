// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Phasing;
using Framework.Constants;

namespace Forged.MapServer.Globals.Caching;

public class MapObjectCache
{
    private readonly GridDefines _gridDefines;
    private readonly PhasingHandler _phasingHandler;
    private readonly Dictionary<(uint mapId, Difficulty difficulty), Dictionary<uint, CellObjectGuids>> _mapObjectGuidsStore = new();
    private readonly Dictionary<(uint mapId, Difficulty diffuculty, uint phaseId), Dictionary<uint, CellObjectGuids>> _mapPersonalObjectGuidsStore = new();

    public MapObjectCache(GridDefines gridDefines, PhasingHandler phasingHandler)
    {
        _gridDefines = gridDefines;
        _phasingHandler = phasingHandler;
    }

    public CellObjectGuids GetCellObjectGuids(uint mapid, Difficulty difficulty, uint cellid)
    {
        var key = (mapid, difficulty);

        if (_mapObjectGuidsStore.TryGetValue(key, out var internDict) && internDict.TryGetValue(cellid, out var val))
            return val;

        return null;
    }

    public CellObjectGuids GetCellPersonalObjectGuids(uint mapid, Difficulty spawnMode, uint phaseId, uint cellID)
    {
        var guids = _mapPersonalObjectGuidsStore.LookupByKey((mapid, spawnMode, phaseId));

        return guids?.LookupByKey(cellID);
    }

    public Dictionary<uint, CellObjectGuids> GetMapObjectGuids(uint mapid, Difficulty difficulty)
    {
        var key = (mapid, difficulty);

        return _mapObjectGuidsStore.LookupByKey(key);
    }

    public bool HasPersonalSpawns(uint mapid, Difficulty spawnMode, uint phaseId)
    {
        return _mapPersonalObjectGuidsStore.ContainsKey((mapid, spawnMode, phaseId));
    }

    public void AddSpawnDataToGrid(SpawnData data)
    {
        var cellId = _gridDefines.ComputeCellCoord(data.SpawnPoint.X, data.SpawnPoint.Y).GetId();
        var isPersonalPhase = _phasingHandler.IsPersonalPhase(data.PhaseId);

        if (!isPersonalPhase)
            foreach (var difficulty in data.SpawnDifficulties)
            {
                var key = (data.MapId, difficulty);

                if (!_mapObjectGuidsStore.ContainsKey(key))
                    _mapObjectGuidsStore[key] = new Dictionary<uint, CellObjectGuids>();

                if (!_mapObjectGuidsStore[key].ContainsKey(cellId))
                    _mapObjectGuidsStore[key][cellId] = new CellObjectGuids();

                _mapObjectGuidsStore[key][cellId].AddSpawn(data);
            }
        else
            foreach (var difficulty in data.SpawnDifficulties)
            {
                var key = (data.MapId, difficulty, data.PhaseId);

                if (!_mapPersonalObjectGuidsStore.ContainsKey(key))
                    _mapPersonalObjectGuidsStore[key] = new Dictionary<uint, CellObjectGuids>();

                if (!_mapPersonalObjectGuidsStore[key].ContainsKey(cellId))
                    _mapPersonalObjectGuidsStore[key][cellId] = new CellObjectGuids();

                _mapPersonalObjectGuidsStore[key][cellId].AddSpawn(data);
            }
    }

    public void RemoveSpawnDataFromGrid(SpawnData data)
    {
        var cellId = _gridDefines.ComputeCellCoord(data.SpawnPoint.X, data.SpawnPoint.Y).GetId();
        var isPersonalPhase = _phasingHandler.IsPersonalPhase(data.PhaseId);

        if (!isPersonalPhase)
            foreach (var difficulty in data.SpawnDifficulties)
            {
                var key = (data.MapId, difficulty);

                if (!_mapObjectGuidsStore.ContainsKey(key) || !_mapObjectGuidsStore[key].ContainsKey(cellId))
                    continue;

                _mapObjectGuidsStore[(data.MapId, difficulty)][cellId].RemoveSpawn(data);
            }
        else
            foreach (var difficulty in data.SpawnDifficulties)
            {
                var key = (data.MapId, difficulty, data.PhaseId);

                if (!_mapPersonalObjectGuidsStore.ContainsKey(key) || !_mapPersonalObjectGuidsStore[key].ContainsKey(cellId))
                    continue;

                _mapPersonalObjectGuidsStore[key][cellId].RemoveSpawn(data);
            }
    }
}