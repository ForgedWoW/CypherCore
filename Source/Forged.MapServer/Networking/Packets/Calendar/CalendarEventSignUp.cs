// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Calendar;

internal class CalendarEventSignUp : ClientPacket
{
    public bool Tentative;
    public ulong EventID;
    public ulong ClubID;
    public CalendarEventSignUp(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        EventID = _worldPacket.ReadUInt64();
        ClubID = _worldPacket.ReadUInt64();
        Tentative = _worldPacket.HasBit();
    }
}