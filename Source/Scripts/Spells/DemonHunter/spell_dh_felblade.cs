// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(232893)]
public class spell_dh_felblade : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleOnHit(int effIndex)
	{
		if (!Caster || !HitUnit)
			return;

		if (Caster.GetDistance2d(HitUnit) <= 15.0f)
		{
			Caster.CastSpell(HitUnit, DemonHunterSpells.FELBLADE_CHARGE, true);
			Caster.CastSpell(HitUnit, DemonHunterSpells.FELBLADE_DAMAGE, true);
		}
	}
}