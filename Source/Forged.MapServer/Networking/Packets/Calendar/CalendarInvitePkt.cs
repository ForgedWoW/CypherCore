// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Calendar;

internal class CalendarInvitePkt : ClientPacket
{
    public ulong ClubID;
    public bool Creating = true;
    public ulong EventID;
    public bool IsSignUp;
    public ulong ModeratorID;
    public string Name;
    public CalendarInvitePkt(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        EventID = WorldPacket.ReadUInt64();
        ModeratorID = WorldPacket.ReadUInt64();
        ClubID = WorldPacket.ReadUInt64();

        var nameLen = WorldPacket.ReadBits<ushort>(9);
        Creating = WorldPacket.HasBit();
        IsSignUp = WorldPacket.HasBit();

        Name = WorldPacket.ReadString(nameLen);
    }
}