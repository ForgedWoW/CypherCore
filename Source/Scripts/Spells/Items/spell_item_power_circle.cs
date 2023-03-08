// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 45043 - Power Circle (Shifting Naaru Sliver)
internal class spell_item_power_circle : AuraScript, IAuraCheckAreaTarget, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ItemSpellIds.LimitlessPower);
	}

	public bool CheckAreaTarget(Unit target)
	{
		return target.GetGUID() == CasterGUID;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		Target.CastSpell(null, ItemSpellIds.LimitlessPower, true);
		var buff = Target.GetAura(ItemSpellIds.LimitlessPower);

		buff?.SetDuration(Duration);
	}

	private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		Target.RemoveAura(ItemSpellIds.LimitlessPower);
	}
}