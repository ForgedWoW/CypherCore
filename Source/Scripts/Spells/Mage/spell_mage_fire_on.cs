﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[SpellScript(205029)]
public class spell_mage_fire_on : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 2, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		var caster = Caster;
		var target = HitUnit;

		if (caster == null || target == null || caster.TypeId != TypeId.Player)
			return;

		caster.AsPlayer.SpellHistory.ResetCharges(Global.SpellMgr.GetSpellInfo(MageSpells.FireBlast, Difficulty.None).ChargeCategoryId);
		// caster->ToPlayer()->GetSpellHistory()->ResetCharges(Global.SpellMgr->GetSpellInfo(FIRE_BLAST, Difficulty.None)->ChargeCategoryId);
	}
}