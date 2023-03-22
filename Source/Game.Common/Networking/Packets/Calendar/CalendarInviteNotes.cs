// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class CalendarInviteNotes : ServerPacket
{
	public ObjectGuid InviteGuid;
	public ulong EventID;
	public string Notes = "";
	public bool ClearPending;
	public CalendarInviteNotes() : base(ServerOpcodes.CalendarInviteNotes) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(InviteGuid);
		_worldPacket.WriteUInt64(EventID);

		_worldPacket.WriteBits(Notes.GetByteCount(), 8);
		_worldPacket.WriteBit(ClearPending);
		_worldPacket.FlushBits();
		_worldPacket.WriteString(Notes);
	}
}