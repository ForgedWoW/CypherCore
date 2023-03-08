// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warlock;

[SpellScript(WarlockSpells.INQUISITORS_GAZE)]
public class spell_warlock_inquisitors_gaze : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleOnHit(int effectIndex)
	{
		var target = HitUnit;

		if (target != null)
		{
			var damage = (Caster.SpellBaseDamageBonusDone(SpellInfo.GetSchoolMask()) * 15 * 16) / 100;
			Caster.CastSpell(target, WarlockSpells.INQUISITORS_GAZE_EFFECT, new CastSpellExtraArgs(SpellValueMod.BasePoint0, damage));
		}
	}
}