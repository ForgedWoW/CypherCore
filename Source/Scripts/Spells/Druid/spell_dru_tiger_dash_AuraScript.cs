﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[Script] // 252216 - Tiger Dash (Aura)
internal class spell_dru_tiger_dash_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 1, AuraType.PeriodicDummy));
	}

	private void HandlePeriodic(AuraEffect aurEff)
	{
		var effRunSpeed = GetEffect(0);

		if (effRunSpeed != null)
		{
			var reduction = aurEff.Amount;
			effRunSpeed.ChangeAmount(effRunSpeed.Amount - reduction);
		}
	}
}