// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

// 264178 - Demonbolt
[SpellScript(264178)]
public class spell_warlock_demonbolt_new : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHit));
	}

	private void HandleHit(int effIndex)
	{
		if (Caster)
		{
			Caster.CastSpell(Caster, WarlockSpells.DEMONBOLT_ENERGIZE, true);
			Caster.CastSpell(Caster, WarlockSpells.DEMONBOLT_ENERGIZE, true);
		}
	}
}