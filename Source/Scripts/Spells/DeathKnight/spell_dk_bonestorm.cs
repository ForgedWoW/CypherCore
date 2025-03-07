﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(194844)]
public class spell_dk_bonestorm : AuraScript, IHasAuraEffects
{
	private int m_ExtraSpellCost;
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Load()
	{
		var caster = Caster;

		if (caster == null)
			return false;

		var availablePower = Math.Min(caster.GetPower(PowerType.RunicPower), 90);

		//Round down to nearest multiple of 10
		m_ExtraSpellCost = availablePower - (availablePower % 10);

		return true;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 2, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real));
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 2, AuraType.PeriodicTriggerSpell));
	}

	private void HandleApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var m_newDuration = Duration + (m_ExtraSpellCost / 10);
		SetDuration(m_newDuration);

		var caster = Caster;

		if (caster != null)
		{
			var m_newPower = caster.GetPower(PowerType.RunicPower) - m_ExtraSpellCost;

			if (m_newPower < 0)
				m_newPower = 0;

			caster.SetPower(PowerType.RunicPower, m_newPower);
		}
	}

	private void HandlePeriodic(AuraEffect UnnamedParameter)
	{
		var caster = Caster;

		if (caster == null)
			return;

		caster.CastSpell(caster, DeathKnightSpells.BONESTORM_HEAL, true);
	}
}