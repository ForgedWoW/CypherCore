// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Calendar;

internal class CalendarComplain : ClientPacket
{
    private ulong EventID;
    private ObjectGuid InvitedByGUID;
    private ulong InviteID;
    public CalendarComplain(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        InvitedByGUID = WorldPacket.ReadPackedGuid();
        EventID = WorldPacket.ReadUInt64();
        InviteID = WorldPacket.ReadUInt64();
    }
}