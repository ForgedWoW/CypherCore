// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

class CalendarUpdateEvent : ClientPacket
{
	public uint MaxSize;
	public CalendarUpdateEventInfo EventInfo;
	public CalendarUpdateEvent(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		EventInfo.Read(_worldPacket);
		MaxSize = _worldPacket.ReadUInt32();
	}
}