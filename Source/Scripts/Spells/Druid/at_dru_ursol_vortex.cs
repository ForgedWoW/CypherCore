// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Druid;

[Script]
public class at_dru_ursol_vortex : AreaTriggerScript, IAreaTriggerOnUnitEnter, IAreaTriggerOnUnitExit
{
	private bool _hasPull = false;

	public void OnUnitEnter(Unit target)
	{
		var caster = At.GetCaster();

		if (caster != null && caster.IsInCombatWith(target))
			caster.CastSpell(target, DruidSpells.URSOL_VORTEX_DEBUFF, true);
	}

	public void OnUnitExit(Unit target)
	{
		target.RemoveAura(DruidSpells.URSOL_VORTEX_DEBUFF);

		if (!_hasPull && target.IsValidAttackTarget(At.GetCaster()))
		{
			_hasPull = true;
			target.CastSpell(At.Location, DruidSpells.URSOL_VORTEX_PULL, true);
		}
	}
}