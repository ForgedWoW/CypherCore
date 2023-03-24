﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Social;

public class DelIgnore : ClientPacket
{
	public QualifiedGUID Player;
	public DelIgnore(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Player.Read(_worldPacket);
	}
}
