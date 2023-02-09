﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 28720 - Nightmare Vine
internal class spell_gen_nightmare_vine : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(GenericSpellIds.NightmarePollen);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(uint effIndex)
	{
		PreventHitDefaultEffect(effIndex);
		Unit target = GetHitUnit();

		if (target)
			// 25% chance of casting Nightmare Pollen
			if (RandomHelper.randChance(25))
				target.CastSpell(target, GenericSpellIds.NightmarePollen, true);
	}
}