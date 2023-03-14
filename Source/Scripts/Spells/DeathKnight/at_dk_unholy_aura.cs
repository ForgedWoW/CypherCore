// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.DeathKnight;

[Script]
public class at_dk_unholy_aura : AreaTriggerScript, IAreaTriggerOnUnitEnter, IAreaTriggerOnUnitExit
{
	public void OnUnitEnter(Unit unit)
	{
		var caster = At.GetCaster();

		if (caster != null)
			if (!unit.IsFriendlyTo(caster))
				caster.CastSpell(unit, DeathKnightSpells.UNHOLY_AURA, true);
	}

	public void OnUnitExit(Unit unit)
	{
		unit.RemoveAura(DeathKnightSpells.UNHOLY_AURA);
	}
}