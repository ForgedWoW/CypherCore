// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[Script] // 22568 - Ferocious Bite
internal class spell_dru_ferocious_bite : SpellScript, IHasSpellEffects
{
	private double _damageMultiplier = 0.0f;
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleLaunchTarget, 1, SpellEffectName.PowerBurn, SpellScriptHookType.LaunchTarget));
		SpellEffects.Add(new EffectHandler(HandleHitTargetBurn, 1, SpellEffectName.PowerBurn, SpellScriptHookType.EffectHitTarget));
		SpellEffects.Add(new EffectHandler(HandleHitTargetDmg, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleHitTargetBurn(int effIndex)
	{
		var newValue = (int)((double)EffectValue * _damageMultiplier);
		EffectValue = newValue;
	}

	private void HandleHitTargetDmg(int effIndex)
	{
		var newValue = (int)((double)HitDamage * (1.0f + _damageMultiplier));
		HitDamage = newValue;
	}

	private void HandleLaunchTarget(int effIndex)
	{
		var caster = Caster;

		var maxExtraConsumedPower = EffectValue;

		var auraEffect = caster.GetAuraEffect(DruidSpellIds.IncarnationKingOfTheJungle, 1);

		if (auraEffect != null)
		{
			var multiplier = 1.0f + (double)auraEffect.Amount / 100.0f;
			maxExtraConsumedPower = (int)((double)maxExtraConsumedPower * multiplier);
			EffectValue = maxExtraConsumedPower;
		}

		_damageMultiplier = Math.Min(caster.GetPower(PowerType.Energy), maxExtraConsumedPower) / maxExtraConsumedPower;
	}
}