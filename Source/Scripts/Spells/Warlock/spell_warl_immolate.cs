// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

[SpellScript(348)] // 348 - Immolate
internal class spell_warl_immolate : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleOnEffectHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleOnEffectHit(int effIndex)
	{
		Caster.CastSpell(HitUnit, WarlockSpells.IMMOLATE_DOT, Spell);
	}
}