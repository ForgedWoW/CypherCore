// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Conditions;
using Game.Entities;

namespace Game.Spells;

public class WorldObjectSpellAreaTargetCheck : WorldObjectSpellTargetCheck
{
	readonly float _range;
	readonly Position _position;

	public WorldObjectSpellAreaTargetCheck(float range, Position position, WorldObject caster, WorldObject referer, SpellInfo spellInfo, SpellTargetCheckTypes selectionType, List<Condition> condList, SpellTargetObjectTypes objectType)
		: base(caster, referer, spellInfo, selectionType, condList, objectType)
	{
		_range = range;
		_position = position;
	}

	public override bool Invoke(WorldObject target)
	{
		if (target.ToGameObject())
		{
			// isInRange including the dimension of the GO
			var isInRange = target.ToGameObject().IsInRange(_position.X, _position.Y, _position.Z, _range);

			if (!isInRange)
				return false;
		}
		else
		{
			var isInsideCylinder = target.IsWithinDist2d(_position, _range) && Math.Abs(target.Location.Z - _position.Z) <= _range;

			if (!isInsideCylinder)
				return false;
		}

		return base.Invoke(target);
	}
}