// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Maps.Checks;

public class PlayerOrPetCheck : ICheck<WorldObject>
{
	public bool Invoke(WorldObject obj)
	{
		if (obj.IsTypeId(TypeId.Player))
			return false;

		var creature = obj.AsCreature;

		if (creature)
			return !creature.IsPet;

		return true;
	}
}