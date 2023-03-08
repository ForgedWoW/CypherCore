// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Models;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(200389)]
public class spell_dru_cultivation : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.PeriodicHeal));
	}

	private void CalculateAmount(AuraEffect UnnamedParameter, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
	{
		if (!Caster)
			return;

		amount.Value = MathFunctions.CalculatePct(Caster.SpellBaseHealingBonusDone(SpellSchoolMask.Nature), 60);
	}
}