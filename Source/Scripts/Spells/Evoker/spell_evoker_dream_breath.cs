// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.DataStorage;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.GREEN_DREAM_BREATH, EvokerSpells.GREEN_DREAM_BREATH_2)]
internal class spell_evoker_dream_breath : SpellScript, ISpellOnEpowerSpellEnd
{
	public void EmpowerSpellEnd(SpellEmpowerStageRecord stage, uint stageDelta)
    {
        var args = new CastSpellExtraArgs(TriggerCastFlags.TriggeredAllowProc) { EmpowerStage = stage.Stage };
		Caster.CastSpell(new CastSpellTargetArg(), EvokerSpells.GREEN_DREAM_BREATH_CHARGED, args);
	}
}