// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Hunter;

[Script]
public class at_hun_freezing_trapAI : AreaTriggerScript, IAreaTriggerOnCreate, IAreaTriggerOnUnitEnter
{
	public enum UsedSpells
	{
		FREEZING_TRAP_STUN = 3355
	}

	public int timeInterval;

	public void OnCreate()
    {
        timeInterval = 200;
        var caster = At.GetCaster();

		if (caster == null)
			return;

		if (!caster.AsPlayer)
			return;

		foreach (var itr in At.InsideUnits)
		{
			var target = ObjectAccessor.Instance.GetUnit(caster, itr);

			if (!caster.IsFriendlyTo(target))
			{
				caster.CastSpell(target, UsedSpells.FREEZING_TRAP_STUN, true);
				At.Remove();

				return;
			}
		}
	}

	public void OnUnitEnter(Unit unit)
	{
		var caster = At.GetCaster();

		if (caster == null || unit == null)
			return;

		if (!caster.AsPlayer)
			return;

		if (!caster.IsFriendlyTo(unit))
		{
			caster.CastSpell(unit, UsedSpells.FREEZING_TRAP_STUN, true);
			At.Remove();

			return;
		}
	}
}