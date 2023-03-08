// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Models;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[Script] // 48743 - Death Pact
internal class spell_dk_death_pact : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(HandleCalcAmount, 1, AuraType.SchoolHealAbsorb));
	}

	private void HandleCalcAmount(AuraEffect aurEff, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
	{
		var caster = Caster;

		if (caster)
			amount.Value = caster.CountPctFromMaxHealth(amount);
	}
}