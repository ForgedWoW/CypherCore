// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Calendar;

internal class CalendarCopyEvent : ClientPacket
{
    public ulong ModeratorID;
    public ulong EventID;
    public ulong EventClubID;
    public long Date;
    public CalendarCopyEvent(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        EventID = _worldPacket.ReadUInt64();
        ModeratorID = _worldPacket.ReadUInt64();
        EventClubID = _worldPacket.ReadUInt64();
        Date = _worldPacket.ReadPackedTime();
    }
}