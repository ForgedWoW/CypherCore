// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Interfaces;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

public class AIRelocationNotifier : IGridNotifierCreature
{
	readonly Unit _unit;
	readonly bool _isCreature;

	public GridType GridType { get; set; }

	public AIRelocationNotifier(Unit unit, GridType gridType)
	{
		_unit = unit;
		_isCreature = unit.IsTypeId(TypeId.Unit);
		GridType = gridType;
	}

	public void Visit(IList<Creature> objs)
	{
		for (var i = 0; i < objs.Count; ++i)
		{
			var creature = objs[i];
			NotifierHelpers.CreatureUnitRelocationWorker(creature, _unit);

			if (_isCreature)
				NotifierHelpers.CreatureUnitRelocationWorker(_unit.AsCreature, creature);
		}
	}
}