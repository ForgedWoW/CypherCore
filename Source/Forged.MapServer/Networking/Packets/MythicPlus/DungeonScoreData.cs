// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Game.Networking.Packets;

public class DungeonScoreData
{
	public List<DungeonScoreSeasonData> Seasons = new();
	public int TotalRuns;

	public void Write(WorldPacket data)
	{
		data.WriteInt32(Seasons.Count);
		data.WriteInt32(TotalRuns);

		foreach (var season in Seasons)
			season.Write(data);
	}
}