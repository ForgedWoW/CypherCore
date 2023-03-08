﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Hunter;

[Script]
public class at_hun_caltropsAI : AreaTriggerAI
{
	public enum UsedSpells
	{
		CALTROPS_AURA = 194279
	}

	public int timeInterval;

	public at_hun_caltropsAI(AreaTrigger areatrigger) : base(areatrigger)
	{
		// How often should the action be executed
		timeInterval = 1000;
	}

	public override void OnUpdate(uint p_Time)
	{
		var caster = at.GetCaster();

		if (caster == null)
			return;

		if (caster.TypeId != TypeId.Player)
			return;

		// Check if we can handle actions
		timeInterval += (int)p_Time;

		if (timeInterval < 1000)
			return;

		foreach (var guid in at.InsideUnits)
		{
			var unit = ObjectAccessor.Instance.GetUnit(caster, guid);

			if (unit != null)
				if (!caster.IsFriendlyTo(unit))
					caster.CastSpell(unit, UsedSpells.CALTROPS_AURA, true);
		}

		timeInterval -= 1000;
	}
}