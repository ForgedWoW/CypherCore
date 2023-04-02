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
        _worldPacket.WriteUInt64(EventID);
        _worldPacket.WritePackedTime(Date);
        _worldPacket.WriteUInt32((uint)Flags);
        _worldPacket.WriteUInt8((byte)EventType);
        _worldPacket.WriteInt32(TextureID);
        _worldPacket.WritePackedGuid(EventGuildID);
        _worldPacket.WriteUInt64(InviteID);
        _worldPacket.WriteUInt8((byte)Status);
        _worldPacket.WriteUInt8((byte)ModeratorStatus);

        // Todo: check order
        _worldPacket.WritePackedGuid(InvitedByGuid);
        _worldPacket.WritePackedGuid(OwnerGuid);

        _worldPacket.WriteBits(EventName.GetByteCount(), 8);
        _worldPacket.FlushBits();
        _worldPacket.WriteString(EventName);
    }
}