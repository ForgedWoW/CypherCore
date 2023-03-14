// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(8936)]
public class spell_dru_regrowth : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHealEffect, 0, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleHealEffect(int effIndex)
	{
		if (Caster.HasAura(DruidSpells.BLOODTALONS))
			Caster.AddAura(DruidSpells.BLOODTALONS_TRIGGERED, Caster);

		var clearcasting = Caster.GetAura(DruidSpells.CLEARCASTING);

		if (clearcasting != null)
		{
			if (Caster.HasAura(DruidSpells.MOMENT_OF_CLARITY))
			{
				var amount = clearcasting.GetEffect(0).Amount;
				clearcasting.GetEffect(0).SetAmount(amount - 1);

				if (amount == -102)
					Caster.RemoveAura(DruidSpells.CLEARCASTING);
			}
			else
			{
				Caster.RemoveAura(DruidSpells.CLEARCASTING);
			}
		}
	}
}