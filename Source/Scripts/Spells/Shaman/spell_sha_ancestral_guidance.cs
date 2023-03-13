// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 108281 - Ancestral Guidance
[SpellScript(108281)]
internal class spell_sha_ancestral_guidance : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.AncestralGuidanceHeal);
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (eventInfo.HealInfo.SpellInfo.Id == ShamanSpells.AncestralGuidanceHeal)
			return false;

		return true;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.PeriodicDummy, AuraScriptHookType.EffectProc));
	}

	private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		var bp0 = MathFunctions.CalculatePct((int)eventInfo.DamageInfo.Damage, aurEff.Amount);

		if (bp0 != 0)
		{
			CastSpellExtraArgs args = new(aurEff);
			args.AddSpellMod(SpellValueMod.BasePoint0, bp0);
			eventInfo.Actor.CastSpell(eventInfo.Actor, ShamanSpells.AncestralGuidanceHeal, args);
		}
	}
}