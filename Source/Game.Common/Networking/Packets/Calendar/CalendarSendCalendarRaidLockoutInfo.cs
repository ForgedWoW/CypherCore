// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Calendar;

public struct CalendarSendCalendarRaidLockoutInfo
{
	public void Write(WorldPacket data)
	{
		data.WriteUInt64(InstanceID);
		data.WriteInt32(MapID);
		data.WriteUInt32(DifficultyID);
		data.WriteInt32(ExpireTime);
	}

	public ulong InstanceID;
	public int MapID;
	public uint DifficultyID;
	public int ExpireTime;
}
