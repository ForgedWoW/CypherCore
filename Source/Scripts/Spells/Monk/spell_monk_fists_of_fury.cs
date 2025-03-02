﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(MonkSpells.FISTS_OF_FURY)]
public class spell_monk_fists_of_fury : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 2, AuraType.PeriodicDummy));
	}

	private void HandlePeriodic(AuraEffect aurEff)
	{
		var caster = Caster;

		if (caster == null)
			return;

		if (aurEff.GetTickNumber() % 6 == 0)
			caster.CastSpell(Target, MonkSpells.FISTS_OF_FURY_DAMAGE, true);
	}
}