// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Calendar;

internal class CalendarRaidLockoutRemoved : ServerPacket
{
    public Difficulty DifficultyID;
    public ulong InstanceID;
    public int MapID;
    public CalendarRaidLockoutRemoved() : base(ServerOpcodes.CalendarRaidLockoutRemoved) { }

    public override void Write()
    {
        WorldPacket.WriteUInt64(InstanceID);
        WorldPacket.WriteInt32(MapID);
        WorldPacket.WriteUInt32((uint)DifficultyID);
    }
}