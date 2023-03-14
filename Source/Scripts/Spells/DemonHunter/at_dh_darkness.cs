// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.DemonHunter;

[Script]
public class at_dh_darkness : AreaTriggerScript, IAreaTriggerOnUnitEnter, IAreaTriggerOnUnitExit, IAreaTriggerOnInitialize
{
	private bool entered;

	public void OnInitialize()
	{
		At.SetDuration(8000);
	}

	public void OnUnitEnter(Unit unit)
	{
		var caster = At.GetCaster();

		if (caster == null || unit == null)
			return;

		if (caster.IsFriendlyTo(unit) && !unit.HasAura(DemonHunterSpells.DARKNESS_ABSORB))
		{
			entered = true;

			if (entered)
			{
				caster.CastSpell(unit, DemonHunterSpells.DARKNESS_ABSORB, true);
				entered = false;
			}
		}
	}

	public void OnUnitExit(Unit unit)
	{
		var caster = At.GetCaster();

		if (caster == null || unit == null)
			return;

		if (unit.HasAura(DemonHunterSpells.DARKNESS_ABSORB))
			unit.RemoveAurasDueToSpell(DemonHunterSpells.DARKNESS_ABSORB, caster.GUID);
	}
}