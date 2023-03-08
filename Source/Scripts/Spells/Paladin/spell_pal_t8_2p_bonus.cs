// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Paladin;

[SpellScript(64890)] // 64890 - Item - Paladin T8 Holy 2P Bonus
internal class spell_pal_t8_2p_bonus : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(PaladinSpells.HolyMending);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		var healInfo = eventInfo.HealInfo;

		if (healInfo == null ||
			healInfo.GetHeal() == 0)
			return;

		var caster = eventInfo.Actor;
		var target = eventInfo.ProcTarget;

		var spellInfo = Global.SpellMgr.GetSpellInfo(PaladinSpells.HolyMending, CastDifficulty);
		var amount = (int)MathFunctions.CalculatePct(healInfo.GetHeal(), aurEff.Amount);
		amount /= (int)spellInfo.MaxTicks;

		CastSpellExtraArgs args = new(aurEff);
		args.AddSpellMod(SpellValueMod.BasePoint0, amount);
		caster.CastSpell(target, PaladinSpells.HolyMending, args);
	}
}