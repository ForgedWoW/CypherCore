// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Calendar;

internal class CalendarEventSignUp : ClientPacket
{
    public ulong ClubID;
    public ulong EventID;
    public bool Tentative;
    public CalendarEventSignUp(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        EventID = WorldPacket.ReadUInt64();
        ClubID = WorldPacket.ReadUInt64();
        Tentative = WorldPacket.HasBit();
    }
}