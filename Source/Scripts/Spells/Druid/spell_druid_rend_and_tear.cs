﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(204053)]
public class spell_druid_rend_and_tear : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	private struct Spells
	{
		public static uint SPELL_DRUID_REND_AND_TEAR = 204053;
		public static uint SPELL_DRUID_TRASH_DOT = 192090;
	}

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(Spells.SPELL_DRUID_REND_AND_TEAR, Spells.SPELL_DRUID_TRASH_DOT);
	}

	private void CalculateAmount(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
	{
		amount = -1;
	}

	private void Absorb(AuraEffect auraEffect, DamageInfo dmgInfo, ref uint absorbAmount)
	{
		Unit caster   = GetCaster();
		Unit attacker = dmgInfo.GetAttacker();
		absorbAmount = 0;

		if (caster == null || attacker == null || !HasEffect(1))
		{
			return;
		}

		if (caster.GetShapeshiftForm() == ShapeShiftForm.BearForm)
		{
			Aura trashDOT = attacker.GetAura(Spells.SPELL_DRUID_TRASH_DOT, caster.GetGUID());
			if (trashDOT != null)
			{
				absorbAmount = MathFunctions.CalculatePct(dmgInfo.GetDamage(), trashDOT.GetStackAmount() * GetSpellInfo().GetEffect(1).BasePoints);
			}
		}
	}

	private void HandleEffectCalcSpellMod(AuraEffect aurEff, ref SpellModifier spellMod)
	{
		if (spellMod == null)
		{
			return;
		}

		((SpellModifierByClassMask)spellMod).value = GetCaster().GetShapeshiftForm() == ShapeShiftForm.BearForm ? aurEff.GetAmount() : 0;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
		AuraEffects.Add(new AuraEffectAbsorbHandler(Absorb, 0));
		AuraEffects.Add(new AuraEffectCalcSpellModHandler(HandleEffectCalcSpellMod,  1,  AuraType.AddFlatModifier));
		AuraEffects.Add(new AuraEffectCalcSpellModHandler(HandleEffectCalcSpellMod,  2,  AuraType.AddFlatModifier));
	}
}