// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Paladin;

// 271580 - Divine Judgement
// 85804 - Selfless Healer
[SpellScript(new uint[]
{
    271580, 85804
})]
public class spell_pal_proc_from_holy_power_consumption : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo eventInfo)
    {
        var procSpell = eventInfo.ProcSpell;

        if (procSpell == null)
            return false;

        var cost = SpellInfo.CalcPowerCost(PowerType.HolyPower, false, Caster, SpellInfo.GetSchoolMask(), null);

        return cost != null && cost.Amount > 0;
    }
}