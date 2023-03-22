// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Game.Networking.Packets;

public class DungeonScoreSeasonData
{
	public int Season;
	public List<DungeonScoreMapData> SeasonMaps = new();
	public List<DungeonScoreMapData> LadderMaps = new();
	public float SeasonScore;
	public float LadderScore = 0.0f;

	public void Write(WorldPacket data)
	{
		data.WriteInt32(Season);
		data.WriteInt32(SeasonMaps.Count);
		data.WriteInt32(LadderMaps.Count);
		data.WriteFloat(SeasonScore);
		data.WriteFloat(LadderScore);

		foreach (var map in SeasonMaps)
			map.Write(data);

		foreach (var map in LadderMaps)
			map.Write(data);
	}
}