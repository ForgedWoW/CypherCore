// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(213017)]
public class spell_dh_artifact_fueled_by_pain : AuraScript, IHasAuraEffects, IAuraCheckProc
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		return eventInfo.SpellInfo != null && (eventInfo.SpellInfo.Id == ShatteredSoulsSpells.SOUL_FRAGMENT_HEAL_VENGEANCE || eventInfo.SpellInfo.Id == ShatteredSoulsSpells.LESSER_SOUL_SHARD_HEAL);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void OnProc(AuraEffect aurEff, ProcEventInfo UnnamedParameter)
	{
		var caster = Caster;

		if (caster == null)
			return;

		var duration = aurEff.Amount * Time.InMilliseconds;
		var aur = caster.AddAura(DemonHunterSpells.METAMORPHOSIS_VENGEANCE, caster);

		if (aur != null)
		{
			aur.SetMaxDuration(duration);
			aur.RefreshDuration();
		}
	}
}