// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Quest;

[Script] // 93072 - Get Our Boys Back Dummy
internal class spell_q28813_get_our_boys_back_dummy : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		var caster = Caster;
		var injuredStormwindInfantry = caster.FindNearestCreature(CreatureIds.InjuredStormwindInfantry, 5.0f, true);

		if (injuredStormwindInfantry)
		{
			injuredStormwindInfantry.SetCreatorGUID(caster.GUID);
			injuredStormwindInfantry.CastSpell(injuredStormwindInfantry, QuestSpellIds.RenewedLife, true);
		}
	}
}