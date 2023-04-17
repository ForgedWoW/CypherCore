// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Items;

[Script] // 64415 Val'anyr Hammer of Ancient Kings - Equip Effect
internal class SpellItemValanyrHammerOfAncientKings : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo eventInfo)
    {
        return eventInfo.HealInfo != null && eventInfo.HealInfo.EffectiveHeal > 0;
    }
}