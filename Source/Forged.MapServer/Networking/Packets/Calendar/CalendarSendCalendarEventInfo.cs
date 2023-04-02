﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Calendar;

internal struct CalendarSendCalendarEventInfo
{
    public long Date;

    public ulong EventClubID;

    public ulong EventID;

    public string EventName;

    public CalendarEventType EventType;

    public CalendarFlags Flags;

    public ObjectGuid OwnerGuid;

    public int TextureID;

    public void Write(WorldPacket data)
    {
        data.WriteUInt64(EventID);
        data.WriteUInt8((byte)EventType);
        data.WritePackedTime(Date);
        data.WriteUInt32((uint)Flags);
        data.WriteInt32(TextureID);
        data.WriteUInt64(EventClubID);
        data.WritePackedGuid(OwnerGuid);

        data.WriteBits(EventName.GetByteCount(), 8);
        data.FlushBits();
        data.WriteString(EventName);
    }
}