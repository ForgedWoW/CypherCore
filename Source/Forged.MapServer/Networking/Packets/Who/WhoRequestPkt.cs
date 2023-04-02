// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Networking.Packets.Who;

public class WhoRequestPkt : ClientPacket
{
    public List<int> Areas = new();
    public WhoRequest Request = new();
    public uint RequestID;
    public WhoRequestPkt(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        var areasCount = WorldPacket.ReadBits<uint>(4);

        Request.Read(WorldPacket);
        RequestID = WorldPacket.ReadUInt32();

        for (var i = 0; i < areasCount; ++i)
            Areas.Add(WorldPacket.ReadInt32());
    }
}