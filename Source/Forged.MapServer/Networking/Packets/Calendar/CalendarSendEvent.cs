// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Calendar;

internal class CalendarSendEvent : ServerPacket
{
    public long Date;
    public string Description;
    public ObjectGuid EventGuildID;
    public ulong EventID;
    public string EventName;
    public CalendarSendEventType EventType;
    public CalendarFlags Flags;
    public CalendarEventType GetEventType;
    public List<CalendarEventInviteInfo> Invites = new();
    public long LockDate;
    public ObjectGuid OwnerGuid;
    public int TextureID;
    public CalendarSendEvent() : base(ServerOpcodes.CalendarSendEvent) { }

    public override void Write()
    {
        _worldPacket.WriteUInt8((byte)EventType);
        _worldPacket.WritePackedGuid(OwnerGuid);
        _worldPacket.WriteUInt64(EventID);
        _worldPacket.WriteUInt8((byte)GetEventType);
        _worldPacket.WriteInt32(TextureID);
        _worldPacket.WriteUInt32((uint)Flags);
        _worldPacket.WritePackedTime(Date);
        _worldPacket.WriteUInt32((uint)LockDate);
        _worldPacket.WritePackedGuid(EventGuildID);
        _worldPacket.WriteInt32(Invites.Count);

        _worldPacket.WriteBits(EventName.GetByteCount(), 8);
        _worldPacket.WriteBits(Description.GetByteCount(), 11);
        _worldPacket.FlushBits();

        foreach (var invite in Invites)
            invite.Write(_worldPacket);

        _worldPacket.WriteString(EventName);
        _worldPacket.WriteString(Description);
    }
}