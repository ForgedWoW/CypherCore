﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage.Structs.S;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.BLUE_ETERNITY_SURGE, EvokerSpells.BLUE_ETERNITY_SURGE_2)]
internal class SpellEvokerEternitySurge : SpellScript, ISpellOnEpowerSpellEnd
{
    public void EmpowerSpellEnd(SpellEmpowerStageRecord stage, uint stageDelta)
    {
        var caster = Caster;

        // cast on primary target
        caster.SpellFactory.CastSpell(EvokerSpells.ETERNITY_SURGE_CHARGED, true, stage.Stage);

        // determine number of additional targets
        var multi = 1;

        if (caster.HasSpell(EvokerSpells.ETERNITYS_SPAN))
            multi = 2;

        var targets = 1 * multi;

        switch (Spell.EmpoweredStage)
        {
            case 1:
                targets = 2 * multi;

                break;
            case 2:
                targets = 3 * multi;

                break;
            case 3:
                targets = 4 * multi;

                break;
        }

        targets--;

        if (targets > 0)
        {
            // get targets
            var targetList = new List<Unit>();
            caster.GetEnemiesWithinRange(targetList, GetEffectInfo(1).MaxRadiusEntry.RadiusMax);

            // reduce targetList to the number allowed
            targetList.RandomResize(targets);

            // cast on targets
            foreach (var target in targetList)
                caster.SpellFactory.CastSpell(target, EvokerSpells.ETERNITY_SURGE_CHARGED, true, stage.Stage);
        }
    }
}