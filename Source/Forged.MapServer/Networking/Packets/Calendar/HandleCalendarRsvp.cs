// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Calendar;

internal class HandleCalendarRsvp : ClientPacket
{
	public ulong InviteID;
	public ulong EventID;
	public CalendarInviteStatus Status;
	public HandleCalendarRsvp(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		EventID = _worldPacket.ReadUInt64();
		InviteID = _worldPacket.ReadUInt64();
		Status = (CalendarInviteStatus)_worldPacket.ReadUInt8();
	}
}