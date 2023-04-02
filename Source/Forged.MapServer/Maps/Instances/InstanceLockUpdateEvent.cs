// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.Structs.D;

namespace Forged.MapServer.Maps.Instances;

public struct InstanceLockUpdateEvent
{
    public DungeonEncounterRecord CompletedEncounter;
    public uint? EntranceWorldSafeLocId;
    public uint InstanceCompletedEncountersMask;
    public uint InstanceId;
    public string NewData;
    public InstanceLockUpdateEvent(uint instanceId, string newData, uint instanceCompletedEncountersMask, DungeonEncounterRecord completedEncounter, uint? entranceWorldSafeLocId)
    {
        InstanceId = instanceId;
        NewData = newData;
        InstanceCompletedEncountersMask = instanceCompletedEncountersMask;
        CompletedEncounter = completedEncounter;
        EntranceWorldSafeLocId = entranceWorldSafeLocId;
    }
}