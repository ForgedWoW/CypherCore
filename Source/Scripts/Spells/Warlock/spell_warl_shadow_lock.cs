﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

// Shadow Lock - 171140
[SpellScript(171140)]
public class spell_warl_shadow_lock : SpellScript, ISpellCheckCast, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public SpellCastResult CheckCast()
	{
		var caster = Caster;
		var pet = caster.GetGuardianPet();

		if (caster == null || pet == null)
			return SpellCastResult.DontReport;

		if (pet.SpellHistory.HasCooldown(WarlockSpells.DOOMGUARD_SHADOW_LOCK))
			return SpellCastResult.CantDoThatRightNow;

		return SpellCastResult.SpellCastOk;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleHit(int effIndex)
	{
		var caster = Caster;
		var target = HitUnit;
		var pet = caster.GetGuardianPet();

		if (caster == null || pet == null || target == null)
			return;

		/*if (pet->GetEntry() != PET_ENTRY_DOOMGUARD)
			return;*/

		pet.CastSpell(target, WarlockSpells.DOOMGUARD_SHADOW_LOCK, true);

		caster.AsPlayer.SpellHistory.ModifyCooldown(SpellInfo.Id, TimeSpan.FromSeconds(24));
	}
}