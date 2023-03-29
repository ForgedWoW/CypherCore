// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Phasing;

namespace Forged.MapServer.Maps;

internal class ObjectGridLoaderBase
{
    internal Cell i_cell;
    internal Grid i_grid;
    internal Map i_map;
    internal uint i_gameObjects;
    internal uint i_creatures;
    internal uint i_corpses;
    internal uint i_areaTriggers;

    public ObjectGridLoaderBase(Grid grid, Map map, Cell cell)
    {
        i_cell = new Cell(cell);
        i_grid = grid;
        i_map = map;
    }

    public uint GetLoadedCreatures()
    {
        return i_creatures;
    }

    public uint GetLoadedGameObjects()
    {
        return i_gameObjects;
    }

    public uint GetLoadedCorpses()
    {
        return i_corpses;
    }

    public uint GetLoadedAreaTriggers()
    {
        return i_areaTriggers;
    }

    internal void LoadHelper<T>(SortedSet<ulong> guid_set, CellCoord cell, ref uint count, Map map, uint phaseId = 0, ObjectGuid? phaseOwner = null) where T : WorldObject, new()
    {
        foreach (var guid in guid_set)
        {
            // Don't spawn at all if there's a respawn timer
            if (!map.ShouldBeSpawnedOnGridLoad<T>(guid))
                continue;

            T obj = new();

            if (!obj.LoadFromDB(guid, map, false, phaseOwner.HasValue /*allowDuplicate*/))
            {
                obj.Dispose();

                continue;
            }

            if (phaseOwner.HasValue)
            {
                PhasingHandler.InitDbPersonalOwnership(obj.Location.PhaseShift, phaseOwner.Value);
                map.MultiPersonalPhaseTracker.RegisterTrackedObject(phaseId, phaseOwner.Value, obj);
            }

            AddObjectHelper(cell, ref count, map, obj);
        }
    }

    private void AddObjectHelper<T>(CellCoord cellCoord, ref uint count, Map map, T obj) where T : WorldObject
    {
        var cell = new Cell(cellCoord);
        map.AddToGrid(obj, cell);
        obj.AddToWorld();

        if (obj.IsCreature)
            if (obj.IsActive)
                map.AddToActive(obj);

        ++count;
    }
}