// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Generic;

[Script("spell_item_soul_harvesters_charm")]
[Script("spell_item_commendation_of_kaelthas")]
[Script("spell_item_corpse_tongue_coin")]
[Script("spell_item_corpse_tongue_coin_heroic")]
[Script("spell_item_petrified_twilight_scale")]
[Script("spell_item_petrified_twilight_scale_heroic")]
internal class SpellGenProcBelowPctDamaged : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo eventInfo)
    {
        var damageInfo = eventInfo.DamageInfo;

        if (damageInfo == null ||
            damageInfo.Damage == 0)
            return false;

        var pct = SpellInfo.GetEffect(0).CalcValue();

        if (eventInfo.ActionTarget.HealthBelowPctDamaged(pct, damageInfo.Damage))
            return true;

        return false;
    }
}