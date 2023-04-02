// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Calendar;

internal class CalendarCommunityInviteRequest : ClientPacket
{
    public ulong ClubId;
    public byte MaxLevel = 100;
    public byte MaxRankOrder;
    public byte MinLevel = 1;
    public CalendarCommunityInviteRequest(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        ClubId = _worldPacket.ReadUInt64();
        MinLevel = _worldPacket.ReadUInt8();
        MaxLevel = _worldPacket.ReadUInt8();
        MaxRankOrder = _worldPacket.ReadUInt8();
    }
}