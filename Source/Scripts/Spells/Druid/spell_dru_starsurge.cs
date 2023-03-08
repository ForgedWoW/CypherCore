// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(78674)]
public class spell_dru_starsurge : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		if (Caster)
			if (Caster.GetAuraCount(DruidSpells.STARLORD_BUFF) < 3)
				Caster.CastSpell(null, DruidSpells.STARLORD_BUFF, true);
	}
}