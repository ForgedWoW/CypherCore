// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Calendar;

internal class CalendarModeratorStatus : ServerPacket
{
    public bool ClearPending;
    public ulong EventID;
    public ObjectGuid InviteGuid;
    public CalendarInviteStatus Status;
    public CalendarModeratorStatus() : base(ServerOpcodes.CalendarModeratorStatus) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(InviteGuid);
        WorldPacket.WriteUInt64(EventID);
        WorldPacket.WriteUInt8((byte)Status);

        WorldPacket.WriteBit(ClearPending);
        WorldPacket.FlushBits();
    }
}