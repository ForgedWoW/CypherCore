// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Movement;

public class WaypointPath
{
    public uint ID { get; set; }
    public List<WaypointNode> Nodes { get; set; } = new();

    public WaypointPath() { }

    public WaypointPath(uint id, List<WaypointNode> nodes)
    {
        ID = id;
        Nodes = nodes;
    }
}