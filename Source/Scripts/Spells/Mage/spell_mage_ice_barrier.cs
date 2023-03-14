// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Models;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 11426 - Ice Barrier
internal class spell_mage_ice_barrier : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();


	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.SchoolAbsorb, AuraScriptHookType.EffectProc));
	}

	private void CalculateAmount(AuraEffect aurEff, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
	{
		canBeRecalculated.Value = false;
		var caster = Caster;

		if (caster)
			amount.Value += (caster.SpellBaseHealingBonusDone(SpellInfo.GetSchoolMask()) * 10.0f);
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		var caster = eventInfo.DamageInfo.Victim;
		var target = eventInfo.DamageInfo.Attacker;

		if (caster && target)
			caster.CastSpell(target, MageSpells.Chilled, true);
	}
}