// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Conditions;
using Game.Entities;

namespace Game.Spells;

public class WorldObjectSpellTrajTargetCheck : WorldObjectSpellTargetCheck
{
	readonly float _range;
	readonly Position _position;

	public WorldObjectSpellTrajTargetCheck(float range, Position position, WorldObject caster, SpellInfo spellInfo, SpellTargetCheckTypes selectionType, List<Condition> condList, SpellTargetObjectTypes objectType)
		: base(caster, caster, spellInfo, selectionType, condList, objectType)
	{
		_range = range;
		_position = position;
	}

	public override bool Invoke(WorldObject target)
	{
		// return all targets on missile trajectory (0 - size of a missile)
		if (!Caster.Location.HasInLine(target.Location, target.CombatReach, SpellConst.TrajectoryMissileSize))
			return false;

		if (target.Location.GetExactDist2d(_position) > _range)
			return false;

		return base.Invoke(target);
	}
}