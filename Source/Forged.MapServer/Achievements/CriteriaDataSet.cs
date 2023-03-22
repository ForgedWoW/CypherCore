// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Achievements;

public class CriteriaDataSet
{
	readonly List<CriteriaData> _storage = new();
	uint _criteriaId;

	public void Add(CriteriaData data)
	{
		_storage.Add(data);
	}

	public bool Meets(Player source, WorldObject target, uint miscValue = 0, uint miscValue2 = 0)
	{
		foreach (var data in _storage)
			if (!data.Meets(_criteriaId, source, target, miscValue, miscValue2))
				return false;

		return true;
	}

	public void SetCriteriaId(uint id)
	{
		_criteriaId = id;
	}
}