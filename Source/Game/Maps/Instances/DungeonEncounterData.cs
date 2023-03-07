// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Maps;

public class DungeonEncounterData
{
	public uint BossId { get; set; }
    public uint[] DungeonEncounterId { get; set; } = new uint[4];

	public DungeonEncounterData(uint bossId, params uint[] dungeonEncounterIds)
	{
		BossId             = bossId;
		DungeonEncounterId = dungeonEncounterIds;
	}
}