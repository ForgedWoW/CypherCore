// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

struct CalendarSendCalendarEventInfo
{
	public void Write(WorldPacket data)
	{
		data.WriteUInt64(EventID);
		data.WriteUInt8((byte)EventType);
		data.WritePackedTime(Date);
		data.WriteUInt32((uint)Flags);
		data.WriteInt32(TextureID);
		data.WriteUInt64(EventClubID);
		data.WritePackedGuid(OwnerGuid);

		data.WriteBits(EventName.GetByteCount(), 8);
		data.FlushBits();
		data.WriteString(EventName);
	}

	public ulong EventID;
	public string EventName;
	public CalendarEventType EventType;
	public long Date;
	public CalendarFlags Flags;
	public int TextureID;
	public ulong EventClubID;
	public ObjectGuid OwnerGuid;
}