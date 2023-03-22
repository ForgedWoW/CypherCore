// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Networking.Packets;

public class CalendarSendCalendar : ServerPacket
{
	public long ServerTime;
	public List<CalendarSendCalendarInviteInfo> Invites = new();
	public List<CalendarSendCalendarRaidLockoutInfo> RaidLockouts = new();
	public List<CalendarSendCalendarEventInfo> Events = new();
	public CalendarSendCalendar() : base(ServerOpcodes.CalendarSendCalendar) { }

	public override void Write()
	{
		_worldPacket.WritePackedTime(ServerTime);
		_worldPacket.WriteInt32(Invites.Count);
		_worldPacket.WriteInt32(Events.Count);
		_worldPacket.WriteInt32(RaidLockouts.Count);

		foreach (var invite in Invites)
			invite.Write(_worldPacket);

		foreach (var lockout in RaidLockouts)
			lockout.Write(_worldPacket);

		foreach (var Event in Events)
			Event.Write(_worldPacket);
	}
}