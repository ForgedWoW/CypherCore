// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Networking.Packets.Calendar;

public class CalendarInviteRemovedAlert : ServerPacket
{
	public ulong EventID;
	public long Date;
	public CalendarFlags Flags;
	public CalendarInviteStatus Status;
	public CalendarInviteRemovedAlert() : base(ServerOpcodes.CalendarInviteRemovedAlert) { }

	public override void Write()
	{
		_worldPacket.WriteUInt64(EventID);
		_worldPacket.WritePackedTime(Date);
		_worldPacket.WriteUInt32((uint)Flags);
		_worldPacket.WriteUInt8((byte)Status);
	}
}
