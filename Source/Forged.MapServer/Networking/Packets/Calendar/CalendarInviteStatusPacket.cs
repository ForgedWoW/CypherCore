// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Calendar;

internal class CalendarInviteStatusPacket : ServerPacket
{
    public bool ClearPending;
    public long Date;
    public ulong EventID;
    public CalendarFlags Flags;
    public ObjectGuid InviteGuid;
    public long ResponseTime;
    public CalendarInviteStatus Status;
    public CalendarInviteStatusPacket() : base(ServerOpcodes.CalendarInviteStatus) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(InviteGuid);
        WorldPacket.WriteUInt64(EventID);
        WorldPacket.WritePackedTime(Date);
        WorldPacket.WriteUInt32((uint)Flags);
        WorldPacket.WriteUInt8((byte)Status);
        WorldPacket.WritePackedTime(ResponseTime);

        WorldPacket.WriteBit(ClearPending);
        WorldPacket.FlushBits();
    }
}