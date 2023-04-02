// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Who;

public class WhoResponsePkt : ServerPacket
{
    public uint RequestID;
    public List<WhoEntry> Response = new();
    public WhoResponsePkt() : base(ServerOpcodes.Who) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(RequestID);
        WorldPacket.WriteBits(Response.Count, 6);
        WorldPacket.FlushBits();

        Response.ForEach(p => p.Write(WorldPacket));
    }
}