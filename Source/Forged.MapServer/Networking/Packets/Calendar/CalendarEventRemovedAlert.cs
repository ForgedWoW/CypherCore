// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Calendar;

internal class CalendarEventRemovedAlert : ServerPacket
{
    public bool ClearPending;
    public long Date;
    public ulong EventID;
    public CalendarEventRemovedAlert() : base(ServerOpcodes.CalendarEventRemovedAlert) { }

    public override void Write()
    {
        WorldPacket.WriteUInt64(EventID);
        WorldPacket.WritePackedTime(Date);

        WorldPacket.WriteBit(ClearPending);
        WorldPacket.FlushBits();
    }
}