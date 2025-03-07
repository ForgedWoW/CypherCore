﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(202808)]
public class spell_dru_primal_vitality : AuraScript, IHasAuraEffects
{
	private const int PRIMAL_VITALITY_PASSIVE = 202808;
	private const int PRIMAL_VITALITY_EFFECT = 202812;
	private const int PROWL = 5215;

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (eventInfo.SpellInfo != null)
			return false;

		if (eventInfo.DamageInfo != null)
			return false;

		if (eventInfo.SpellInfo.Id != PROWL)
			return false;

		return true;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
	}

	private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		var target = eventInfo.ProcTarget;

		if (target != null)
			if (!target.HasAura(PRIMAL_VITALITY_EFFECT))
				target.AddAura(PRIMAL_VITALITY_EFFECT, target);
	}
}