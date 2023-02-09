﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 282559 - Enlisted
internal class spell_gen_war_mode_enlisted : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(ScriptSpellId, Difficulty.None);

		if (spellInfo.HasAura(AuraType.ModXpPct))
			AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcWarModeBonus, SpellConst.EffectAll, AuraType.ModXpPct));

		if (spellInfo.HasAura(AuraType.ModXpQuestPct))
			AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcWarModeBonus, SpellConst.EffectAll, AuraType.ModXpQuestPct));

		if (spellInfo.HasAura(AuraType.ModCurrencyGainFromSource))
			AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcWarModeBonus, SpellConst.EffectAll, AuraType.ModCurrencyGainFromSource));

		if (spellInfo.HasAura(AuraType.ModMoneyGain))
			AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcWarModeBonus, SpellConst.EffectAll, AuraType.ModMoneyGain));

		if (spellInfo.HasAura(AuraType.ModAnimaGain))
			AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcWarModeBonus, SpellConst.EffectAll, AuraType.ModAnimaGain));

		if (spellInfo.HasAura(AuraType.Dummy))
			AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcWarModeBonus, SpellConst.EffectAll, AuraType.Dummy));
	}

	private void CalcWarModeBonus(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
	{
		Player target = GetUnitOwner().ToPlayer();

		if (target == null)
			return;

		switch (target.GetTeamId())
		{
			case TeamId.Alliance:
				amount = Global.WorldStateMgr.GetValue(WorldStates.WarModeAllianceBuffValue, target.GetMap());

				break;
			case TeamId.Horde:
				amount = Global.WorldStateMgr.GetValue(WorldStates.WarModeHordeBuffValue, target.GetMap());

				break;
		}
	}
}