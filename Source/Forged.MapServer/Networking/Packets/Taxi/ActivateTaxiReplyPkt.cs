// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Taxi;

internal class ActivateTaxiReplyPkt : ServerPacket
{
    public ActivateTaxiReply Reply;
    public ActivateTaxiReplyPkt() : base(ServerOpcodes.ActivateTaxiReply) { }

    public override void Write()
    {
        WorldPacket.WriteBits(Reply, 4);
        WorldPacket.FlushBits();
    }
}