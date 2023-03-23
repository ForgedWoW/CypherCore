// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(new uint[]
{
	205320, 205414, 222029
})]
public class spell_monk_strike_of_the_windlord : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		var target = HitUnit;

		if (target != null)
		{
			var damage = EffectValue;
			MathFunctions.AddPct(ref damage, target.GetTotalAuraModifier(AuraType.ModDamagePercentTaken));
			HitDamage = damage;
		}
	}
}