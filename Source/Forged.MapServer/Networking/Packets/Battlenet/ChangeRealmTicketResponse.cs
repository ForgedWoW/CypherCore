// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Framework.IO;

namespace Forged.MapServer.Networking.Packets.Battlenet;

internal class ChangeRealmTicketResponse : ServerPacket
{
    public bool Allow = true;
    public ByteBuffer Ticket;
    public uint Token;
    public ChangeRealmTicketResponse() : base(ServerOpcodes.ChangeRealmTicketResponse) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(Token);
        WorldPacket.WriteBit(Allow);
        WorldPacket.WriteUInt32(Ticket.GetSize());
        WorldPacket.WriteBytes(Ticket);
    }
}