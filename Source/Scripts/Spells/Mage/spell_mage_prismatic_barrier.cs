﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Models;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 235450 - Prismatic Barrier
internal class spell_mage_prismatic_barrier : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
	}

	private void CalculateAmount(AuraEffect aurEff, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
	{
		canBeRecalculated.Value = false;
		var caster = Caster;

		if (caster)
			amount.Value += (caster.SpellBaseHealingBonusDone(SpellInfo.GetSchoolMask()) * 7.0f);
	}
}