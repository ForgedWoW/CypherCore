﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Calendar;

internal class CalendarInviteRemovedAlert : ServerPacket
{
    public long Date;
    public ulong EventID;
    public CalendarFlags Flags;
    public CalendarInviteStatus Status;
    public CalendarInviteRemovedAlert() : base(ServerOpcodes.CalendarInviteRemovedAlert) { }

    public override void Write()
    {
        _worldPacket.WriteUInt64(EventID);
        _worldPacket.WritePackedTime(Date);
        _worldPacket.WriteUInt32((uint)Flags);
        _worldPacket.WriteUInt8((byte)Status);
    }
}