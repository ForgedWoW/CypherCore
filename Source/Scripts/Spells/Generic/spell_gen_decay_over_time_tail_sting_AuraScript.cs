// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Generic;

[Script] // 36659 - Tail Sting
internal class SpellGenDecayOverTimeTailStingAuraScript : AuraScript, IAuraOnProc, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo eventInfo)
    {
        return eventInfo.SpellInfo == SpellInfo;
    }

    public void OnProc(ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        ModStackAmount(-1);
    }
}