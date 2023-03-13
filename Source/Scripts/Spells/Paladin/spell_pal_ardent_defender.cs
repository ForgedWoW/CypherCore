// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Models;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Paladin;

// 31850 - ardent defender
[SpellScript(31850)]
public class spell_pal_ardent_defender : AuraScript, IHasAuraEffects
{
	private double absorbPct;
	private double healPct;
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public spell_pal_ardent_defender()
	{
		absorbPct = 0;
		healPct = 0;
	}

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(PaladinSpells.ARDENT_DEFENDER);
	}

	public override bool Load()
	{
		absorbPct = SpellInfo.GetEffect(0).CalcValue();
		healPct = SpellInfo.GetEffect(1).CalcValue();

		return OwnerAsUnit.IsPlayer;
	}

	public void CalculateAmount(AuraEffect UnnamedParameter, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
	{
		amount.Value = -1;
	}

	public double Absorb(AuraEffect aurEff, DamageInfo dmgInfo, double absorbAmount)
	{
		absorbAmount = MathFunctions.CalculatePct(dmgInfo.Damage, absorbPct);

		var target = Target;

		if (dmgInfo.Damage < target.Health)
			return absorbAmount;

		double healAmount = target.CountPctFromMaxHealth(healPct);
		target.CastSpell(target, PaladinSpells.ARDENT_DEFENDER_HEAL, (int)healAmount);
		aurEff.Base.Remove();

		return absorbAmount;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 1, AuraType.SchoolAbsorb));
		AuraEffects.Add(new AuraEffectAbsorbHandler(Absorb, 1));
	}
}