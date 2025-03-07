﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(115151)]
public class spell_monk_renewing_mist : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.Launch));
	}

	private void HandleDummy(int effIndex)
	{
		PreventHitDefaultEffect(effIndex);
		var target = ExplTargetUnit;

		if (target != null)
			Caster.CastSpell(target, MonkSpells.RENEWING_MIST_HOT, true);
	}
}