// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Hunter;

[Script]
public class at_hun_tar_trap_not_activatedAI : AreaTriggerAI
{
	public enum UsedSpells
	{
		ACTIVATE_TAR_TRAP = 187700
	}

	public int timeInterval;

	public at_hun_tar_trap_not_activatedAI(AreaTrigger areatrigger) : base(areatrigger)
	{
		timeInterval = 200;
	}

	public override void OnCreate()
	{
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

	public override void OnUnitEnter(Unit unit)
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