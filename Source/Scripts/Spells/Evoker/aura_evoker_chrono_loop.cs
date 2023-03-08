// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.CHRONO_LOOP)]
public class aura_evoker_chrono_loop : AuraScript, IHasAuraEffects, IAuraOnRemove
{
	long _health = 0;
	uint _mapId = 0;
	Position _pos;
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public void AuraRemoved()
	{
		var unit = UnitOwner;

		if (!unit.IsAlive)
			return;

		unit.SetHealth(Math.Min(_health, unit.GetMaxHealth()));

		if (unit.Location.MapId == _mapId)
			unit.UpdatePosition(_pos, true);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(AuraApplied,
													0,
													AuraType.Dummy,
													AuraEffectHandleModes.Real,
													AuraScriptHookType.EffectApply));
	}

	private void AuraApplied(AuraEffect aurEff, AuraEffectHandleModes handleModes)
	{
		var unit = UnitOwner;
		_health = unit.GetHealth();
		_mapId = unit.Location.MapId;
		_pos = new Position(unit.Location);
	}
}