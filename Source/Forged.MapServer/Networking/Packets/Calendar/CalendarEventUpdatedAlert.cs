// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Calendar;

internal class CalendarEventUpdatedAlert : ServerPacket
{
    public bool ClearPending;
    public long Date;
    public string Description;
    public ulong EventID;
    public string EventName;
    public CalendarEventType EventType;
    public CalendarFlags Flags;
    public long LockDate;
    public long OriginalDate;
    public int TextureID;
    public CalendarEventUpdatedAlert() : base(ServerOpcodes.CalendarEventUpdatedAlert) { }

    public override void Write()
    {
        WorldPacket.WriteUInt64(EventID);

        WorldPacket.WritePackedTime(OriginalDate);
        WorldPacket.WritePackedTime(Date);
        WorldPacket.WriteUInt32((uint)LockDate);
        WorldPacket.WriteUInt32((uint)Flags);
        WorldPacket.WriteInt32(TextureID);
        WorldPacket.WriteUInt8((byte)EventType);

        WorldPacket.WriteBits(EventName.GetByteCount(), 8);
        WorldPacket.WriteBits(Description.GetByteCount(), 11);
        WorldPacket.WriteBit(ClearPending);
        WorldPacket.FlushBits();

        WorldPacket.WriteString(EventName);
        WorldPacket.WriteString(Description);
    }
}