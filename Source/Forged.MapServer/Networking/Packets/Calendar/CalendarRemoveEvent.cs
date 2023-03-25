// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Calendar;

class CalendarRemoveEvent : ClientPacket
{
	public ulong ModeratorID;
	public ulong EventID;
	public ulong ClubID;
	public uint Flags;
	public CalendarRemoveEvent(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		EventID = _worldPacket.ReadUInt64();
		ModeratorID = _worldPacket.ReadUInt64();
		ClubID = _worldPacket.ReadUInt64();
		Flags = _worldPacket.ReadUInt32();
	}
}