// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Calendar;

internal class CalendarInviteStatusAlert : ServerPacket
{
    public long Date;
    public ulong EventID;
    public uint Flags;
    public byte Status;
    public CalendarInviteStatusAlert() : base(ServerOpcodes.CalendarInviteStatusAlert) { }

    public override void Write()
    {
        _worldPacket.WriteUInt64(EventID);
        _worldPacket.WritePackedTime(Date);
        _worldPacket.WriteUInt32(Flags);
        _worldPacket.WriteUInt8(Status);
    }
}