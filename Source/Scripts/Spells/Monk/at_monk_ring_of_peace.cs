// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Monk;

[Script]
public class at_monk_ring_of_peace : AreaTriggerScript, IAreaTriggerOnUnitEnter
{
	public void OnUnitEnter(Unit target)
	{
		if (At.GetCaster())
			if (At.GetCaster().IsValidAttackTarget(target))
				target.CastSpell(target, MonkSpells.RING_OF_PEACE_KNOCKBACK, true);
	}
}