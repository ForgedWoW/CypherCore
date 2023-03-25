﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

class CalendarAddEvent : ClientPacket
{
	public uint MaxSize = 100;
	public CalendarAddEventInfo EventInfo = new();
	public CalendarAddEvent(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		EventInfo.Read(_worldPacket);
		MaxSize = _worldPacket.ReadUInt32();
	}
}