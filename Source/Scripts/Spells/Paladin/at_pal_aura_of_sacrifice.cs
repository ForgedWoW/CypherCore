// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Paladin;

// Aura of Sacrifice - 183416
// AreaTriggerID - 100102 (custom)
[Script]
public class at_pal_aura_of_sacrifice : AreaTriggerAI
{
	public at_pal_aura_of_sacrifice(AreaTrigger areatrigger) : base(areatrigger)
	{
		At.SetPeriodicProcTimer(1000);
	}

	public override void OnUnitEnter(Unit unit)
	{
		var caster = At.GetCaster();

		if (caster != null)
			if (unit.IsPlayer && caster.IsPlayer && caster != unit)
				if (caster.AsPlayer.IsInSameRaidWith(unit.AsPlayer))
					caster.CastSpell(unit, PaladinSpells.AURA_OF_SACRIFICE_ALLY, true);
	}

	public override void OnUnitExit(Unit unit)
	{
		unit.RemoveAura(PaladinSpells.AURA_OF_SACRIFICE_ALLY);
	}
}