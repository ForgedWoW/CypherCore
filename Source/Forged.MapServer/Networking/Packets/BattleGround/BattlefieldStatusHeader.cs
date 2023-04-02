// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Networking.Packets.LFG;

namespace Forged.MapServer.Networking.Packets.BattleGround;

public class BattlefieldStatusHeader
{
    public uint InstanceID;
    public List<ulong> QueueID = new();
    public byte RangeMax;
    public byte RangeMin;
    public bool RegisteredMatch;
    public byte TeamSize;
    public RideTicket Ticket;
    public bool TournamentRules;

    public void Write(WorldPacket data)
    {
        Ticket.Write(data);
        data.WriteInt32(QueueID.Count);
        data.WriteUInt8(RangeMin);
        data.WriteUInt8(RangeMax);
        data.WriteUInt8(TeamSize);
        data.WriteUInt32(InstanceID);

        foreach (var queueID in QueueID)
            data.WriteUInt64(queueID);

        data.WriteBit(RegisteredMatch);
        data.WriteBit(TournamentRules);
        data.FlushBits();
    }
}