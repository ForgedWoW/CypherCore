// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[Script] // 67151 - Item - Hunter T9 4P Bonus (Steady Shot)
internal class spell_hun_t9_4p_bonus : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(HunterSpells.T94PGreatness);
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (eventInfo.Actor.IsTypeId(TypeId.Player) &&
			eventInfo.Actor.AsPlayer.CurrentPet)
			return true;

		return false;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		var caster = eventInfo.Actor;

		caster.CastSpell(caster.AsPlayer.CurrentPet, HunterSpells.T94PGreatness, new CastSpellExtraArgs(aurEff));
	}
}