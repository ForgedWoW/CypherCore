// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Calendar;

internal class CalendarCopyEvent : ClientPacket
{
    public long Date;
    public ulong EventClubID;
    public ulong EventID;
    public ulong ModeratorID;
    public CalendarCopyEvent(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        EventID = WorldPacket.ReadUInt64();
        ModeratorID = WorldPacket.ReadUInt64();
        EventClubID = WorldPacket.ReadUInt64();
        Date = WorldPacket.ReadPackedTime();
    }
}