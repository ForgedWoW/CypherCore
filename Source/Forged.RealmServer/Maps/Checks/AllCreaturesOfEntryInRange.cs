// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Maps;

public class AllCreaturesOfEntryInRange : ICheck<Creature>
{
	readonly WorldObject _pObject;
	readonly uint _uiEntry;
	readonly float _fRange;

	public AllCreaturesOfEntryInRange(WorldObject obj, uint entry, float maxRange = 0f)
	{
		_pObject = obj;
		_uiEntry = entry;
		_fRange = maxRange;
	}

	public bool Invoke(Creature creature)
	{
		if (_uiEntry != 0)
			if (creature.Entry != _uiEntry)
				return false;

		if (_fRange != 0f)
		{
			if (_fRange > 0.0f && !_pObject.IsWithinDist(creature, _fRange, false))
				return false;

			if (_fRange < 0.0f && _pObject.IsWithinDist(creature, _fRange, false))
				return false;
		}

		return true;
	}
}