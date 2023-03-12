// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum SpawnGroupFlags
{
	None = 0x00,
	System = 0x01,
	CompatibilityMode = 0x02,
	ManualSpawn = 0x04,
	DynamicSpawnRate = 0x08,
	EscortQuestNpc = 0x10,
	DespawnOnConditionFailure = 0x20,

	All = (System | CompatibilityMode | ManualSpawn | DynamicSpawnRate | EscortQuestNpc | DespawnOnConditionFailure)
}