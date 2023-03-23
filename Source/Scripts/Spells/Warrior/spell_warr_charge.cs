﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warrior;

[SpellScript(100)] // 100 - Charge
internal class spell_warr_charge : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		var spellId = WarriorSpells.CHARGE_EFFECT;

		if (Caster.HasAura(WarriorSpells.GLYPH_OF_THE_BLAZING_TRAIL))
			spellId = WarriorSpells.CHARGE_EFFECT_BLAZING_TRAIL;

		Caster.CastSpell(HitUnit, spellId, true);
	}
}