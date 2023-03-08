// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Models;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(152118)]
public class spell_pri_clarity_of_will : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
	}

	private void CalculateAmount(AuraEffect aurEff, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
	{
		var caster = aurEff.Caster;

		if (caster != null)
		{
			var player = caster.AsPlayer;

			if (player != null)
			{
				var absorbamount = 9.0f * player.SpellBaseHealingBonusDone(SpellInfo.GetSchoolMask());
				amount.Value += absorbamount;
			}
		}
	}
}