// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

// 63106 - Siphon Life @ Glyph of Siphon Life
[SpellScript(63106)]
public class spell_warlock_siphon_life : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleHit(int effIndex)
	{
		var caster = Caster;
		var heal = caster.SpellHealingBonusDone(caster, SpellInfo, caster.CountPctFromMaxHealth(SpellInfo.GetEffect(effIndex).BasePoints), DamageEffectType.Heal, EffectInfo, 1, Spell);
		heal /= 100; // 0.5%
		heal = caster.SpellHealingBonusTaken(caster, SpellInfo, heal, DamageEffectType.Heal);
		HitHeal = (int)heal;
		PreventHitDefaultEffect(effIndex);
	}
}