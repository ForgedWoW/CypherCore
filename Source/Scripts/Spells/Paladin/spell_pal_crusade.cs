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

// 231895
[SpellScript(231895)]
public class spell_pal_crusade : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.AddPctModifier));
		AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.AddPctModifier, AuraScriptHookType.EffectProc));
	}

	private void CalculateAmount(AuraEffect UnnamedParameter, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
	{
		amount.Value /= 10;
	}

	private void OnProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
	{
		var powerCosts = eventInfo.SpellInfo.CalcPowerCost(eventInfo.Actor, SpellSchoolMask.Holy);

		foreach (var powerCost in powerCosts)
			if (powerCost.Power == PowerType.HolyPower)
				Aura.ModStackAmount(powerCost.Amount, AuraRemoveMode.Default, false);
	}
}