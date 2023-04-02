// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Calendar;

internal class CalendarEventInviteInfo
{
    public ObjectGuid Guid;
    public ulong InviteID;
    public byte InviteType;
    public byte Level = 1;
    public CalendarModerationRank Moderator;
    public string Notes;
    public long ResponseTime;
    public CalendarInviteStatus Status;
    public void Write(WorldPacket data)
    {
        data.WritePackedGuid(Guid);
        data.WriteUInt64(InviteID);

        data.WriteUInt8(Level);
        data.WriteUInt8((byte)Status);
        data.WriteUInt8((byte)Moderator);
        data.WriteUInt8(InviteType);

        data.WritePackedTime(ResponseTime);

        data.WriteBits(Notes.GetByteCount(), 8);
        data.FlushBits();
        data.WriteString(Notes);
    }
}