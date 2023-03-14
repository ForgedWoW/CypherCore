// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[Script] // 781 - Disengage
internal class spell_hun_posthaste : SpellScript, ISpellAfterCast
{
	public void AfterCast()
	{
		if (Caster.HasAura(HunterSpells.PosthasteTalent))
		{
			Caster.RemoveMovementImpairingAuras(true);
			Caster.CastSpell(Caster, HunterSpells.PosthasteIncreaseSpeed, Spell);
		}
	}
}