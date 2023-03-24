// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Common.Entities.AreaTriggers;
using Game.Common.Entities.Creatures;
using Game.Common.Entities.GameObjects;
using Game.Common.Entities.Objects;

namespace Game.Common.Entities;

public class SpawnData : SpawnMetadata
{
	public uint Id; // entry in respective _template table
	public Position SpawnPoint;
	public PhaseUseFlagsValues PhaseUseFlags;
	public uint PhaseId;
	public uint PhaseGroup;
	public int terrainSwapMap;
	public uint poolId;
	public int spawntimesecs;
	public List<Difficulty> SpawnDifficulties;
	public uint ScriptId;
	public string StringId;

	public SpawnData(SpawnObjectType t) : base(t)
	{
		SpawnPoint = new Position();
		terrainSwapMap = -1;
		SpawnDifficulties = new List<Difficulty>();
	}

	public static SpawnObjectType TypeFor<T>()
	{
		switch (typeof(T).Name)
		{
			case nameof(Creature):
				return SpawnObjectType.Creature;
			case nameof(GameObject):
				return SpawnObjectType.GameObject;
			case nameof(AreaTrigger):
				return SpawnObjectType.AreaTrigger;
			default:
				return SpawnObjectType.NumSpawnTypes;
		}
	}
}
