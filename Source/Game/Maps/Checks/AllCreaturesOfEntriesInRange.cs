// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

public class AllCreaturesOfEntriesInRange : ICheck<Creature>
{
	readonly WorldObject _pObject;
	readonly uint[] _uiEntry;
	readonly float _fRange;

	public AllCreaturesOfEntriesInRange(WorldObject obj, uint[] entry, float maxRange = 0f)
	{
		_pObject = obj;
		_uiEntry = entry;
		_fRange = maxRange;
	}

	public bool Invoke(Creature creature)
	{
		if (_uiEntry != null)
		{
			var match = false;

			foreach (var entry in _uiEntry)
				if (entry != 0 && creature.GetEntry() == entry)
					match = true;

			if (!match)
				return false;
		}

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