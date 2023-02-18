﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(203974)]
public class spell_druid_earthwarden : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	private struct Spells
	{
		public static readonly uint SPELL_DRUID_EARTHWARDEN = 203974;
		public static readonly uint SPELL_DRUID_EARTHWARDEN_TRIGGERED = 203975;
		public static readonly uint SPELL_DRUID_TRASH = 77758;
	}

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(Spells.SPELL_DRUID_EARTHWARDEN, Spells.SPELL_DRUID_EARTHWARDEN_TRIGGERED, Spells.SPELL_DRUID_TRASH);
	}

	private void OnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		if (!GetCaster().ToPlayer().GetSpellHistory().HasCooldown(Spells.SPELL_DRUID_EARTHWARDEN))
			GetCaster().AddAura(Spells.SPELL_DRUID_EARTHWARDEN_TRIGGERED, GetCaster());

		GetCaster().ToPlayer().GetSpellHistory().AddCooldown(Spells.SPELL_DRUID_EARTHWARDEN, 0, TimeSpan.FromMicroseconds(500));
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}
}