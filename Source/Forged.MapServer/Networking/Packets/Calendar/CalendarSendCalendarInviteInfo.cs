// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Calendar;

internal struct CalendarSendCalendarInviteInfo
{
	public void Write(WorldPacket data)
	{
		data.WriteUInt64(EventID);
		data.WriteUInt64(InviteID);
		data.WriteUInt8((byte)Status);
		data.WriteUInt8((byte)Moderator);
		data.WriteUInt8(InviteType);
		data.WritePackedGuid(InviterGuid);
	}

	public ulong EventID;
	public ulong InviteID;
	public ObjectGuid InviterGuid;
	public CalendarInviteStatus Status;
	public CalendarModerationRank Moderator;
	public byte InviteType;
}