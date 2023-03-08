// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

// 231489 - Compounding Horror
[SpellScript(231489)]
internal class spell_warlock_compounding_horror : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleHit(int effIndex)
	{
		var caster = Caster;

		if (caster == null)
			return;

		var damage = HitDamage;
		var stacks = 0;
		var aur = caster.GetAura(WarlockSpells.COMPOUNDING_HORROR);

		if (aur != null)
			stacks = aur.StackAmount;

		HitDamage = damage * stacks;

		caster.RemoveAura(WarlockSpells.COMPOUNDING_HORROR);
	}
}