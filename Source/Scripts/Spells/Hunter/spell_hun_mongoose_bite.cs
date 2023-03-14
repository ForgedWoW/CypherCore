// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(190928)]
public class spell_hun_mongoose_bite : SpellScript, ISpellAfterHit
{
	public void AfterHit()
	{
		var dur = 0;
		var aur = Caster.GetAura(HunterSpells.MONGOOSE_FURY);

		if (aur != null)
			dur = aur.Duration;

		Caster.CastSpell(Caster, HunterSpells.MONGOOSE_FURY, true);

		aur = Caster.GetAura(HunterSpells.MONGOOSE_FURY);

		if (aur != null)
			if (dur != 0)
				aur.SetDuration(dur);
	}
}