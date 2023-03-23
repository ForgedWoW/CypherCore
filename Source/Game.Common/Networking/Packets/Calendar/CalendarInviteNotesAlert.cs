// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Calendar;

public class CalendarInviteNotesAlert : ServerPacket
{
	public ulong EventID;
	public string Notes;

	public CalendarInviteNotesAlert(ulong eventID, string notes) : base(ServerOpcodes.CalendarInviteNotesAlert)
	{
		EventID = eventID;
		Notes = notes;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt64(EventID);

		_worldPacket.WriteBits(Notes.GetByteCount(), 8);
		_worldPacket.FlushBits();
		_worldPacket.WriteString(Notes);
	}
}
