// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

public class GetAllAlliesOfTargetCreaturesWithinRange : ICheck<Creature>
{
	readonly Unit _pObject;
	readonly float _fRange;

	public GetAllAlliesOfTargetCreaturesWithinRange(Unit obj, float maxRange = 0f)
	{
		_pObject = obj;
		_fRange  = maxRange;
	}

	public bool Invoke(Creature creature)
	{
		if (creature.IsHostileTo(_pObject))
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