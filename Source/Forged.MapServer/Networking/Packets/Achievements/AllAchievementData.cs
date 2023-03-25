﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

public class AllAchievementData : ServerPacket
{
	public AllAchievements Data = new();
	public AllAchievementData() : base(ServerOpcodes.AllAchievementData, ConnectionType.Instance) { }

	public override void Write()
	{
		Data.Write(_worldPacket);
	}
}

//Structs