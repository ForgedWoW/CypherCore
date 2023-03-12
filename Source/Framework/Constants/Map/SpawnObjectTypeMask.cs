// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum SpawnObjectTypeMask
{
	Creature = (1 << SpawnObjectType.Creature),
	GameObject = (1 << SpawnObjectType.GameObject),
	AreaTrigger = (1 << SpawnObjectType.AreaTrigger),

	WithData = (1 << SpawnObjectType.NumSpawnTypesWithData) - 1,
	All = (1 << SpawnObjectType.NumSpawnTypes) - 1
}