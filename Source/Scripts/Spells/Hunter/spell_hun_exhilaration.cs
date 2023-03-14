// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[Script] // 109304 - Exhilaration
internal class spell_hun_exhilaration : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		if (Caster.HasAura(HunterSpells.ExhilarationR2) && !Caster.HasAura(HunterSpells.Lonewolf))
			Caster.CastSpell(null, HunterSpells.ExhilarationPet, true);
	}
}