﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman
{
	// Frostbrand - 196834
	[SpellScript(196834)]
	public class bfa_spell_frostbrand_SpellScript : SpellScript, ISpellOnHit
	{
		public override bool Load()
		{
			return GetCaster().IsPlayer();
		}

		public void OnHit()
		{
			var caster = GetCaster();
			var target = GetHitUnit();

			if (caster == null || target == null)
				return;

			caster.CastSpell(target, ShamanSpells.SPELL_FROSTBRAND_SLOW, true);
		}
	}
}