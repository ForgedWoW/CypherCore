// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[Script] // 208673 - Sigil of Chains
internal class spell_dh_sigil_of_chains : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleEffectHitTarget, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleEffectHitTarget(int effIndex)
	{
		var loc = ExplTargetDest;

		if (loc != null)
		{
			Caster.CastSpell(HitUnit, DemonHunterSpells.SigilOfChainsSlow, new CastSpellExtraArgs(true));
			HitUnit.CastSpell(loc, DemonHunterSpells.SigilOfChainsGrip, new CastSpellExtraArgs(true));
		}
	}
}