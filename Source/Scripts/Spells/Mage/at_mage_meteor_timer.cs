// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Mage;

[Script]
public class at_mage_meteor_timer : AreaTriggerScript, IAreaTriggerOnCreate, IAreaTriggerOnRemove
{
	public void OnCreate()
	{
		var caster = At.GetCaster();

		if (caster == null)
			return;

		var tempSumm = caster.SummonCreature(12999, At.Location, TempSummonType.TimedDespawn, TimeSpan.FromSeconds(5));

		if (tempSumm != null)
		{
			tempSumm.Faction = caster.Faction;
			tempSumm.SetSummonerGUID(caster.GUID);
			PhasingHandler.InheritPhaseShift(tempSumm, caster);
			caster.CastSpell(tempSumm, MageSpells.METEOR_VISUAL, true);
		}
	}

	public void OnRemove()
	{
		var caster = At.GetCaster();

		if (caster == null)
			return;

		var tempSumm = caster.SummonCreature(12999, At.Location, TempSummonType.TimedDespawn, TimeSpan.FromSeconds(5));

		if (tempSumm != null)
		{
			tempSumm.Faction = caster.Faction;
			tempSumm.SetSummonerGUID(caster.GUID);
			PhasingHandler.InheritPhaseShift(tempSumm, caster);
			caster.CastSpell(tempSumm, MageSpells.METEOR_DAMAGE, true);
		}
	}
}