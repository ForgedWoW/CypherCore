﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(122280)]
public class spell_monk_healing_elixirs_aura : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	private void OnProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		if (!GetCaster())
			return;

		if (eventInfo.GetDamageInfo() != null)
			return;

		if (eventInfo.GetDamageInfo() != null)
			return;

		var caster = GetCaster();

		if (caster != null)
			if (caster.HealthBelowPctDamaged(35, eventInfo.GetDamageInfo().GetDamage()))
			{
				caster.CastSpell(caster, MonkSpells.SPELL_MONK_HEALING_ELIXIRS_RESTORE_HEALTH, true);
				caster.GetSpellHistory().ConsumeCharge(MonkSpells.SPELL_MONK_HEALING_ELIXIRS_RESTORE_HEALTH);
			}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}
}