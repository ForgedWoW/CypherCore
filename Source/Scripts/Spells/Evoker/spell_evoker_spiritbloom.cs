// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.DataStorage;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.SPIRITBLOOM, EvokerSpells.SPIRITBLOOM_2)]
internal class spell_evoker_spiritbloom : SpellScript, ISpellOnEpowerSpellEnd
{
	public void EmpowerSpellEnd(SpellEmpowerStageRecord stage, uint stageDelta)
	{
		var caster = Caster;

		// cast on primary target
		caster.CastSpell(EvokerSpells.SPIRITBLOOM_CHARGED, true, stage.Stage);

		// determine number of additional targets
		var targets = 0;

		switch (Spell.EmpoweredStage)
		{
			case 1:
				targets = 1;

				break;
			case 2:
				targets = 2;

				break;
			case 3:
				targets = 3;

				break;
			default:
				break;
		}

		if (targets > 0)
		{
			// get targets that are injured
			var targetList = new List<Unit>();
			caster.GetAlliesWithinRange(targetList, SpellInfo.GetMaxRange());
			targetList.RemoveIf(a => a.IsFullHealth);

			// reduce targetList to the number allowed
			while (targetList.Count > targets)
				targetList.RemoveAt(targetList.Count - 1);

			// cast on targets
			foreach (var target in targetList)
				caster.CastSpell(target, EvokerSpells.SPIRITBLOOM_CHARGED, true, stage.Stage);
		}
	}
}