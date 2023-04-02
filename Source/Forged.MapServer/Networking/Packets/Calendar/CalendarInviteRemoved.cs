// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Calendar;

internal class CalendarInviteRemoved : ServerPacket
{
    public bool ClearPending;
    public ulong EventID;
    public uint Flags;
    public ObjectGuid InviteGuid;
    public CalendarInviteRemoved() : base(ServerOpcodes.CalendarInviteRemoved) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(InviteGuid);
        WorldPacket.WriteUInt64(EventID);
        WorldPacket.WriteUInt32(Flags);

        WorldPacket.WriteBit(ClearPending);
        WorldPacket.FlushBits();
    }
}