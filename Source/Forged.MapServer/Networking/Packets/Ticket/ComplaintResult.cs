// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Ticket;

public class ComplaintResult : ServerPacket
{
    public SupportSpamType ComplaintType;
    public byte Result;
    public ComplaintResult() : base(ServerOpcodes.ComplaintResult) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32((uint)ComplaintType);
        WorldPacket.WriteUInt8(Result);
    }
}