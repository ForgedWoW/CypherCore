// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman;

// 51490 - Thunderstorm
[SpellScript(51490)]
public class spell_sha_thunderstorm : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleKnockBack, 1, SpellEffectName.KnockBack, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleKnockBack(int effIndex)
	{
		// Glyph of Thunderstorm
		if (Caster.HasAura(ShamanSpells.GLYPH_OF_THUNDERSTORM))
			PreventHitDefaultEffect(effIndex);
	}
}