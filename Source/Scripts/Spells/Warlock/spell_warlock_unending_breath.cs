// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

// 5697 - Unending Breath
[SpellScript(5697)]
internal class spell_warlock_unending_breath : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.ApplyAura, SpellScriptHookType.LaunchTarget));
	}

	private void HandleHit(int effIndex)
	{
		PreventHitDefaultEffect(effIndex);
		var caster = Caster;
		var target = HitUnit;

		if (target != null)
			if (caster.HasAura(WarlockSpells.SOULBURN))
				caster.CastSpell(target, WarlockSpells.SOULBURN_UNENDING_BREATH, true);
	}
}