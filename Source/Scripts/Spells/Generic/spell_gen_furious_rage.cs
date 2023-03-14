// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 35491 - Furious Rage
internal class spell_gen_furious_rage : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(AfterApply, 0, AuraType.ModDamagePercentDone, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
		AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 0, AuraType.ModDamagePercentDone, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void AfterApply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var target = Target;
		target.TextEmote(EmoteIds.FuriousRage, target, false);
	}

	private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		if (TargetApplication.RemoveMode != AuraRemoveMode.Expire)
			return;

		var target = Target;
		target.TextEmote(EmoteIds.Exhausted, target, false);
		target.CastSpell(target, GenericSpellIds.Exhaustion, true);
	}
}