// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[Script]
internal class spell_dru_travel_form_dummy_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();


	public override bool Load()
	{
		return Caster.IsTypeId(TypeId.Player);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
		AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var player = Target.AsPlayer;

		// Outdoor check already passed - Travel Form (dummy) has ATTR0_OUTDOORS_ONLY attribute.
		var triggeredSpellId = spell_dru_travel_form_AuraScript.GetFormSpellId(player, CastDifficulty, false);

		player.CastSpell(player, triggeredSpellId, new CastSpellExtraArgs(aurEff));
	}

	private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		// No need to check remove mode, it's safe for Auras to remove each other in AfterRemove hook.
		Target.RemoveAura(DruidSpellIds.FormStag);
		Target.RemoveAura(DruidSpellIds.FormAquatic);
		Target.RemoveAura(DruidSpellIds.FormFlight);
		Target.RemoveAura(DruidSpellIds.FormSwiftFlight);
	}
}