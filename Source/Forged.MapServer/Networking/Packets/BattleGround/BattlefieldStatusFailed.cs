// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.LFG;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattleGround;

public class BattlefieldStatusFailed : ServerPacket
{
    public ObjectGuid ClientID;
    public ulong QueueID;
    public int Reason;
    public RideTicket Ticket = new();
    public BattlefieldStatusFailed() : base(ServerOpcodes.BattlefieldStatusFailed) { }

    public override void Write()
    {
        Ticket.Write(WorldPacket);
        WorldPacket.WriteUInt64(QueueID);
        WorldPacket.WriteInt32(Reason);
        WorldPacket.WritePackedGuid(ClientID);
    }
}