// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.AI;

struct ValidTargetSelectPredicate : ICheck<Unit>
{
	readonly IUnitAI _ai;

	public ValidTargetSelectPredicate(IUnitAI ai)
	{
		_ai = ai;
	}

	public bool Invoke(Unit target)
	{
		return _ai.CanAIAttack(target);
	}
}