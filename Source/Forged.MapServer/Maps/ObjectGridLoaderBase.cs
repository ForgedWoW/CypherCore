// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Phasing;

namespace Forged.MapServer.Maps;

internal class ObjectGridLoaderBase
{
    public ObjectGridLoaderBase(Grid grid, Map map, Cell cell)
    {
        Cell = new Cell(cell, map.GridDefines);
        Grid = grid;
        Map = map;
    }

    internal uint AreaTriggers { get; set; }
    internal Cell Cell { get; set; }
    internal uint Corpses { get; set; }
    internal uint Creatures { get; set; }
    internal uint GameObjects { get; set; }
    internal Grid Grid { get; set; }
    internal Map Map { get; set; }
    public uint GetLoadedAreaTriggers()
    {
        return AreaTriggers;
    }

    public uint GetLoadedCorpses()
    {
        return Corpses;
    }

    public uint GetLoadedCreatures()
    {
        return Creatures;
    }

    public uint GetLoadedGameObjects()
    {
        return GameObjects;
    }
    internal uint LoadHelper<T>(SortedSet<ulong> guidSet, CellCoord cell, Map map, uint phaseId = 0, ObjectGuid? phaseOwner = null) where T : WorldObject
    {
        var count = 0u;
        foreach (var guid in guidSet)
        {
            // Don't spawn at all if there's a respawn timer
            if (!map.ShouldBeSpawnedOnGridLoad<T>(guid))
                continue;

            var obj = Map.ClassFactory.Resolve<T>();

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

            count += AddObjectHelper(cell, map, obj);
        }

        return count;
    }

    private uint AddObjectHelper<T>(CellCoord cellCoord, Map map, T obj) where T : WorldObject
    {
        var cell = new Cell(cellCoord, Map.GridDefines);
        map.AddToGrid(obj, cell);
        obj.AddToWorld();

        if (!obj.IsCreature)
            return 1;

        if (obj.IsActive)
            map.AddToActive(obj);

        return  1;
    }
}