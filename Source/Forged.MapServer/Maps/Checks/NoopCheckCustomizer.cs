// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Maps.Checks;

public class NoopCheckCustomizer
{
	public virtual bool Test(WorldObject o)
	{
		return true;
	}

	public virtual void Update(WorldObject o) { }
}