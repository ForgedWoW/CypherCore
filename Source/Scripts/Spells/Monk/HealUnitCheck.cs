// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;

namespace Scripts.Spells.Monk;

public class HealUnitCheck : ICheck<WorldObject>
{
	private readonly Unit m_source;

	public HealUnitCheck(Unit source)
	{
		m_source = source;
	}

	public bool Invoke(WorldObject @object)
	{
		var unit = @object.ToUnit();

		if (unit == null)
			return true;

		if (m_source.IsFriendlyTo(unit))
			return false;

		return true;
	}
}