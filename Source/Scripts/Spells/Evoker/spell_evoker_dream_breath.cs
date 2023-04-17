// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.Structs.S;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.GREEN_DREAM_BREATH, EvokerSpells.GREEN_DREAM_BREATH_2)]
internal class SpellEvokerDreamBreath : SpellScript, ISpellOnEpowerSpellEnd
{
    public void EmpowerSpellEnd(SpellEmpowerStageRecord stage, uint stageDelta)
    {
        var args = new CastSpellExtraArgs(TriggerCastFlags.TriggeredAllowProc)
        {
            EmpowerStage = stage.Stage
        };

        Caster.SpellFactory.CastSpell(new CastSpellTargetArg(), EvokerSpells.GREEN_DREAM_BREATH_CHARGED, args);
    }
}