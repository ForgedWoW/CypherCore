// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.LFG;

internal class LFGJoinResult : ServerPacket
{
    public List<LFGBlackListPkt> BlackList = new();
    public List<string> BlackListNames = new();
    public byte Result;
    public byte ResultDetail;
    public RideTicket Ticket = new();
    public LFGJoinResult() : base(ServerOpcodes.LfgJoinResult) { }

    public override void Write()
    {
        Ticket.Write(WorldPacket);

        WorldPacket.WriteUInt8(Result);
        WorldPacket.WriteUInt8(ResultDetail);
        WorldPacket.WriteInt32(BlackList.Count);
        WorldPacket.WriteInt32(BlackListNames.Count);

        foreach (var blackList in BlackList)
            blackList.Write(WorldPacket);

        foreach (var str in BlackListNames)
            WorldPacket.WriteBits(str.GetByteCount() + 1, 24);

        foreach (var str in BlackListNames)
            if (!str.IsEmpty())
                WorldPacket.WriteCString(str);
    }
}