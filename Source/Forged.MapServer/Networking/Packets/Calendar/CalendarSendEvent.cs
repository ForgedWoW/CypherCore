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
        WorldPacket.WriteUInt8((byte)EventType);
        WorldPacket.WritePackedGuid(OwnerGuid);
        WorldPacket.WriteUInt64(EventID);
        WorldPacket.WriteUInt8((byte)GetEventType);
        WorldPacket.WriteInt32(TextureID);
        WorldPacket.WriteUInt32((uint)Flags);
        WorldPacket.WritePackedTime(Date);
        WorldPacket.WriteUInt32((uint)LockDate);
        WorldPacket.WritePackedGuid(EventGuildID);
        WorldPacket.WriteInt32(Invites.Count);

        WorldPacket.WriteBits(EventName.GetByteCount(), 8);
        WorldPacket.WriteBits(Description.GetByteCount(), 11);
        WorldPacket.FlushBits();

        foreach (var invite in Invites)
            invite.Write(WorldPacket);

        WorldPacket.WriteString(EventName);
        WorldPacket.WriteString(Description);
    }
}