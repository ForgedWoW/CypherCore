// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Shaman;

//AT ID : 5760
//Spell ID : 198839
[Script]
public class at_earthen_shield_totem : AreaTriggerScript, IAreaTriggerOnCreate, IAreaTriggerOnUnitEnter, IAreaTriggerOnUnitExit, IAreaTriggerOnRemove
{
	public int timeInterval;

	public void OnCreate()
    {
        timeInterval = 200;
        var caster = At.GetCaster();

		if (caster == null)
			return;

		foreach (var itr in At.InsideUnits)
		{
			var target = ObjectAccessor.Instance.GetUnit(caster, itr);

			if (caster.IsFriendlyTo(target) || target == caster.OwnerUnit)
				if (!target.IsTotem)
					caster.CastSpell(target, SpellsUsed.EARTHEN_SHIELD_ABSORB, true);
		}
	}

	public void OnUnitEnter(Unit unit)
	{
		var caster = At.GetCaster();

		if (caster == null || unit == null)
			return;

		if (unit.IsTotem)
			return;

		if (caster.IsFriendlyTo(unit) || unit == caster.OwnerUnit)
			caster.CastSpell(unit, SpellsUsed.EARTHEN_SHIELD_ABSORB, true);
	}

	public void OnUnitExit(Unit unit)
	{
		var caster = At.GetCaster();

		if (caster == null || unit == null)
			return;

		if (unit.IsTotem)
			return;

		if (unit.HasAura(SpellsUsed.EARTHEN_SHIELD_ABSORB) && unit.GetAura(SpellsUsed.EARTHEN_SHIELD_ABSORB).Caster == caster)
			unit.RemoveAura(SpellsUsed.EARTHEN_SHIELD_ABSORB);
	}

	public void OnRemove()
	{
		var caster = At.GetCaster();

		if (caster == null)
			return;

		foreach (var itr in At.InsideUnits)
		{
			var target = ObjectAccessor.Instance.GetUnit(caster, itr);

			if (target != null)
				if (!target.IsTotem)
					if (target.HasAura(SpellsUsed.EARTHEN_SHIELD_ABSORB) && target.GetAura(SpellsUsed.EARTHEN_SHIELD_ABSORB).Caster == caster)
						target.RemoveAura(SpellsUsed.EARTHEN_SHIELD_ABSORB);
		}
	}

	public struct SpellsUsed
	{
		public const uint EARTHEN_SHIELD_ABSORB = 201633;
	}
}