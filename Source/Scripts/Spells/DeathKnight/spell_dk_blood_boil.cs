// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[Script] // 50842 - Blood Boil
internal class spell_dk_blood_boil : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		Caster.CastSpell(HitUnit, DeathKnightSpells.BloodPlague, true);
	}
}