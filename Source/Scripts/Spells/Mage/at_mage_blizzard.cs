// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Mage;

[Script]
public class at_mage_blizzard : AreaTriggerAI
{
	public int timeInterval;

	public at_mage_blizzard(AreaTrigger areatrigger) : base(areatrigger)
	{
		timeInterval = 1000;
	}

	public override void OnCreate()
	{
		at.SetDuration(8000);
	}

	public override void OnUpdate(uint diff)
	{
		var caster = at.GetCaster();

		if (caster == null)
			return;

		if (!caster.IsPlayer)
			return;

		timeInterval += (int)diff;

		if (timeInterval < 1000)
			return;

		var tempSumm = caster.SummonCreature(12999, at.Location, TempSummonType.TimedDespawn, TimeSpan.FromSeconds(8100));

		{
			tempSumm.Faction = caster.Faction;
			tempSumm.SetSummonerGUID(caster.GUID);
			PhasingHandler.InheritPhaseShift(tempSumm, caster);
			caster.CastSpell(tempSumm, UsingSpells.BLIZZARD_DAMAGE, true);
		}

		timeInterval -= 1000;
	}

	public struct UsingSpells
	{
		public const uint BLIZZARD_DAMAGE = 190357;
	}
}