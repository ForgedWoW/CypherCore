// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps;

public class RespawnDo : IDoWork<WorldObject>
{
	public void Invoke(WorldObject obj)
	{
		switch (obj.TypeId)
		{
			case TypeId.Unit:
				obj.				AsCreature.Respawn();

				break;
			case TypeId.GameObject:
				obj.				AsGameObject.Respawn();

				break;
		}
	}
}