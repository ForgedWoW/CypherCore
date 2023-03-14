// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// Ascendance (Water) - 114052
[SpellScript(114052)]
public class spell_sha_ascendance_water : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();


	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (eventInfo.HealInfo != null && eventInfo.SpellInfo != null && eventInfo.SpellInfo.Id == eSpells.RestorativeMists)
			return false;

		if (eventInfo.HealInfo == null)
			return false;

		return true;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 1, AuraType.PeriodicDummy, AuraScriptHookType.EffectProc));
	}

	private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		var bp0 = eventInfo.HealInfo.Heal;

		if (bp0 != 0)
			eventInfo.ActionTarget.CastSpell(eventInfo.Actor, eSpells.RestorativeMists, new CastSpellExtraArgs(aurEff).AddSpellMod(SpellValueMod.BasePoint0, (int)bp0));
	}

	private struct eSpells
	{
		public const uint RestorativeMists = 114083;
	}
}