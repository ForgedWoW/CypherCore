// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;

namespace Forged.MapServer.Networking.Packets.Ticket;

public struct SupportTicketHeader
{
    public void Read(WorldPacket packet)
    {
        MapID = packet.ReadUInt32();
        Position = packet.ReadVector3();
        Facing = packet.ReadFloat();
        Program = packet.ReadInt32();
    }

    public uint MapID;
    public Vector3 Position;
    public float Facing;
    public int Program;
}