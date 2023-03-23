// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(203704)]
public class spell_demon_hunter_mana_break : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public void HandleHit(int effIndex)
	{
		var caster = Caster;
		var target = HitUnit;

		if (caster == null || target == null)
			return;

		var damage = (double)SpellInfo.GetEffect(1).BasePoints;
		var powerPct = target.GetPowerPct(PowerType.Mana);

		if (powerPct >= 1.0f)
			damage += (100.0f - powerPct) / 10.0f * SpellInfo.GetEffect(2).BasePoints;

		damage = Math.Max((double)HitUnit.CountPctFromMaxHealth(SpellInfo.GetEffect(1).BasePoints), (double)damage);

		HitDamage = damage;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}
}