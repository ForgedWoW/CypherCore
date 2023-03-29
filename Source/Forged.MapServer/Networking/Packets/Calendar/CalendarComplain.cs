// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Calendar;

internal class CalendarComplain : ClientPacket
{
    private ObjectGuid InvitedByGUID;
    private ulong InviteID;
    private ulong EventID;
    public CalendarComplain(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        InvitedByGUID = _worldPacket.ReadPackedGuid();
        EventID = _worldPacket.ReadUInt64();
        InviteID = _worldPacket.ReadUInt64();
    }
}