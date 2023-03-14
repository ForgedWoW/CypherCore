// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.LANDSLIDE)]
internal class spell_evoker_landslide : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		Caster.CastSpell(Spell.Targets.DstPos, EvokerSpells.LANDSLIDE_AREA_TRIGGER, true);
	}
}