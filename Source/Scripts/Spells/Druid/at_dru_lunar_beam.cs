// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Druid;

[Script]
public class at_dru_lunar_beam : AreaTriggerScript, IAreaTriggerOnCreate, IAreaTriggerOnPeriodicProc
{
	public void OnCreate()
	{
		At.SetPeriodicProcTimer(1000);
	}

	public void OnPeriodicProc()
	{
		if (At.GetCaster())
			At.GetCaster().CastSpell(At.Location, DruidSpells.LUNAR_BEAM_DAMAGE_HEAL, true);
	}
}