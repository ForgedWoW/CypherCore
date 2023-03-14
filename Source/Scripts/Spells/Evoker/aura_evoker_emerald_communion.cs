// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Models;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.EMERALD_COMMUNION)]
internal class aura_evoker_emerald_communion : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcAmount, 7, AuraType.PeriodicHeal));
	}

	private void CalcAmount(AuraEffect aurEff, BoxedValue<double> amount, BoxedValue<bool> recalculate)
	{
		recalculate.Value = false;

		amount.Value = (Caster.MaxHealth * (GetEffect(4).BaseAmount * 0.01)) / (Aura.Duration / aurEff.Period);
	}
}