// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.Entities.Units;

namespace Forged.MapServer.AI.PlayerAI;

internal struct ValidTargetSelectPredicate : ICheck<Unit>
{
    private readonly IUnitAI _ai;

	public ValidTargetSelectPredicate(IUnitAI ai)
	{
		_ai = ai;
	}

	public bool Invoke(Unit target)
	{
		return _ai.CanAIAttack(target);
	}
}