// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;
using Game.Common.Networking.Packets.Achievements;

namespace Game.Common.Networking.Packets.Achievements;

public class AllAchievements
{
	public List<EarnedAchievement> Earned = new();
	public List<CriteriaProgressPkt> Progress = new();

	public void Write(WorldPacket data)
	{
		data.WriteInt32(Earned.Count);
		data.WriteInt32(Progress.Count);

		foreach (var earned in Earned)
			earned.Write(data);

		foreach (var progress in Progress)
			progress.Write(data);
	}
}
