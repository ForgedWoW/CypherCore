// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(124081)]
public class spell_monk_zen_pulse : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(OnHit, 1, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}

	private void OnHit(int effIndex)
	{
		Caster.CastSpell(Caster, MonkSpells.ZEN_PULSE_HEAL, true);
	}
}