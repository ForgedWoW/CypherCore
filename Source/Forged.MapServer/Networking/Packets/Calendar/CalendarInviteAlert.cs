// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Calendar;

internal class CalendarInviteAlert : ServerPacket
{
    public long Date;
    public ObjectGuid EventGuildID;
    public ulong EventID;
    public string EventName;
    public CalendarEventType EventType;
    public CalendarFlags Flags;
    public ObjectGuid InvitedByGuid;
    public ulong InviteID;
    public CalendarModerationRank ModeratorStatus;
    public ObjectGuid OwnerGuid;
    public CalendarInviteStatus Status;
    public int TextureID;
    public CalendarInviteAlert() : base(ServerOpcodes.CalendarInviteAlert) { }

    public override void Write()
    {
        WorldPacket.WriteUInt64(EventID);
        WorldPacket.WritePackedTime(Date);
        WorldPacket.WriteUInt32((uint)Flags);
        WorldPacket.WriteUInt8((byte)EventType);
        WorldPacket.WriteInt32(TextureID);
        WorldPacket.WritePackedGuid(EventGuildID);
        WorldPacket.WriteUInt64(InviteID);
        WorldPacket.WriteUInt8((byte)Status);
        WorldPacket.WriteUInt8((byte)ModeratorStatus);

        // Todo: check order
        WorldPacket.WritePackedGuid(InvitedByGuid);
        WorldPacket.WritePackedGuid(OwnerGuid);

        WorldPacket.WriteBits(EventName.GetByteCount(), 8);
        WorldPacket.FlushBits();
        WorldPacket.WriteString(EventName);
    }
}