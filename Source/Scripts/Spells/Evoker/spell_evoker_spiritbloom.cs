// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage.Structs.S;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.GREEN_SPIRITBLOOM, EvokerSpells.GREEN_SPIRITBLOOM_2)]
internal class SpellEvokerSpiritbloom : SpellScript, ISpellOnEpowerSpellEnd
{
    public void EmpowerSpellEnd(SpellEmpowerStageRecord stage, uint stageDelta)
    {
        var caster = Caster;

        // cast on primary target
        caster.SpellFactory.CastSpell(EvokerSpells.SPIRITBLOOM_CHARGED, true, stage.Stage);

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
        }

        if (targets > 0)
        {
            // get targets that are injured
            var targetList = new List<Unit>();
            caster.GetAlliesWithinRange(targetList, SpellInfo.GetMaxRange());
            targetList.RemoveIf(a => a.IsFullHealth);

            // reduce targetList to the number allowed
            targetList.RandomResize(targets);

            // cast on targets
            var args = new CastSpellExtraArgs(TriggerCastFlags.TriggeredAllowProc)
            {
                EmpowerStage = stage.Stage
            };

            foreach (var target in targetList)
                caster.SpellFactory.CastSpell(target, EvokerSpells.SPIRITBLOOM_CHARGED, args);
        }
    }
}