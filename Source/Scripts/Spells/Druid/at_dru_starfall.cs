// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Druid;

[Script]
public class at_dru_starfall : AreaTriggerScript, IAreaTriggerOnCreate, IAreaTriggerOnPeriodicProc
{
	public int timeInterval;

	public void OnCreate()
	{
		// How often should the action be executed
		At.SetPeriodicProcTimer(850);
	}

	public void OnPeriodicProc()
	{
		var caster = At.GetCaster();

		if (caster != null)
			foreach (var objguid in At.InsideUnits)
			{
				var unit = ObjectAccessor.Instance.GetUnit(caster, objguid);

				if (unit != null)
					if (caster.IsValidAttackTarget(unit))
						if (unit.IsInCombat)
						{
							caster.CastSpell(unit, StarfallSpells.STARFALL_DAMAGE, true);
							caster.CastSpell(unit, StarfallSpells.STELLAR_EMPOWERMENT, true);
						}
			}
	}
}