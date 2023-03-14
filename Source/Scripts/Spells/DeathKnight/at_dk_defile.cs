// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.DeathKnight;

[Script]
public class at_dk_defile : AreaTriggerScript, IAreaTriggerOnUnitEnter, IAreaTriggerOnUnitExit, IAreaTriggerOnCreate
{
	public void OnCreate()
	{
		At.GetCaster().CastSpell(At.Location, DeathKnightSpells.SUMMON_DEFILE, true);
	}

	public void OnUnitEnter(Unit unit)
	{
		var caster = At.GetCaster();

		if (caster != null)
			caster.CastSpell(unit, DeathKnightSpells.DEFILE_DUMMY, true);
	}

	public void OnUnitExit(Unit unit)
	{
		unit.RemoveAura(DeathKnightSpells.DEFILE_DUMMY);
	}
}