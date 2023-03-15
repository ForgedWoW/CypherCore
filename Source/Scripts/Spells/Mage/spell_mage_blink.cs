// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[SpellScript(1953)]
public class spell_mage_blink : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		var caster = Caster;

		if (caster.HasAura(MageSpells.TEMPEST_BARRIER))
			caster.CastSpell(MageSpells.TEMPEST_BARRIER);

		if (caster.HasAura(MageSpells.BLAZING_SOUL))
			caster.AddAura(MageSpells.BLAZING_BARRIER, caster);

		if (caster.HasAura(MageSpells.PRISMATIC_CLOAK))
			caster.AddAura(MageSpells.PRISMATIC_CLOAK_BUFF, caster);
	}
}