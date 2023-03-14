// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[Script] // 33395 Water Elemental's Freeze
internal class spell_mage_water_elemental_freeze : SpellScript, ISpellAfterHit
{
	public void AfterHit()
	{
		var owner = Caster.OwnerUnit;

		if (!owner)
			return;

		owner.CastSpell(owner, MageSpells.FingersOfFrost, true);
	}
}