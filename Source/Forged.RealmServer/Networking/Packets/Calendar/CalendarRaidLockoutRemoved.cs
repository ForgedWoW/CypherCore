// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer.Networking.Packets;

class CalendarRaidLockoutRemoved : ServerPacket
{
	public ulong InstanceID;
	public int MapID;
	public Difficulty DifficultyID;
	public CalendarRaidLockoutRemoved() : base(ServerOpcodes.CalendarRaidLockoutRemoved) { }

	public override void Write()
	{
		_worldPacket.WriteUInt64(InstanceID);
		_worldPacket.WriteInt32(MapID);
		_worldPacket.WriteUInt32((uint)DifficultyID);
	}
}