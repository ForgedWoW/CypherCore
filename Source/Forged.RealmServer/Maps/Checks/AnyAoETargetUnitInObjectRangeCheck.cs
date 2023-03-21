// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Spells;

namespace Forged.RealmServer.Maps;

public class AnyAoETargetUnitInObjectRangeCheck : ICheck<Unit>
{
	readonly WorldObject _obj;
	readonly Unit _funit;
	readonly SpellInfo _spellInfo;
	readonly float _range;
	readonly bool _incOwnRadius;
	readonly bool _incTargetRadius;

	public AnyAoETargetUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range, SpellInfo spellInfo = null, bool incOwnRadius = true, bool incTargetRadius = true)
	{
		_obj = obj;
		_funit = funit;
		_spellInfo = spellInfo;
		_range = range;
		_incOwnRadius = incOwnRadius;
		_incTargetRadius = incTargetRadius;
	}

	public bool Invoke(Unit u)
	{
		// Check contains checks for: live, uninteractible, non-attackable flags, flight check and GM check, ignore totems
		if (u.IsTypeId(TypeId.Unit) && u.IsTotem)
			return false;

		if (_spellInfo != null)
		{
			if (!u.IsPlayer)
			{
				if (_spellInfo.HasAttribute(SpellAttr3.OnlyOnPlayer))
					return false;

				if (_spellInfo.HasAttribute(SpellAttr5.NotOnPlayerControlledNpc) && u.IsControlledByPlayer)
					return false;
			}
			else if (_spellInfo.HasAttribute(SpellAttr5.NotOnPlayer))
			{
				return false;
			}
		}

		if (!_funit.IsValidAttackTarget(u, _spellInfo))
			return false;

		var searchRadius = _range;

		if (_incOwnRadius)
			searchRadius += _obj.CombatReach;

		if (_incTargetRadius)
			searchRadius += u.CombatReach;

		return u.IsInMap(_obj) && u.InSamePhase(_obj) && u.Location.IsWithinDoubleVerticalCylinder(_obj.Location, searchRadius, searchRadius);
	}
}