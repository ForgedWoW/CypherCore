// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Monk;

[Script]
public class at_monk_chi_burst : AreaTriggerScript, IAreaTriggerOnUnitEnter
{
	public void OnUnitEnter(Unit target)
	{
		if (!At.GetCaster())
			return;

		if (At.GetCaster().IsValidAssistTarget(target))
			At.GetCaster().CastSpell(target, MonkSpells.CHI_BURST_HEAL, true);

		if (At.GetCaster().IsValidAttackTarget(target))
			At.GetCaster().CastSpell(target, MonkSpells.CHI_BURST_DAMAGE, true);
	}
}