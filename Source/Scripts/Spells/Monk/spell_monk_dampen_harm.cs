// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Models;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(122278)]
public class spell_monk_dampen_harm : AuraScript, IHasAuraEffects
{
	private double healthPct;
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Load()
	{
		healthPct = SpellInfo.GetEffect(0).CalcValue(Caster);

		return OwnerAsUnit.AsPlayer;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
		AuraEffects.Add(new AuraEffectAbsorbHandler(Absorb, 0));
	}

	private void CalculateAmount(AuraEffect UnnamedParameter, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
	{
		amount.Value = -1;
	}

	private double Absorb(AuraEffect auraEffect, DamageInfo dmgInfo, double absorbAmount)
	{
		var target = Target;
		var health = target.CountPctFromMaxHealth(healthPct);

		if (dmgInfo.GetDamage() < health)
			return absorbAmount;

		absorbAmount = dmgInfo.GetDamage() * (SpellInfo.GetEffect(0).CalcValue(Caster) / 100);
		auraEffect.Base.DropCharge();

		return absorbAmount;
	}
}