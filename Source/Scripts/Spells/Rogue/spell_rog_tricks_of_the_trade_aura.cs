// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[Script] // 57934 - Tricks of the Trade
internal class spell_rog_tricks_of_the_trade_aura : AuraScript, IHasAuraEffects
{
	private ObjectGuid _redirectTarget;
	public List<IAuraEffectHandler> AuraEffects { get; } = new();


	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 1, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	public void SetRedirectTarget(ObjectGuid guid)
	{
		_redirectTarget = guid;
	}

	private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		if (TargetApplication.RemoveMode != AuraRemoveMode.Default ||
			!Target.HasAura(RogueSpells.TricksOfTheTradeProc))
			Target.GetThreatManager().UnregisterRedirectThreat(RogueSpells.TricksOfTheTrade);
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		var rogue = Target;

		if (Global.ObjAccessor.GetUnit(rogue, _redirectTarget))
			rogue.CastSpell(rogue, RogueSpells.TricksOfTheTradeProc, new CastSpellExtraArgs(aurEff));

		Remove(AuraRemoveMode.Default);
	}
}