﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

public struct GameRuleValuePair
{
	public int Rule;
	public int Value;

	public void Write(WorldPacket data)
	{
		data.WriteInt32(Rule);
		data.WriteInt32(Value);
	}
}