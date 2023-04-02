// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;

namespace Forged.MapServer.Networking.Packets.Movement;

public class MovementMonsterSpline
{
    public bool CrzTeleport;
    public Vector3 Destination;
    public uint Id;
    public MovementSpline Move;
    public byte StopDistanceTolerance; // Determines how far from spline destination the mover is allowed to stop in place 0, 0, 3.0, 2.76, numeric_limits<float>::max, 1.1, float(INT_MAX); default before this field existed was distance 3.0 (index 2)
    public MovementMonsterSpline()
    {
        Move = new MovementSpline();
    }

    public void Write(WorldPacket data)
    {
        data.WriteUInt32(Id);
        data.WriteVector3(Destination);
        data.WriteBit(CrzTeleport);
        data.WriteBits(StopDistanceTolerance, 3);

        Move.Write(data);
    }
}