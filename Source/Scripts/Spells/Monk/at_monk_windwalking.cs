﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Monk;

[Script]
public class at_monk_windwalking : AreaTriggerAI
{
	public at_monk_windwalking(AreaTrigger areatrigger) : base(areatrigger) { }

	public override void OnUnitEnter(Unit unit)
	{
		var caster = at.GetCaster();

		if (caster == null || unit == null)
			return;

		if (!caster.ToPlayer())
			return;

		var aur = unit.GetAura(MonkSpells.WINDWALKER_AURA);

		if (aur != null)
			aur.SetDuration(-1);
		else if (caster.IsFriendlyTo(unit))
			caster.CastSpell(unit, MonkSpells.WINDWALKER_AURA, true);
	}

	public override void OnUnitExit(Unit unit)
	{
		var caster = at.GetCaster();

		if (caster == null || unit == null)
			return;

		if (!caster.ToPlayer())
			return;

		if (unit.HasAura(MonkSpells.WINDWALKING) && unit != caster) // Don't remove from other WW monks.
			return;

		var aur = unit.GetAura(MonkSpells.WINDWALKER_AURA, caster.GUID);

		if (aur != null)
		{
			aur.SetMaxDuration(10 * Time.InMilliseconds);
			aur.SetDuration(10 * Time.InMilliseconds);
		}
	}

	public override void OnRemove()
	{
		var caster = at.GetCaster();

		if (caster == null)
			return;

		if (!caster.ToPlayer())
			return;

		foreach (var guid in at.InsideUnits)
		{
			var unit = ObjectAccessor.Instance.GetUnit(caster, guid);

			if (unit != null)
			{
				if (unit.HasAura(MonkSpells.WINDWALKING) && unit != caster) // Don't remove from other WW monks.
					continue;

				var aur = unit.GetAura(MonkSpells.WINDWALKER_AURA, caster.GUID);

				if (aur != null)
				{
					aur.SetMaxDuration(10 * Time.InMilliseconds);
					aur.SetDuration(10 * Time.InMilliseconds);
				}
			}
		}
	}
}