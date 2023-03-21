// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.Conditions;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Spells;

public class WorldObjectSpellLineTargetCheck : WorldObjectSpellAreaTargetCheck
{
	readonly Position _position;
	readonly float _lineWidth;

	public WorldObjectSpellLineTargetCheck(Position srcPosition, Position dstPosition, float lineWidth, float range, WorldObject caster, SpellInfo spellInfo, SpellTargetCheckTypes selectionType, List<Condition> condList, SpellTargetObjectTypes objectType)
		: base(range, caster.Location, caster, caster, spellInfo, selectionType, condList, objectType)
	{
		_position = srcPosition;
		_lineWidth = lineWidth;

		if (dstPosition != null && srcPosition != dstPosition)
			_position.Orientation = srcPosition.GetAbsoluteAngle(dstPosition);
	}

	public override bool Invoke(WorldObject target)
	{
		if (!_position.HasInLine(target.Location, target.CombatReach, _lineWidth))
			return false;

		return base.Invoke(target);
	}
}