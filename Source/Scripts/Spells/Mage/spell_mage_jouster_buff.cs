// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Models;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[SpellScript(195391)]
public class spell_mage_jouster_buff : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.ModDamagePercentTaken));
	}

	private void CalculateAmount(AuraEffect UnnamedParameter, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
	{
		var caster = Caster;

		if (caster == null)
			return;

		var jousterRank = caster.GetAuraEffect(MageSpells.JOUSTER, 0);

		if (jousterRank != null)
			amount.Value = jousterRank.Amount;
	}
}