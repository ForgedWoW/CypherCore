// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 11119 - Ignite
internal class spell_mage_ignite : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(MageSpells.Ignite);
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		return eventInfo.ProcTarget;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		var igniteDot = Global.SpellMgr.GetSpellInfo(MageSpells.Ignite, CastDifficulty);
		var pct = aurEff.Amount;

		var amount = (int)(MathFunctions.CalculatePct(eventInfo.DamageInfo.Damage, pct) / igniteDot.MaxTicks);

		CastSpellExtraArgs args = new(aurEff);
		args.AddSpellMod(SpellValueMod.BasePoint0, amount);
		Target.CastSpell(eventInfo.ProcTarget, MageSpells.Ignite, args);
	}
}