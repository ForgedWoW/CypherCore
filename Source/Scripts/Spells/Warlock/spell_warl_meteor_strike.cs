// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

// Meteor Strike - 171152
[SpellScript(171152)]
public class spell_warl_meteor_strike : SpellScript, ISpellAfterHit, ISpellCheckCast
{
	public void AfterHit()
	{
		var caster = Caster;
		var pet = caster.GetGuardianPet();

		if (caster == null || pet == null)
			return;

		/*if (pet->GetEntry() != PET_ENTRY_INFERNAL)
			return;*/

		pet.CastSpell(pet, WarlockSpells.INFERNAL_METEOR_STRIKE, true);

		caster.AsPlayer.
		SpellHistory.ModifyCooldown(SpellInfo.Id, TimeSpan.FromSeconds(60));
	}

	public SpellCastResult CheckCast()
	{
		var caster = Caster;
		var pet = caster.GetGuardianPet();

		if (caster == null || pet == null)
			return SpellCastResult.DontReport;

		if (pet.SpellHistory.HasCooldown(WarlockSpells.INFERNAL_METEOR_STRIKE))
			return SpellCastResult.CantDoThatRightNow;

		return SpellCastResult.SpellCastOk;
	}
}