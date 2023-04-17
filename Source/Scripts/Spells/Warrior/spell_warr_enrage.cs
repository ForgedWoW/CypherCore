// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

// Enrage - 184361
[SpellScript(184361)]
public class SpellWarrEnrage : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo eventInfo)
    {
        if (eventInfo.SpellInfo.Id == WarriorSpells.BLOODTHIRST_DAMAGE && (eventInfo.HitMask & ProcFlagsHit.Critical) != 0)
            return true;

        return false;
    }
}