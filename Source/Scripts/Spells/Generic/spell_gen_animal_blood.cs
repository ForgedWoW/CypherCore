﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 46221 - Animal Blood
internal class spell_gen_animal_blood : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();


	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		// Remove all Auras with spell Id 46221, except the one currently being applied
		Aura aur;

		while ((aur = OwnerAsUnit.GetOwnedAura(GenericSpellIds.AnimalBlood, ObjectGuid.Empty, ObjectGuid.Empty, Aura)) != null)
			OwnerAsUnit.RemoveOwnedAura(aur);
	}

	private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var owner = OwnerAsUnit;

		if (owner)
			owner.CastSpell(owner, GenericSpellIds.SpawnBloodPool, true);
	}
}