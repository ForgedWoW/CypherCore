// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Calendar;

internal class CalendarRemoveEvent : ClientPacket
{
    public ulong ClubID;
    public ulong EventID;
    public uint Flags;
    public ulong ModeratorID;
    public CalendarRemoveEvent(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        EventID = _worldPacket.ReadUInt64();
        ModeratorID = _worldPacket.ReadUInt64();
        ClubID = _worldPacket.ReadUInt64();
        Flags = _worldPacket.ReadUInt32();
    }
}