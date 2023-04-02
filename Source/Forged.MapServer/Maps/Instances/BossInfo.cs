// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.DataStorage.Structs.D;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Maps.Instances;

public class BossInfo
{
    public BossInfo()
    {
        State = EncounterState.ToBeDecided;

        for (var i = 0; i < (int)DoorType.Max; ++i)
            Door[i] = new List<ObjectGuid>();
    }

    public List<AreaBoundary> Boundary { get; set; } = new();
    public List<ObjectGuid>[] Door { get; set; } = new List<ObjectGuid>[(int)DoorType.Max];
    public DungeonEncounterRecord[] DungeonEncounters { get; set; } = new DungeonEncounterRecord[MapConst.MaxDungeonEncountersPerBoss];
    public List<ObjectGuid> Minion { get; set; } = new();
    public EncounterState State { get; set; }
    public DungeonEncounterRecord GetDungeonEncounterForDifficulty(Difficulty difficulty)
    {
        return DungeonEncounters.FirstOrDefault(dungeonEncounter => dungeonEncounter?.DifficultyID == 0 || (Difficulty)dungeonEncounter?.DifficultyID == difficulty);
    }
}