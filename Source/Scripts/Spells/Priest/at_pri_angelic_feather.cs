// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Priest;

[Script]
public class at_pri_angelic_feather : AreaTriggerScript, IAreaTriggerOnCreate, IAreaTriggerOnUnitEnter
{
	public void OnCreate()
	{
		var caster = At.GetCaster();

		if (caster != null)
		{
			var areaTriggers = caster.GetAreaTriggers(PriestSpells.ANGELIC_FEATHER_AREATRIGGER);

			if (areaTriggers.Count >= 3)
				areaTriggers[0].SetDuration(0);
		}
	}

	public void OnUnitEnter(Unit unit)
	{
		var caster = At.GetCaster();

		if (caster != null)
			if (caster.IsFriendlyTo(unit) && unit.IsPlayer)
			{
				// If target already has aura, increase duration to max 130% of initial duration
				caster.CastSpell(unit, PriestSpells.ANGELIC_FEATHER_AURA, true);
				At.SetDuration(0);
			}
	}
}