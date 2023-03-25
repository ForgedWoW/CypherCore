// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Framework.IO;

namespace Forged.MapServer.Networking.Packets.Battlenet;

class ChangeRealmTicketResponse : ServerPacket
{
	public uint Token;
	public bool Allow = true;
	public ByteBuffer Ticket;
	public ChangeRealmTicketResponse() : base(ServerOpcodes.ChangeRealmTicketResponse) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(Token);
		_worldPacket.WriteBit(Allow);
		_worldPacket.WriteUInt32(Ticket.GetSize());
		_worldPacket.WriteBytes(Ticket);
	}
}