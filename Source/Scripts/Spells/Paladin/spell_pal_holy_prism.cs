// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Paladin;

[SpellScript(114165)] // 114165 - Holy Prism
internal class spell_pal_holy_prism : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		if (Caster.IsFriendlyTo(HitUnit))
			Caster.CastSpell(HitUnit, PaladinSpells.HolyPrismTargetAlly, true);
		else
			Caster.CastSpell(HitUnit, PaladinSpells.HolyPrismTargetEnemy, true);

		Caster.CastSpell(HitUnit, PaladinSpells.HolyPrismTargetBeamVisual, true);
	}
}