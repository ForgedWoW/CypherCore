// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.DataStorage;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.FIRE_BREATH, EvokerSpells.FIRE_BREATH_2)]
internal class spell_evoker_fire_breath : SpellScript, ISpellOnEpowerSpellEnd
{
	public void EmpowerSpellEnd(SpellEmpowerStageRecord stage, uint stageDelta)
	{
		Caster.CastSpell(EvokerSpells.FIRE_BREATH_CHARGED, true, stage.Stage);
	}
}