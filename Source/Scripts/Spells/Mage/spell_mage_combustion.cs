﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Models;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[SpellScript(190319)]
public class spell_mage_combustion : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcAmount, 1, AuraType.ModRating));
		AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 1, AuraType.ModRating, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}

	private void CalcAmount(AuraEffect UnnamedParameter, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
	{
		var caster = Caster;

		if (caster == null)
			return;

		if (!caster.IsPlayer)
			return;

		var crit = caster.AsPlayer.GetRatingBonusValue(CombatRating.CritSpell);
		amount.Value += crit;
	}

	private void HandleRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		Caster.RemoveAura(MageSpells.INFERNO);
	}
}