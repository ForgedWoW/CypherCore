// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Monk;

[Script]
public class at_monk_gift_of_the_ox_sphere : AreaTriggerScript, IAreaTriggerOnCreate, IAreaTriggerOnUpdate, IAreaTriggerOnUnitEnter, IAreaTriggerOnRemove
{
	public enum SpellsUsed
	{
		GIFT_OF_THE_OX_HEAL = 178173,
		HEALING_SPHERE_COOLDOWN = 224863
	}

	public uint pickupDelay;

	public void OnCreate()
	{
		pickupDelay = 1000;
	}

	public void OnUpdate(uint diff)
	{
		if (pickupDelay >= diff)
			pickupDelay -= diff;
		else
			pickupDelay = 0;
	}

	public void OnUnitEnter(Unit unit)
	{
		var caster = At.GetCaster();

		if (caster != null)
			if (unit == caster && pickupDelay == 0)
			{
				caster.CastSpell(caster, SpellsUsed.GIFT_OF_THE_OX_HEAL, true);
				At.Remove();
			}
	}

	public void OnRemove()
	{
		//Todo : Remove cooldown
		var caster = At.GetCaster();

		if (caster != null)
			if (caster.HasAura(SpellsUsed.HEALING_SPHERE_COOLDOWN))
				caster.RemoveAura(SpellsUsed.HEALING_SPHERE_COOLDOWN);
	}
}