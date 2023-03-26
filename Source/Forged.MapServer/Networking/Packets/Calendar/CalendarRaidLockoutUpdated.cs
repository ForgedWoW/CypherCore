// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Calendar;

internal class CalendarRaidLockoutUpdated : ServerPacket
{
	public long ServerTime;
	public int MapID;
	public uint DifficultyID;
	public int NewTimeRemaining;
	public int OldTimeRemaining;
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