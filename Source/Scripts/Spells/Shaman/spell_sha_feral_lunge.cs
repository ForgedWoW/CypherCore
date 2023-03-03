﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman
{
    // Feral Lunge - 196884
    [SpellScript(196884)]
	public class spell_sha_feral_lunge : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override bool Validate(SpellInfo UnnamedParameter)
		{
			if (Global.SpellMgr.GetSpellInfo(ShamanSpells.FERAL_LUNGE_DAMAGE, Difficulty.None) != null)
				return false;

			return true;
		}

		private void HandleDamage(int effIndex)
		{
			var caster = GetCaster();
			var target = GetHitUnit();

			if (caster == null || target == null)
				return;

			caster.CastSpell(target, ShamanSpells.FERAL_LUNGE_DAMAGE, true);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleDamage, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}
	}
}