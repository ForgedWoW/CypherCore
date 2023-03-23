// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(116095)]
public class spell_monk_disable : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(OnHitTarget, 0, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHitTarget));
	}

	private void OnHitTarget(int effIndex)
	{
		var target = ExplTargetUnit;

		if (target != null)
			if (target.HasAuraType(AuraType.ModDecreaseSpeed))
				Caster.CastSpell(target, MonkSpells.DISABLE_ROOT, true);
	}
}