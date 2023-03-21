// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Maps;

public class UnitAuraCheck<T> : ICheck<T> where T : WorldObject
{
	readonly bool _present;
	readonly uint _spellId;
	readonly ObjectGuid _casterGUID;

	public UnitAuraCheck(bool present, uint spellId, ObjectGuid casterGUID = default)
	{
		_present = present;
		_spellId = spellId;
		_casterGUID = casterGUID;
	}

	public bool Invoke(T obj)
	{
		return obj.AsUnit && obj.AsUnit.HasAura(_spellId, _casterGUID) == _present;
	}

	public static implicit operator Predicate<T>(UnitAuraCheck<T> unit)
	{
		return unit.Invoke;
	}
}