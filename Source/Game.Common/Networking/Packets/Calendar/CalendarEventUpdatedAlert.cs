// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Game.Networking.Packets;

public class CalendarEventUpdatedAlert : ServerPacket
{
	public ulong EventID;
	public long Date;
	public CalendarFlags Flags;
	public long LockDate;
	public long OriginalDate;
	public int TextureID;
	public CalendarEventType EventType;
	public bool ClearPending;
	public string Description;
	public string EventName;
	public CalendarEventUpdatedAlert() : base(ServerOpcodes.CalendarEventUpdatedAlert) { }

	public override void Write()
	{
		_worldPacket.WriteUInt64(EventID);

		_worldPacket.WritePackedTime(OriginalDate);
		_worldPacket.WritePackedTime(Date);
		_worldPacket.WriteUInt32((uint)LockDate);
		_worldPacket.WriteUInt32((uint)Flags);
		_worldPacket.WriteInt32(TextureID);
		_worldPacket.WriteUInt8((byte)EventType);

		_worldPacket.WriteBits(EventName.GetByteCount(), 8);
		_worldPacket.WriteBits(Description.GetByteCount(), 11);
		_worldPacket.WriteBit(ClearPending);
		_worldPacket.FlushBits();

		_worldPacket.WriteString(EventName);
		_worldPacket.WriteString(Description);
	}
}