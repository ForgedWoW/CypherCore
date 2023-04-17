// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Framework.Constants;

namespace Scripts.Spells.Monk;

[SpellScript(195630)]
public class SpellMonkElusiveBrawlerStacks : AuraScript
{
    public bool CheckProc(ProcEventInfo eventInfo)
    {
        if (eventInfo.HitMask.HasFlag(ProcFlagsHit.Dodge))
            return false;

        var elusiveBrawler = Caster.GetAura(MonkSpells.ELUSIVE_BRAWLER, Caster.GUID);

        if (elusiveBrawler != null)
            elusiveBrawler.SetDuration(0);

        return true;
    }
}