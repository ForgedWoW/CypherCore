// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[SpellScript(159286)]
public class SpellDruPrimalFury : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo eventInfo)
    {
        var spellCanProc = (eventInfo.SpellInfo.Id == DruidSpells.Shred || eventInfo.SpellInfo.Id == DruidSpells.Rake || eventInfo.SpellInfo.Id == DruidSpells.SwipeCat || eventInfo.SpellInfo.Id == DruidSpells.MoonfireCat);

        if ((eventInfo.HitMask & ProcFlagsHit.Critical) != 0 && spellCanProc)
            return true;

        return false;
    }
}