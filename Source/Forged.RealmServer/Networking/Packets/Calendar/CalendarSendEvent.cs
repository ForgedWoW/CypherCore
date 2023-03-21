// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

class CalendarSendEvent : ServerPacket
{
	public ObjectGuid OwnerGuid;
	public ObjectGuid EventGuildID;
	public ulong EventID;
	public long Date;
	public long LockDate;
	public CalendarFlags Flags;
	public int TextureID;
	public CalendarEventType GetEventType;
	public CalendarSendEventType EventType;
	public string Description;
	public string EventName;
	public List<CalendarEventInviteInfo> Invites = new();
	public CalendarSendEvent() : base(ServerOpcodes.CalendarSendEvent) { }

	public override void Write()
	{
		_worldPacket.WriteUInt8((byte)EventType);
		_worldPacket.WritePackedGuid(OwnerGuid);
		_worldPacket.WriteUInt64(EventID);
		_worldPacket.WriteUInt8((byte)GetEventType);
		_worldPacket.WriteInt32(TextureID);
		_worldPacket.WriteUInt32((uint)Flags);
		_worldPacket.WritePackedTime(Date);
		_worldPacket.WriteUInt32((uint)LockDate);
		_worldPacket.WritePackedGuid(EventGuildID);
		_worldPacket.WriteInt32(Invites.Count);

		_worldPacket.WriteBits(EventName.GetByteCount(), 8);
		_worldPacket.WriteBits(Description.GetByteCount(), 11);
		_worldPacket.FlushBits();

		foreach (var invite in Invites)
			invite.Write(_worldPacket);

		_worldPacket.WriteString(EventName);
		_worldPacket.WriteString(Description);
	}
}