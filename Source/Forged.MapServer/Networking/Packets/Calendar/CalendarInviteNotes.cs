// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Calendar;

internal class CalendarInviteNotes : ServerPacket
{
    public bool ClearPending;
    public ulong EventID;
    public ObjectGuid InviteGuid;
    public string Notes = "";
    public CalendarInviteNotes() : base(ServerOpcodes.CalendarInviteNotes) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(InviteGuid);
        WorldPacket.WriteUInt64(EventID);

        WorldPacket.WriteBits(Notes.GetByteCount(), 8);
        WorldPacket.WriteBit(ClearPending);
        WorldPacket.FlushBits();
        WorldPacket.WriteString(Notes);
    }
}