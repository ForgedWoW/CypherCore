﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(201469)]
public class spell_dh_artifact_demon_speed : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		var caster = Caster;

		if (caster == null)
			return;

		var aurEff = caster.GetAuraEffect(DemonHunterSpells.DEMON_SPEED, 0);

		if (aurEff != null)
			for (byte i = 0; i < aurEff.Amount; ++i)
				caster.SpellHistory.RestoreCharge(Global.SpellMgr.GetSpellInfo(DemonHunterSpells.FEL_RUSH, Difficulty.None).ChargeCategoryId);
	}
}