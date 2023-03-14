// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 35201 - Paralytic Poison
internal class spell_gen_paralytic_poison : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();


	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleStun, 0, AuraType.PeriodicDamage, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void HandleStun(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		if (TargetApplication.RemoveMode != AuraRemoveMode.Expire)
			return;

		Target.CastSpell((Unit)null, GenericSpellIds.Paralysis, new CastSpellExtraArgs(aurEff));
	}
}