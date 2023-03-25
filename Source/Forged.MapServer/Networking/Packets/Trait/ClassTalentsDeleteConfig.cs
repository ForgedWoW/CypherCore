﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

class ClassTalentsDeleteConfig : ClientPacket
{
	public int ConfigID;

	public ClassTalentsDeleteConfig(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ConfigID = _worldPacket.ReadInt32();
	}
}