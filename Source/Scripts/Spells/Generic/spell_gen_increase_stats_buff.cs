﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script("spell_pal_blessing_of_kings")]
[Script("spell_pal_blessing_of_might")]
[Script("spell_dru_mark_of_the_wild")]
[Script("spell_pri_power_word_fortitude")]
[Script("spell_pri_shadow_protection")]
internal class spell_gen_increase_stats_buff : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		if (HitUnit.IsInRaidWith(Caster))
			Caster.CastSpell(Caster, (uint)EffectValue + 1, true); // raid buff
		else
			Caster.CastSpell(HitUnit, (uint)EffectValue, true); // single-Target buff
	}
}