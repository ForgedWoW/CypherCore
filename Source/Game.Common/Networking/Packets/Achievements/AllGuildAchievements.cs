// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Networking.Packets;

public class AllGuildAchievements : ServerPacket
{
	public List<EarnedAchievement> Earned = new();
	public AllGuildAchievements() : base(ServerOpcodes.AllGuildAchievements) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Earned.Count);

		foreach (var earned in Earned)
			earned.Write(_worldPacket);
	}
}