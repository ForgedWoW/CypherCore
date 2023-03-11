// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Paladin;

//183218
[SpellScript(183218)]
public class spell_pal_hand_of_hindrance : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}

	private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes mode)
	{
		if (TargetApplication.RemoveMode == AuraRemoveMode.EnemySpell)
		{
			var caster = Caster;

			if (caster != null)
				if (caster.HasAura(PaladinSpells.LAW_AND_ORDER))
					caster.SpellHistory.ModifyCooldown(PaladinSpells.HAND_OF_HINDRANCE, TimeSpan.FromSeconds(-15));
		}
	}
}