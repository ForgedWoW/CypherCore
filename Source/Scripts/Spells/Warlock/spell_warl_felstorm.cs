// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

// Felstorm - 119914
[SpellScript(119914)]
public class spell_warl_felstorm : SpellScript, ISpellAfterHit, ISpellCheckCast
{
	public void AfterHit()
	{
		var caster = Caster;

		if (caster == null)
			return;

		caster.
		AsPlayer.GetSpellHistory().ModifyCooldown(SpellInfo.Id, TimeSpan.FromSeconds(45));
	}

	public SpellCastResult CheckCast()
	{
		var caster = Caster;
		var pet = caster.GetGuardianPet();

		if (caster == null || pet == null)
			return SpellCastResult.DontReport;

		if (pet.GetSpellHistory().HasCooldown(WarlockSpells.FELGUARD_FELSTORM))
			return SpellCastResult.CantDoThatRightNow;

		return SpellCastResult.SpellCastOk;
	}
}