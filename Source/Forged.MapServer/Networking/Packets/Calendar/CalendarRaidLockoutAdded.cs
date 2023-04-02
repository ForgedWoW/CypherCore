// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Calendar;

internal class CalendarRaidLockoutAdded : ServerPacket
{
    public Difficulty DifficultyID;
    public ulong InstanceID;
    public int MapID;
    public uint ServerTime;
    public int TimeRemaining;
    public CalendarRaidLockoutAdded() : base(ServerOpcodes.CalendarRaidLockoutAdded) { }

    public override void Write()
    {
        WorldPacket.WriteUInt64(InstanceID);
        WorldPacket.WriteUInt32(ServerTime);
        WorldPacket.WriteInt32(MapID);
        WorldPacket.WriteUInt32((uint)DifficultyID);
        WorldPacket.WriteInt32(TimeRemaining);
    }
}