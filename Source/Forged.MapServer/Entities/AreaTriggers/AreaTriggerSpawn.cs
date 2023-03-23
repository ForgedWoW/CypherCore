// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Maps;
using Game.Common.Entities;

namespace Game.Entities;

public class AreaTriggerSpawn : SpawnData
{
	public AreaTriggerId TriggerId;
	public AreaTriggerShapeInfo Shape = new();

	public AreaTriggerSpawn() : base(SpawnObjectType.AreaTrigger) { }
}