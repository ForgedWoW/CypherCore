// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.CHRONO_LOOP)]
public class aura_evoker_chrono_loop : AuraScript, IAuraOnApply, IAuraOnRemove
{
	long _health = 0;
	uint _mapId = 0;
	Position _pos;

	public void AuraRemoved(AuraRemoveMode removeMode)
	{
		var unit = OwnerAsUnit;

		if (!unit.IsAlive)
			return;

		unit.SetHealth(Math.Min(_health, unit.MaxHealth));

		if (unit.Location.MapId == _mapId)
			unit.UpdatePosition(_pos, true);
	}

	public void AuraApplied()
	{
		var unit = OwnerAsUnit;
		_health = unit.Health;
		_mapId = unit.Location.MapId;
		_pos = new Position(unit.Location);
	}
}