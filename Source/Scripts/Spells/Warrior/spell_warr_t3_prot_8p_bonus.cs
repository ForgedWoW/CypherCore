// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Warrior;

[Script] // 28845 - Cheat Death
internal class SpellWarrT3Prot8PBonus : AuraScript, IAuraCheckProc
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