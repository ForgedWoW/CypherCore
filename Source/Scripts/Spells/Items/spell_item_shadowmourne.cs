// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 71903 - Item - Shadowmourne Legendary
internal class spell_item_shadowmourne : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ItemSpellIds.ShadowmourneChaosBaneDamage, ItemSpellIds.ShadowmourneSoulFragment, ItemSpellIds.ShadowmourneChaosBaneBuff);
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (Target.HasAura(ItemSpellIds.ShadowmourneChaosBaneBuff)) // cant collect shards while under effect of Chaos Bane buff
			return false;

		return eventInfo.ProcTarget && eventInfo.ProcTarget.IsAlive;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		Target.CastSpell(Target, ItemSpellIds.ShadowmourneSoulFragment, new CastSpellExtraArgs(aurEff));

		// this can't be handled in AuraScript of SoulFragments because we need to know victim
		var soulFragments = Target.GetAura(ItemSpellIds.ShadowmourneSoulFragment);

		if (soulFragments != null)
			if (soulFragments.StackAmount >= 10)
			{
				Target.CastSpell(eventInfo.ProcTarget, ItemSpellIds.ShadowmourneChaosBaneDamage, new CastSpellExtraArgs(aurEff));
				soulFragments.Remove();
			}
	}

	private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		Target.RemoveAura(ItemSpellIds.ShadowmourneSoulFragment);
	}
}