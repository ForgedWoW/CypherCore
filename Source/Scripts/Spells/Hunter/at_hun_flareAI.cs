// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Hunter;

[Script]
public class at_hun_flareAI : AreaTriggerScript, IAreaTriggerOnCreate
{
	public void OnCreate()
	{
		var caster = At.GetCaster();

		if (caster == null)
			return;

		if (caster.TypeId != TypeId.Player)
			return;

		var tempSumm = caster.SummonCreature(SharedConst.WorldTrigger, At.Location, TempSummonType.TimedDespawn, TimeSpan.FromSeconds(200));

		if (tempSumm == null)
		{
			tempSumm.Faction = caster.Faction;
			tempSumm.SetSummonerGUID(caster.GUID);
			PhasingHandler.InheritPhaseShift(tempSumm, caster);
			caster.CastSpell(tempSumm, HunterSpells.FLARE_EFFECT, true);
		}
	}
}