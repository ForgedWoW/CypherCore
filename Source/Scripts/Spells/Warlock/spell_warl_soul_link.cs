﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

// 108415 - Soul Link 8.xx
[SpellScript(WarlockSpells.SOUL_LINK)]
public class spell_warl_soul_link : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var caster = Caster;

		if (caster != null)
		{
			var target = HitUnit;

			if (target != null)
				if (!target.HasAura(WarlockSpells.SOUL_LINK_BUFF))
					caster.CastSpell(caster, WarlockSpells.SOUL_LINK_BUFF, true);
		}
	}
}