// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Calendar;

internal class CalendarGetEvent : ClientPacket
{
    public ulong EventID;
    public CalendarGetEvent(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        EventID = _worldPacket.ReadUInt64();
    }
}