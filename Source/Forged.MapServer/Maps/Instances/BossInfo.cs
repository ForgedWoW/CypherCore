// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Common.DataStorage.Structs.D;

namespace Game.Maps;

public class BossInfo
{
	public EncounterState State { get; set; }
	public List<ObjectGuid>[] Door { get; set; } = new List<ObjectGuid>[(int)DoorType.Max];
	public List<ObjectGuid> Minion { get; set; } = new();
	public List<AreaBoundary> Boundary { get; set; } = new();
	public DungeonEncounterRecord[] DungeonEncounters { get; set; } = new DungeonEncounterRecord[MapConst.MaxDungeonEncountersPerBoss];

	public BossInfo()
	{
		State = EncounterState.ToBeDecided;

		for (var i = 0; i < (int)DoorType.Max; ++i)
			Door[i] = new List<ObjectGuid>();
	}

	public DungeonEncounterRecord GetDungeonEncounterForDifficulty(Difficulty difficulty)
	{
		return DungeonEncounters.FirstOrDefault(dungeonEncounter => dungeonEncounter?.DifficultyID == 0 || (Difficulty)dungeonEncounter?.DifficultyID == difficulty);
	}
}