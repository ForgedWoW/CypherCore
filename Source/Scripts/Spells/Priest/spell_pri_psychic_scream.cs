// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Priest;

[SpellScript(8122)]
public class SpellPriPsychicScream : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo eventInfo)
    {
        var target = eventInfo.ActionTarget;

        if (target == null)
            return false;

        var dmg = eventInfo.DamageInfo;
        var fear = Aura;

        if (fear != null && dmg != null && dmg.Damage > 0)
            fear.SetDuration(0);

        return true;
    }
}