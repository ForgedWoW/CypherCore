// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(100780)]
public class spell_monk_tiger_palm : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 1, SpellEffectName.Energize, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleHit(int effIndex)
	{
		var powerStrikes = Caster.GetAura(MonkSpells.POWER_STRIKES_AURA);

		if (powerStrikes != null)
		{
			EffectValue = EffectValue + powerStrikes.GetEffect(0).BaseAmount;
			powerStrikes.Remove();
		}
	}
}