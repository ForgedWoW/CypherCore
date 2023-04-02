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
        _worldPacket.WriteUInt64(EventID);

        _worldPacket.WritePackedTime(OriginalDate);
        _worldPacket.WritePackedTime(Date);
        _worldPacket.WriteUInt32((uint)LockDate);
        _worldPacket.WriteUInt32((uint)Flags);
        _worldPacket.WriteInt32(TextureID);
        _worldPacket.WriteUInt8((byte)EventType);

        _worldPacket.WriteBits(EventName.GetByteCount(), 8);
        _worldPacket.WriteBits(Description.GetByteCount(), 11);
        _worldPacket.WriteBit(ClearPending);
        _worldPacket.FlushBits();

        _worldPacket.WriteString(EventName);
        _worldPacket.WriteString(Description);
    }
}