// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Calendar;

internal class CalendarSendCalendar : ServerPacket
{
    public List<CalendarSendCalendarEventInfo> Events = new();
    public List<CalendarSendCalendarInviteInfo> Invites = new();
    public List<CalendarSendCalendarRaidLockoutInfo> RaidLockouts = new();
    public long ServerTime;
    public CalendarSendCalendar() : base(ServerOpcodes.CalendarSendCalendar) { }

    public override void Write()
    {
        WorldPacket.WritePackedTime(ServerTime);
        WorldPacket.WriteInt32(Invites.Count);
        WorldPacket.WriteInt32(Events.Count);
        WorldPacket.WriteInt32(RaidLockouts.Count);

        foreach (var invite in Invites)
            invite.Write(WorldPacket);

        foreach (var lockout in RaidLockouts)
            lockout.Write(WorldPacket);

        foreach (var Event in Events)
            Event.Write(WorldPacket);
    }
}