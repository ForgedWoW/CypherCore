// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.DataStorage;

namespace Game.Maps;

public struct UpdateBossStateSaveDataEvent
{
	public DungeonEncounterRecord DungeonEncounter;
	public uint BossId;
	public EncounterState NewState;

	public UpdateBossStateSaveDataEvent(DungeonEncounterRecord dungeonEncounter, uint bossId, EncounterState state)
	{
		DungeonEncounter = dungeonEncounter;
		BossId           = bossId;
		NewState         = state;
	}
}