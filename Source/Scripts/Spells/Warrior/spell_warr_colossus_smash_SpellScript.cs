// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warrior;

[Script] // 167105 - Colossus Smash 7.1.5
internal class spell_warr_colossus_smash_SpellScript : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var target = HitUnit;

		if (target)
			Caster.CastSpell(target, WarriorSpells.COLOSSUS_SMASH_EFFECT, true);
	}
}