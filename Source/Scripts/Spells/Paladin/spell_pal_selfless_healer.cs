// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Paladin;

[SpellScript(85804)] // 85804 - Selfless Healer
internal class SpellPalSelflessHealer : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraCheckEffectProcHandler(CheckEffectProc, 0, AuraType.ProcTriggerSpell));
    }

    private bool CheckEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        var procSpell = eventInfo.ProcSpell;

        if (procSpell != null)
            return procSpell.HasPowerTypeCost(PowerType.HolyPower);

        return false;
    }
}