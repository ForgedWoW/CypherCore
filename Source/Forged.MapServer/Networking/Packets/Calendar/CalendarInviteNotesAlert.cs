// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Calendar;

internal class CalendarInviteNotesAlert : ServerPacket
{
    public ulong EventID;
    public string Notes;

    public CalendarInviteNotesAlert(ulong eventID, string notes) : base(ServerOpcodes.CalendarInviteNotesAlert)
    {
        EventID = eventID;
        Notes = notes;
    }

    public override void Write()
    {
        WorldPacket.WriteUInt64(EventID);

        WorldPacket.WriteBits(Notes.GetByteCount(), 8);
        WorldPacket.FlushBits();
        WorldPacket.WriteString(Notes);
    }
}