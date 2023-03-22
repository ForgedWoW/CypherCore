﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class HealthUpdate : ServerPacket
{
	public ObjectGuid Guid;
	public long Health;
	public HealthUpdate() : base(ServerOpcodes.HealthUpdate) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Guid);
		_worldPacket.WriteInt64(Health);
	}
}