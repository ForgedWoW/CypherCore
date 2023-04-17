// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Hunter;

[SpellScript(199527)]
public class SpellHunTrueAim : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo eventInfo)
    {
        if (eventInfo.SpellInfo.Id == HunterSpells.AIMED_SHOT || eventInfo.SpellInfo.Id == HunterSpells.ARCANE_SHOT)
            return true;

        return false;
    }
}