// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.DataStorage;

namespace Game.Maps;

public struct InstanceLockUpdateEvent
{
	public uint InstanceId;
	public string NewData;
	public uint InstanceCompletedEncountersMask;
	public DungeonEncounterRecord CompletedEncounter;
	public uint? EntranceWorldSafeLocId;

	public InstanceLockUpdateEvent(uint instanceId, string newData, uint instanceCompletedEncountersMask, DungeonEncounterRecord completedEncounter, uint? entranceWorldSafeLocId)
	{
		InstanceId                      = instanceId;
		NewData                         = newData;
		InstanceCompletedEncountersMask = instanceCompletedEncountersMask;
		CompletedEncounter              = completedEncounter;
		EntranceWorldSafeLocId          = entranceWorldSafeLocId;
	}
}