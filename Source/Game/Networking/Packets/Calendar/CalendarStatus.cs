﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

class CalendarStatus : ClientPacket
{
	public ObjectGuid Guid;
	public ulong EventID;
	public ulong ModeratorID;
	public ulong InviteID;
	public byte Status;
	public CalendarStatus(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Guid = _worldPacket.ReadPackedGuid();
		EventID = _worldPacket.ReadUInt64();
		InviteID = _worldPacket.ReadUInt64();
		ModeratorID = _worldPacket.ReadUInt64();
		Status = _worldPacket.ReadUInt8();
	}
}