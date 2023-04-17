// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenProcChargeDropOnly : AuraScript, IAuraOnProc
{
    public void OnProc(ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
    }
}