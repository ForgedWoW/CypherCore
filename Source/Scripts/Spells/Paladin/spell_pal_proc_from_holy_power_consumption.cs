// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Framework.Constants;

namespace Scripts.Spells.Paladin;

// 271580 - Divine Judgement
// 85804 - Selfless Healer
[SpellScript(new uint[]
{
    271580, 85804
})]
public class SpellPalProcFromHolyPowerConsumption : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo eventInfo)
    {
        var procSpell = eventInfo.ProcSpell;

        if (procSpell == null)
            return false;

        var cost = SpellInfo.CalcPowerCost(PowerType.HolyPower, false, Caster, SpellInfo.SchoolMask, null);

        return cost != null && cost.Amount > 0;
    }
}