// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_trigger_exclude_target_aura_spell : SpellScript, ISpellAfterHit
{
	public void AfterHit()
	{
		var target = HitUnit;

		if (target)
			// Blizz seems to just apply aura without bothering to cast
			Caster.AddAura(SpellInfo.ExcludeTargetAuraSpell, target);
	}
}