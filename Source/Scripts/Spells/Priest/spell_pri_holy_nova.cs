﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Priest;

[SpellScript(132157)]
public class spell_pri_holy_nova : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var caster = Caster;
		var target = HitUnit;

		if (caster == null || target == null)
			return;

		if (target != null)
			if (RandomHelper.randChance(20))
				caster.SpellHistory.ResetCooldown(PriestSpells.HOLY_FIRE, true);
	}
}