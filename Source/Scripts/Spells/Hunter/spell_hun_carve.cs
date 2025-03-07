﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(187708)]
public class spell_hun_carve : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var caster = Caster;
		var target = HitUnit;

		if (caster == null || target == null)
			return;

		if (caster.HasSpell(HunterSpells.SERPENT_STING))
			caster.CastSpell(target, HunterSpells.SERPENT_STING_DAMAGE, true);
	}
}