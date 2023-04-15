// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Maps;

public class MMapData
{
    public Dictionary<uint, ulong> LoadedTileRefs = new();
    public Detour.dtNavMesh NavMesh;
    public Dictionary<uint, Detour.dtNavMeshQuery> NavMeshQueries = new(); // instanceId to query
                                                                           // maps [map grid coords] to [dtTile]

    public MMapData(Detour.dtNavMesh mesh)
    {
        NavMesh = mesh;
    }
}