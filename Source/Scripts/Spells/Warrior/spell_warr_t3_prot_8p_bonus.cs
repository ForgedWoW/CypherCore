﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Warrior;

[Script] // 28845 - Cheat Death
internal class spell_warr_t3_prot_8p_bonus : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo eventInfo)
    {
        if (eventInfo.ActionTarget.HealthBelowPct(20))
            return true;

        var damageInfo = eventInfo.DamageInfo;

        if (damageInfo != null &&
            damageInfo.Damage != 0)
            if (Target.HealthBelowPctDamaged(20, damageInfo.Damage))
                return true;

        return false;
    }
}