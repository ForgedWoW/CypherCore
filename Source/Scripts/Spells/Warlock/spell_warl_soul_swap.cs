// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

[SpellScript(86121)] // 86121 - Soul Swap
internal class spell_warl_soul_swap : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleHit(int effIndex)
	{
		Caster.CastSpell(Caster, WarlockSpells.SOUL_SWAP_OVERRIDE, true);
		HitUnit.CastSpell(Caster, WarlockSpells.SOUL_SWAP_OVERRIDE, true);
	}
}