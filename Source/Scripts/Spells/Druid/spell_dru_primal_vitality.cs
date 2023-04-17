// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[SpellScript(202808)]
public class SpellDruPrimalVitality : AuraScript, IHasAuraEffects
{
    private const int PrimalVitalityPassive = 202808;
    private const int PrimalVitalityEffect = 202812;
    private const int Prowl = 5215;

    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public bool CheckProc(ProcEventInfo eventInfo)
    {
        if (eventInfo.SpellInfo != null)
            return false;

        if (eventInfo.DamageInfo != null)
            return false;

        if (eventInfo.SpellInfo.Id != Prowl)
            return false;

        return true;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
    }

    private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        var target = eventInfo.ProcTarget;

        if (target != null)
            if (!target.HasAura(PrimalVitalityEffect))
                target.AddAura(PrimalVitalityEffect, target);
    }
}