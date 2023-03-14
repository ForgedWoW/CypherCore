// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(80313)]
public class spell_druid_pulverize : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHitTarget, 2, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleHitTarget(int effIndex)
	{
		var target = HitUnit;

		if (target != null)
		{
			target.RemoveAura(Spells.TRASH_DOT_TWO_STACKS_MARKER);
			Caster.CastSpell(target, Spells.PULVERIZE_DAMAGE_REDUCTION_BUFF, true);
		}
	}

	private struct Spells
	{
		public static readonly uint PULVERIZE = 80313;
		public static readonly uint TRASH_DOT_TWO_STACKS_MARKER = 158790;
		public static readonly uint PULVERIZE_DAMAGE_REDUCTION_BUFF = 158792;
	}
}