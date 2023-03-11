// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.DRAGONRAGE_EFFECT)]
internal class spell_evoker_dragonrage : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		var caster = Caster;

		// get targets
		var targetList = new List<Unit>();
		caster.GetEnemiesWithinRange(targetList, SpellInfo.GetMaxRange());

		// reduce targetList to the number allowed
        targetList.RandomResize(SpellInfo.GetEffect(0).BasePoints);

        // cast on targets
        foreach (var target in targetList)
			caster.CastSpell(target, EvokerSpells.PYRE_MISSILE, true);
	}
}