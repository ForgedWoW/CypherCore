// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Shaman;

// Crash Lightning aura - 187878
[SpellScript(187878)]
public class SpellShaCrashLightningAura : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo eventInfo)
    {
        if (eventInfo.SpellInfo.Id == ShamanSpells.STORMSTRIKE_MAIN || eventInfo.SpellInfo.Id == ShamanSpells.LAVA_LASH)
            return true;

        return false;
    }
}