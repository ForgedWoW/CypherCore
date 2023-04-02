// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Maps.Instances;

public class DungeonEncounterData
{
    public DungeonEncounterData(uint bossId, params uint[] dungeonEncounterIds)
    {
        BossId = bossId;
        DungeonEncounterId = dungeonEncounterIds;
    }

    public uint BossId { get; set; }
    public uint[] DungeonEncounterId { get; set; } = new uint[4];
}