﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[SpellScript(208244)]
public class spell_rog_roll_the_bones_visual_SpellScript : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(Prevent, (byte)255, SpellEffectName.Any, SpellScriptHookType.EffectHitTarget));
	}


	private void Prevent(int effIndex)
	{
		var caster = Caster;

		if (caster == null)
			return;

		if (caster.AsPlayer)
		{
			PreventHitAura();
			PreventHitDamage();
			PreventHitDefaultEffect(effIndex);
			PreventHitEffect(effIndex);
		}
	}
}