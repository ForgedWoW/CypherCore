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
        Ticket.Write(_worldPacket);

        _worldPacket.WriteUInt8(Result);
        _worldPacket.WriteUInt8(ResultDetail);
        _worldPacket.WriteInt32(BlackList.Count);
        _worldPacket.WriteInt32(BlackListNames.Count);

        foreach (var blackList in BlackList)
            blackList.Write(_worldPacket);

        foreach (var str in BlackListNames)
            _worldPacket.WriteBits(str.GetByteCount() + 1, 24);

        foreach (var str in BlackListNames)
            if (!str.IsEmpty())
                _worldPacket.WriteCString(str);
    }
}