// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Maps;

public class MMapData
{
    public Dictionary<uint, ulong> LoadedTileRefs { get; set; } = new();
    public Detour.dtNavMesh NavMesh { get; set; }
    public Dictionary<uint, Detour.dtNavMeshQuery> NavMeshQueries { get; set; } = new(); // instanceId to query
                                                                           // maps [map grid coords] to [dtTile]

    public MMapData(Detour.dtNavMesh mesh)
    {
        NavMesh = mesh;
    }
}