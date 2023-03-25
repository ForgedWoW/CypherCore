// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Maps.Checks;

public class AnyDeadUnitObjectInRangeCheck<T> : ICheck<T> where T : WorldObject
{
	readonly WorldObject _searchObj;
	readonly float _range;

	public AnyDeadUnitObjectInRangeCheck(WorldObject searchObj, float range)
	{
		_searchObj = searchObj;
		_range = range;
	}

	public virtual bool Invoke(T obj)
	{
		var player = obj.AsPlayer;

		if (player)
			return !player.IsAlive && !player.HasAuraType(AuraType.Ghost) && _searchObj.IsWithinDistInMap(player, _range);

		var creature = obj.AsCreature;

		if (creature)
			return !creature.IsAlive && _searchObj.IsWithinDistInMap(creature, _range);

		var corpse = obj.AsCorpse;

		if (corpse)
			return corpse.GetCorpseType() != CorpseType.Bones && _searchObj.IsWithinDistInMap(corpse, _range);

		return false;
	}
}