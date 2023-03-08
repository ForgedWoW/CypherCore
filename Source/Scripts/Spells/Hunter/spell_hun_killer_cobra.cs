// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[SpellScript(199532)]
public class spell_hun_killer_cobra : AuraScript, IHasAuraEffects, IAuraCheckProc
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();


	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (eventInfo.SpellInfo.Id == HunterSpells.COBRA_SHOT)
			return true;

		return false;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
	{
		PreventDefaultAction();

		var caster = Caster;

		if (caster != null)
			if (caster.HasAura(HunterSpells.BESTIAL_WRATH))
				if (caster.SpellHistory.HasCooldown(HunterSpells.KILL_COMMAND))
					caster.					SpellHistory.ResetCooldown(HunterSpells.KILL_COMMAND, true);
	}
}