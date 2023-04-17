﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.CYCLE_OF_LIFE_AURA)]
public class AuraEvokerCycleOfLife : AuraScript, IAuraOnProc, IAuraOnApply
{
    double _multiplier = 0;

    public void AuraApply()
    {
        _multiplier = SpellManager.Instance.GetSpellInfo(EvokerSpells.CYCLE_OF_LIFE).GetEffect(0).BasePoints * 0.01;
    }

    public void OnProc(ProcEventInfo info)
    {
        if (info.HealInfo == null)
            return;

        var eff = Aura.AuraEffects[0];

        eff.ChangeAmount(eff.Amount + info.HealInfo.Heal * _multiplier);
    }
}