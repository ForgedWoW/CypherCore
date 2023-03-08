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

[SpellScript(204053)]
public class spell_druid_rend_and_tear : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(Spells.REND_AND_TEAR, Spells.TRASH_DOT);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
		AuraEffects.Add(new AuraEffectAbsorbHandler(Absorb, 0));
		AuraEffects.Add(new AuraEffectCalcSpellModHandler(HandleEffectCalcSpellMod, 1, AuraType.AddFlatModifier));
		AuraEffects.Add(new AuraEffectCalcSpellModHandler(HandleEffectCalcSpellMod, 2, AuraType.AddFlatModifier));
	}

	private void CalculateAmount(AuraEffect UnnamedParameter, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
	{
		amount.Value = -1;
	}

	private double Absorb(AuraEffect auraEffect, DamageInfo dmgInfo, double absorbAmount)
	{
		var caster = Caster;
		var attacker = dmgInfo.GetAttacker();
		absorbAmount = 0;

		if (caster == null || attacker == null || !HasEffect(1))
			return absorbAmount;

		if (caster.GetShapeshiftForm() == ShapeShiftForm.BearForm)
		{
			var trashDOT = attacker.GetAura(Spells.TRASH_DOT, caster.GetGUID());

			if (trashDOT != null)
				absorbAmount = MathFunctions.CalculatePct(dmgInfo.GetDamage(), trashDOT.StackAmount * SpellInfo.GetEffect(1).BasePoints);
		}

		return absorbAmount;
	}

	private void HandleEffectCalcSpellMod(AuraEffect aurEff, SpellModifier spellMod)
	{
		if (spellMod == null)
			return;

		((SpellModifierByClassMask)spellMod).Value = Caster.GetShapeshiftForm() == ShapeShiftForm.BearForm ? aurEff.Amount : 0;
	}

	private struct Spells
	{
		public static readonly uint REND_AND_TEAR = 204053;
		public static readonly uint TRASH_DOT = 192090;
	}
}