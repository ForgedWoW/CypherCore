// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Calendar;

internal class CalendarRaidLockoutUpdated : ServerPacket
{
    public uint DifficultyID;
    public int MapID;
    public int NewTimeRemaining;
    public int OldTimeRemaining;
    public long ServerTime;
    public CalendarRaidLockoutUpdated() : base(ServerOpcodes.CalendarRaidLockoutUpdated) { }

    public override void Write()
    {
        _worldPacket.WritePackedTime(ServerTime);
        _worldPacket.WriteInt32(MapID);
        _worldPacket.WriteUInt32(DifficultyID);
        _worldPacket.WriteInt32(OldTimeRemaining);
        _worldPacket.WriteInt32(NewTimeRemaining);
    }
}