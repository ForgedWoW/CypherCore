// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Mage;

[SpellScript(235711)]
public class SpellMageChronoShift : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo eventInfo)
    {
        var spellCanProc = (eventInfo.SpellInfo.Id == MageSpells.ARCANE_BARRAGE || eventInfo.SpellInfo.Id == MageSpells.ARCANE_BARRAGE_TRIGGERED);

        if (spellCanProc)
            return true;

        return false;
    }
}