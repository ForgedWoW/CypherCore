// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;
using Game.Spells;

namespace Scripts.Spells.Shaman;

//AT id : 3691
//Spell ID : 61882
[Script]
public class at_sha_earthquake_totem : AreaTriggerScript, IAreaTriggerOnCreate, IAreaTriggerOnUpdate
{
	public int timeInterval;

	public void OnCreate()
	{
		timeInterval = 200;
	}

	public void OnUpdate(uint p_Time)
	{
		var caster = At.GetCaster();

		if (caster == null)
			return;

		if (!caster.AsPlayer)
			return;

		// Check if we can handle actions
		timeInterval += (int)p_Time;

		if (timeInterval < 1000)
			return;

		var tempSumm = caster.SummonCreature(SharedConst.WorldTrigger, At.Location, TempSummonType.TimedDespawn, TimeSpan.FromMilliseconds(200));

		if (tempSumm != null)
		{
			tempSumm.Faction = caster.Faction;
			tempSumm.SetSummonerGUID(caster.GUID);
			PhasingHandler.InheritPhaseShift(tempSumm, caster);

			tempSumm.CastSpell(caster,
								UsedSpells.EARTHQUAKE_DAMAGE,
								new CastSpellExtraArgs(TriggerCastFlags.FullMask)
									.AddSpellMod(SpellValueMod.BasePoint0, (int)(caster.GetTotalSpellPowerValue(SpellSchoolMask.Normal, false) * 0.3)));
		}

		timeInterval -= 1000;
	}

	public struct UsedSpells
	{
		public const uint EARTHQUAKE_DAMAGE = 77478;
		public const uint EARTHQUAKE_STUN = 77505;
	}
}