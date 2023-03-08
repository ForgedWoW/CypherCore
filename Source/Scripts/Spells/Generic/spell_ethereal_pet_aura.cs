// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_ethereal_pet_aura : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		var levelDiff = (uint)Math.Abs(Target.Level - eventInfo.ProcTarget.Level);

		return levelDiff <= 9;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		List<TempSummon> minionList = new();
		UnitOwner.GetAllMinionsByEntry(minionList, CreatureIds.EtherealSoulTrader);

		foreach (Creature minion in minionList)
			if (minion.IsAIEnabled)
			{
				minion.AI.Talk(TextIds.SayStealEssence);
				minion.CastSpell(eventInfo.ProcTarget, GenericSpellIds.StealEssenceVisual);
			}
	}
}