// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

public class AllWorldObjectsInRange : ICheck<WorldObject>
{
	readonly WorldObject _pObject;
	readonly float _fRange;

	public AllWorldObjectsInRange(WorldObject obj, float maxRange)
	{
		_pObject = obj;
		_fRange = maxRange;
	}

	public bool Invoke(WorldObject go)
	{
		return _pObject.IsWithinDist(go, _fRange, false) && _pObject.InSamePhase(go);
	}
}