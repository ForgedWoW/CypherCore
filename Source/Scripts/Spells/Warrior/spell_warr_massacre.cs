// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

//206315
[SpellScript(206315)]
public class SpellWarrMassacre : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo procInfo)
    {
        if (procInfo.SpellInfo.Id == WarriorSpells.EXECUTE)
            if ((procInfo.HitMask & ProcFlagsHit.Critical) != 0)
                return true;

        return false;
    }
}