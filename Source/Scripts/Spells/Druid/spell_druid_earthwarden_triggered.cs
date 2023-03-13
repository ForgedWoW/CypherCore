// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Models;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(203975)]
public class spell_druid_earthwarden_triggered : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(Spells.EARTHWARDEN, Spells.EARTHWARDEN_TRIGGERED);
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
		if (dmgInfo.DamageType == DamageEffectType.Direct)
		{
			var earthwarden = Global.SpellMgr.AssertSpellInfo(Spells.EARTHWARDEN, Difficulty.None);

			absorbAmount = MathFunctions.CalculatePct(dmgInfo.Damage, earthwarden.GetEffect(0).BasePoints);
			Caster.RemoveAura(Spells.EARTHWARDEN_TRIGGERED);
		}

		return absorbAmount;
	}

	private struct Spells
	{
		public static readonly uint EARTHWARDEN = 203974;
		public static readonly uint EARTHWARDEN_TRIGGERED = 203975;
	}
}