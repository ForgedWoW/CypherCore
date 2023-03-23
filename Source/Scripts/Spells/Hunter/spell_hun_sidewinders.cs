// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(214579)]
public class spell_hun_sidewinders : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		SpellEffects.Add(new EffectHandler(HandleDummy1, 2, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		var caster = Caster;

		if (caster != null)
		{
			var target = HitUnit;

			if (target != null)
				caster.CastSpell(target, 187131, true);
		}
	}

	private void HandleDummy1(int effIndex)
	{
		var caster = Caster;

		if (caster != null)
		{
			var target = HitUnit;

			if (target != null)
			{
				caster.CastSpell(target, 214581, true);
				caster.SendPlaySpellVisual(target.Location, target.Location.Orientation, 56931, 0, 0, 18.0f, false);
			}
		}
	}
}