// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.Structs.D;
using Framework.Constants;

namespace Forged.MapServer.Maps.Instances;

public struct UpdateBossStateSaveDataEvent
{
    public uint BossId;
    public DungeonEncounterRecord DungeonEncounter;
    public EncounterState NewState;

    public UpdateBossStateSaveDataEvent(DungeonEncounterRecord dungeonEncounter, uint bossId, EncounterState state)
    {
        DungeonEncounter = dungeonEncounter;
        BossId = bossId;
        NewState = state;
    }
}