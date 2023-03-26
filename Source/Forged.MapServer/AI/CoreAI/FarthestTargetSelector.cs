// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.AI.CoreAI;

internal class FarthestTargetSelector : ICheck<Unit>
{
    private readonly Unit _me;
    private readonly float _dist;
    private readonly bool _playerOnly;
    private readonly bool _inLos;

	public FarthestTargetSelector(Unit unit, float dist, bool playerOnly, bool inLos)
	{
		_me = unit;
		_dist = dist;
		_playerOnly = playerOnly;
		_inLos = inLos;
	}

	public bool Invoke(Unit target)
	{
		if (_me == null || target == null)
			return false;

		if (_playerOnly && target.TypeId != TypeId.Player)
			return false;

		if (_dist > 0.0f && !_me.IsWithinCombatRange(target, _dist))
			return false;

		if (_inLos && !_me.IsWithinLOSInMap(target))
			return false;

		return true;
	}
}