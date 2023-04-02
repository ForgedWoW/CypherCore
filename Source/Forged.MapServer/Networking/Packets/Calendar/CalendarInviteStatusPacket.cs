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
        _worldPacket.WritePackedGuid(InviteGuid);
        _worldPacket.WriteUInt64(EventID);
        _worldPacket.WritePackedTime(Date);
        _worldPacket.WriteUInt32((uint)Flags);
        _worldPacket.WriteUInt8((byte)Status);
        _worldPacket.WritePackedTime(ResponseTime);

        _worldPacket.WriteBit(ClearPending);
        _worldPacket.FlushBits();
    }
}