// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

// Taste for Blood - 206333
[SpellScript(206333)]
public class SpellWarrTasteForBlood : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo eventInfo)
    {
        if ((eventInfo.HitMask & ProcFlagsHit.Critical) != 0 && eventInfo.SpellInfo.Id == WarriorSpells.BLOODTHIRST_DAMAGE)
        {
            Aura.SetDuration(0);

            return true;
        }

        return false;
    }
}