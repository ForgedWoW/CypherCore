// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Movement;

public class WaypointNode
{
    public uint Delay { get; set; }
    public byte EventChance { get; set; }
    public uint EventId { get; set; }
    public uint ID { get; set; }
    public WaypointMoveType MoveType { get; set; }
    public float? Orientation { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public WaypointNode()
    {
        MoveType = WaypointMoveType.Run;
    }

    public WaypointNode(uint id, float x, float y, float z, float? orientation = null, uint delay = 0)
    {
        ID = id;
        X = x;
        Y = y;
        Z = z;
        Orientation = orientation;
        Delay = delay;
        EventId = 0;
        MoveType = WaypointMoveType.Walk;
        EventChance = 100;
    }
}