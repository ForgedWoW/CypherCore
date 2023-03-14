// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Shaman;

//AT ID : 6336
//Spell ID : 207495
[Script]
public class at_sha_ancestral_protection_totem : AreaTriggerScript, IAreaTriggerOnCreate, IAreaTriggerOnUnitEnter, IAreaTriggerOnUnitExit, IAreaTriggerOnRemove
{
	public int timeInterval;

	public void OnCreate()
	{
		var caster = At.GetCaster();

		if (caster == null)
			return;

		foreach (var itr in At.InsideUnits)
		{
			var target = ObjectAccessor.Instance.GetUnit(caster, itr);

			if (caster.IsFriendlyTo(target) || target == caster.OwnerUnit)
				if (!target.IsTotem)
					caster.CastSpell(target, SpellsUsed.ANCESTRAL_PROTECTION_TOTEM_AURA, true);
		}
	}

	public void OnRemove()
	{
		var caster = At.GetCaster();

		if (caster == null)
			return;

		foreach (var itr in At.InsideUnits)
		{
			var target = ObjectAccessor.Instance.GetUnit(caster, itr);

			if (!target.IsTotem)
				if (target.HasAura(SpellsUsed.ANCESTRAL_PROTECTION_TOTEM_AURA))
					target.RemoveAura(SpellsUsed.ANCESTRAL_PROTECTION_TOTEM_AURA);
		}
	}

	public void OnUnitEnter(Unit unit)
	{
		var caster = At.GetCaster();

		if (caster == null || unit == null)
			return;

		if (caster.IsFriendlyTo(unit) || unit == caster.OwnerUnit)
		{
			if (unit.IsTotem)
				return;
			else
				caster.CastSpell(unit, SpellsUsed.ANCESTRAL_PROTECTION_TOTEM_AURA, true);
		}
	}

	public void OnUnitExit(Unit unit)
	{
		var caster = At.GetCaster();

		if (caster == null || unit == null)
			return;

		if (unit.HasAura(SpellsUsed.ANCESTRAL_PROTECTION_TOTEM_AURA) && unit.GetAura(SpellsUsed.ANCESTRAL_PROTECTION_TOTEM_AURA).Caster == caster)
			unit.RemoveAura(SpellsUsed.ANCESTRAL_PROTECTION_TOTEM_AURA);
	}

	public struct SpellsUsed
	{
		public const uint ANCESTRAL_PROTECTION_TOTEM_AURA = 207498;
	}
}