// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Spells;

namespace Forged.RealmServer.Maps;

public class AnyDeadUnitSpellTargetInRangeCheck<T> : AnyDeadUnitObjectInRangeCheck<T> where T : WorldObject
{
	readonly WorldObjectSpellTargetCheck _check;

	public AnyDeadUnitSpellTargetInRangeCheck(WorldObject searchObj, float range, SpellInfo spellInfo, SpellTargetCheckTypes check, SpellTargetObjectTypes objectType) : base(searchObj, range)
	{
		_check = new WorldObjectSpellTargetCheck(searchObj, searchObj, spellInfo, check, null, objectType);
	}

	public override bool Invoke(T obj)
	{
		return base.Invoke(obj) && _check.Invoke(obj);
	}
}