// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Warrior;

// 202603 Into the Fray
// MiscId - 5839
[Script]
public class at_into_the_fray : AreaTriggerScript, IAreaTriggerOnUpdate
{
	public void OnUpdate(uint diff)
	{
		var caster = At.GetCaster();

		if (caster == null)
			return;

		var timer = At.VariableStorage.GetValue<uint>("_timer", 0) + diff;

		if (timer >= 250)
		{
			At.VariableStorage.Set<int>("_timer", 0);
			var count = (uint)(At.InsideUnits.Count - 1);

			if (count != 0)
			{
				if (!caster.HasAura(WarriorSpells.INTO_THE_FRAY))
					caster.CastSpell(caster, WarriorSpells.INTO_THE_FRAY, true);

				var itf = caster.GetAura(WarriorSpells.INTO_THE_FRAY);

				if (itf != null)
					itf.SetStackAmount((byte)Math.Min(itf.CalcMaxStackAmount(), count));
			}
			else
			{
				caster.RemoveAura(WarriorSpells.INTO_THE_FRAY);
			}
		}
		else
		{
			At.VariableStorage.Set("_timer", timer);
		}
	}
}