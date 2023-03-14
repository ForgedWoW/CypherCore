// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Hunter;

[Script]
public class at_hun_tar_trap_not_activatedAI : AreaTriggerScript, IAreaTriggerOnCreate, IAreaTriggerOnUnitEnter
{
	public enum UsedSpells
	{
		ACTIVATE_TAR_TRAP = 187700
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
				var tempSumm = caster.SummonCreature(SharedConst.WorldTrigger, At.Location, TempSummonType.TimedDespawn, TimeSpan.FromMinutes(1));

				if (tempSumm != null)
				{
					tempSumm.Faction = caster.Faction;
					tempSumm.SetSummonerGUID(caster.GUID);
					PhasingHandler.InheritPhaseShift(tempSumm, caster);
					caster.CastSpell(tempSumm, UsedSpells.ACTIVATE_TAR_TRAP, true);
					At.Remove();
				}
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
			var tempSumm = caster.SummonCreature(SharedConst.WorldTrigger, At.Location, TempSummonType.TimedDespawn, TimeSpan.FromMinutes(1));

			if (tempSumm != null)
			{
				tempSumm.Faction = caster.Faction;
				tempSumm.SetSummonerGUID(caster.GUID);
				PhasingHandler.InheritPhaseShift(tempSumm, caster);
				caster.CastSpell(tempSumm, UsedSpells.ACTIVATE_TAR_TRAP, true);
				At.Remove();
			}
		}
	}
}