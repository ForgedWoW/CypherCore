// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Calendar;

public class CalendarRaidLockoutAdded : ServerPacket
{
	public ulong InstanceID;
	public Difficulty DifficultyID;
	public int TimeRemaining;
	public uint ServerTime;
	public int MapID;
	public CalendarRaidLockoutAdded() : base(ServerOpcodes.CalendarRaidLockoutAdded) { }

	public override void Write()
	{
		_worldPacket.WriteUInt64(InstanceID);
		_worldPacket.WriteUInt32(ServerTime);
		_worldPacket.WriteInt32(MapID);
		_worldPacket.WriteUInt32((uint)DifficultyID);
		_worldPacket.WriteInt32(TimeRemaining);
	}
}
