// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Calendar;

internal class CalendarInvitePkt : ClientPacket
{
    public ulong ModeratorID;
    public bool IsSignUp;
    public bool Creating = true;
    public ulong EventID;
    public ulong ClubID;
    public string Name;
    public CalendarInvitePkt(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        EventID = _worldPacket.ReadUInt64();
        ModeratorID = _worldPacket.ReadUInt64();
        ClubID = _worldPacket.ReadUInt64();

        var nameLen = _worldPacket.ReadBits<ushort>(9);
        Creating = _worldPacket.HasBit();
        IsSignUp = _worldPacket.HasBit();

        Name = _worldPacket.ReadString(nameLen);
    }
}