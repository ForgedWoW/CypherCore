// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Maps;

public class NoopCheckCustomizer
{
	public virtual bool Test(WorldObject o)
	{
		return true;
	}

	public virtual void Update(WorldObject o) { }
}