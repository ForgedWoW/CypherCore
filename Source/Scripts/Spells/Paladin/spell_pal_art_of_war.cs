// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Paladin;

// 267344 - Art of War
[SpellScript(267344)]
internal class spell_pal_art_of_war : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();


	public override void Register()
	{
		AuraEffects.Add(new AuraCheckEffectProcHandler(CheckProc, 0, AuraType.Dummy));
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		return RandomHelper.randChance(aurEff.Amount);
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		Target.SpellHistory.ResetCooldown(PaladinSpells.BLADE_OF_JUSTICE, true);
		Target.CastSpell(Target, PaladinSpells.ArtOfWarTriggered, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress));
	}
}