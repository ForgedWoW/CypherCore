﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Priest;

[SpellScript(33110)]
public class spell_pri_prayer_of_mending_heal : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleHeal(uint UnnamedParameter)
	{
		var caster = GetOriginalCaster();

		if (caster != null)
		{
			var aurEff = caster.GetAuraEffect(PriestSpells.SPELL_PRIEST_T9_HEALING_2P, 0);

			if (aurEff != null)
			{
				var heal = GetHitHeal();
				MathFunctions.AddPct(ref heal, aurEff.GetAmount());
				SetHitHeal(heal);
			}
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHeal, 0, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));
	}
}