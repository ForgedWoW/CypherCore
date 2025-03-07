﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Items;

[Script] // 40971 - Bonus Healing (Crystal Spire of Karabor)
internal class spell_item_crystal_spire_of_karabor : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		var pct = SpellInfo.GetEffect(0).CalcValue();
		var healInfo = eventInfo.HealInfo;

		if (healInfo != null)
		{
			var healTarget = healInfo.Target;

			if (healTarget)
				if (healTarget.Health - healInfo.EffectiveHeal <= healTarget.CountPctFromMaxHealth(pct))
					return true;
		}

		return false;
	}
}