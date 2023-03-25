﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

class ObjectUpdateRescued : ClientPacket
{
	public ObjectGuid ObjectGUID;
	public ObjectUpdateRescued(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ObjectGUID = _worldPacket.ReadPackedGuid();
	}
}