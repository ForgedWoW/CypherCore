﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(205598)]
public class spell_dh_awaken_the_demon : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		var caster = Caster;

		if (caster == null || eventInfo.DamageInfo != null)
			return;

		if (!SpellInfo.GetEffect(1).IsEffect() || !SpellInfo.GetEffect(2).IsEffect())
			return;

		var threshold1 = caster.CountPctFromMaxHealth(aurEff.BaseAmount);
		var threshold2 = caster.CountPctFromMaxHealth(SpellInfo.GetEffect(1).BasePoints);
		var duration = SpellInfo.GetEffect(2).BasePoints;

		if (caster.Health - eventInfo.DamageInfo.Damage < threshold1)
		{
			if (caster.HasAura(DemonHunterSpells.AWAKEN_THE_DEMON_CD))
				return;

			caster.CastSpell(caster, DemonHunterSpells.AWAKEN_THE_DEMON_CD, true);
			var aur = caster.GetAura(DemonHunterSpells.METAMORPHOSIS_HAVOC);

			if (aur != null)
			{
				aur.SetDuration(Math.Min(duration * Time.InMilliseconds + aur.Duration, aur.MaxDuration));

				return;
			}

			aur = caster.AddAura(DemonHunterSpells.METAMORPHOSIS_HAVOC, caster);

			if (aur != null)
				aur.SetDuration(duration * Time.InMilliseconds);
		}

		// Check only if we are above the second threshold and we are falling under it just now
		if (caster.Health > threshold2 && caster.Health - eventInfo.DamageInfo.Damage < threshold2)
		{
			var aur = caster.GetAura(DemonHunterSpells.METAMORPHOSIS_HAVOC);

			if (aur != null)
			{
				aur.SetDuration(Math.Min(duration * Time.InMilliseconds + aur.Duration, aur.MaxDuration));

				return;
			}
		}
	}
}