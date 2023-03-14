// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Models;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(205725)]
public class spell_dk_anti_magic_barrier : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcAmount, 0, AuraType.ModIncreaseHealth2));
	}

	private void CalcAmount(AuraEffect aurEff, BoxedValue<double> amount, BoxedValue<bool> UnnamedParameter)
	{
		var caster = Caster;

		if (caster != null)
			amount.Value = ((caster.MaxHealth * 25.0f) / 100.0f);
	}
}